using Microsoft.Extensions.Options;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Provides efficient caching, lifecycle management, and disposal of component hierarchies.
/// Delegates component creation to IComponentFactory.
/// Also tracks active cameras in the content tree for automatic viewport management.
/// </summary>
public class ContentManager(
    IComponentFactory componentFactory,
    IOptions<GraphicsSettings> graphicsOptions) 
    : IContentManager
{
    private readonly GraphicsSettings _graphicsSettings = graphicsOptions.Value;
    private readonly Dictionary<string, IComponent> content = [];
    private readonly SortedSet<ICamera> _cameras = new(new RenderPriorityComparer<ICamera>());
    private readonly List<IDrawable> _visibleDrawables = [];
    private bool _disposed;

    /// <summary>
    /// Gets all active cameras in the content tree, sorted by RenderPriority (ascending).
    /// Cameras are automatically discovered by walking the content tree.
    /// Returns empty enumerable if no cameras are found in content.
    /// </summary>
    public IEnumerable<ICamera> ActiveCameras => _cameras;

    /// <summary>
    /// Gets all loaded root content components.
    /// Used by the Renderer to collect draw commands from all content trees.
    /// </summary>
    public IEnumerable<IComponent> LoadedContent => content.Values;

    /// <summary>
    /// Gets all visible drawable components discovered during the last Update cycle.
    /// This cached list is built during OnUpdate to move tree traversal out of the render hot path.
    /// </summary>
    public IEnumerable<IDrawable> VisibleDrawables => _visibleDrawables;

    /// <summary>
    /// Loads content from a template. This is the primary method for creating main content that will be rendered.
    /// 
    /// This method:
    /// 1. Creates the component and its subcomponents from the template
    /// 2. Configures all components (sets IsLoaded = true)
    /// 3. Activates all IRuntimeComponents in the tree (if activate = true)
    /// 4. Caches the content for reuse
    /// 
    /// Use Load() when you want content ready to render immediately.
    /// Use Create() when you need more control over the activation lifecycle.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Load(Template template, bool activate = true)
    {
        if (string.IsNullOrEmpty(template.Name))
        {
            Log.Warning("Cannot load content with empty template name");
            return null;
        }

        // Try to get existing content first
        if (content.TryGetValue(template.Name, out var existingComponent))
        {
            Log.Debug($"Returning existing content '{template.Name}'");
            return existingComponent;
        }

        // Content doesn't exist, create it
        try
        {
            var created = CreateInstance(template);
            if (created != null)
            {
                content[template.Name] = created;

                // Activate all IRuntimeComponents in the tree so content is ready to render
                if (activate)
                {
                    ActivateComponentTree(created);
                }

                // Note: Camera list is refreshed automatically during OnUpdate()

                Log.Info($"Loaded content '{template.Name}'");
                return created;
            }
            else
            {
                Log.Error($"Failed to create content from template '{template.Name}' - CreateInstance returned null");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, $"Exception while creating content from template '{template.Name}'");
            return null;
        }
    }

    /// <inheritdoc/>
    public IComponent? Get(string id)
    {
        return content.TryGetValue(id, out var component) ? component : null;
    }

    /// <summary>
    /// Creates a component instance via dependency injection.
    /// Delegates to ComponentFactory for actual creation, then sets ContentManager reference
    /// and optionally registers in the content dictionary.
    /// 
    /// This method:
    /// 1. Delegates creation to ComponentFactory
    /// 2. Sets ContentManager reference so children can be created
    /// 3. Registers component in content dictionary if it has a Name
    /// 
    /// The component is NOT configured or activated.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType)
    {
        if (componentType == null) return null;

        var component = componentFactory.Create(componentType);
        if (component == null) return null;

        component.ContentManager = this;

        // Add to content dictionary if it has a unique name/id
        var nameProp = component.GetType().GetProperty("Name");
        var idProp = component.GetType().GetProperty("Id");
        string? key = null;
        if (nameProp != null)
            key = nameProp.GetValue(component) as string;
        else if (idProp != null)
            key = idProp.GetValue(component) as string;
        if (!string.IsNullOrEmpty(key))
        {
            if (!content.ContainsKey(key!))
                content[key!] = component;
        }

        return component;
    }

    /// <inheritdoc/>
    public IComponent? Create<T>() where T : IComponent
        => Create(typeof(T));

    /// <summary>
    /// Creates a component from a template.
    /// 
    /// This method:
    /// 1. Creates the component instance (without configuration)
    /// 2. Sets ContentManager reference
    /// 3. Configures the component with template properties
    /// 4. Creates subcomponents from template
    /// 
    /// The component is configured but NOT activated.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType, Template template)
    {
        // Create component instance without configuration
        var component = componentFactory.Create(componentType);
        if (component == null) return null;

        // Set ContentManager so subcomponents can be created
        component.ContentManager = this;

        // Configure the component with template properties
        if (component is IConfigurable configurable)
        {
            configurable.Load(template);
        }

        // Create subcomponents after configuration is complete
        if (component is Component componentWithChildren && template.Subcomponents.Length > 0)
        {
            foreach (var subTemplate in template.Subcomponents)
            {
                componentWithChildren.CreateChild(subTemplate);
            }
        }

        return component;
    }

    /// <inheritdoc/>
    public IComponent? Create<T>(Template template) where T : IComponent
        => Create(typeof(T), template);

    /// <summary>
    /// Creates a component instance from a template.
    /// Gets type from template's ComponentType property, creates component, sets ContentManager, configures, and creates subcomponents.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? CreateInstance(Template template)
    {
        if (template == null) return null;

        // Get component type from the template's ComponentType property
        var componentType = template.ComponentType;
        if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
        {
            Log.Warning($"Template {template.GetType().Name} has invalid ComponentType: {componentType?.Name ?? "null"}");
            return null;
        }

        // Use the full Create method which handles configuration and subcomponents
        return Create(componentType, template);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Activates a component tree by traversing it and activating all IRuntimeComponent instances.
    /// Unlike OnUpdate, this does NOT skip based on IsActive() - all RuntimeComponents in the tree
    /// must be activated. Tree pruning happens during Update, not during activation.
    /// </summary>
    private void ActivateComponentTree(IComponent root)
    {
        var componentStack = new Stack<IComponent>();
        componentStack.Push(root);
        
        while (componentStack.Count > 0)
        {
            var component = componentStack.Pop();
            
            // Activate if it's a RuntimeComponent (activation handles its own children recursively)
            if (component is IRuntimeComponent runtimeComponent)
            {
                runtimeComponent.Activate();
                Log.Debug($"Activated component {component.Name ?? "unnamed"} ({component.GetType().Name})");
                // RuntimeComponent.Activate() recursively activates IRuntimeComponent children
                // So we don't need to traverse them manually - continue to next component
                continue;
            }
            
            // For non-RuntimeComponents (plain containers), manually traverse their children
            // to find RuntimeComponents that need activation
            foreach (var child in component.Children)
            {
                componentStack.Push(child);
            }
        }
    }

    public void OnUpdate(double deltaTime)
    {
        try
        {
            var unloadedKeys = new List<string>();
            var componentStack = new Stack<IComponent>();
            
            // FIRST PASS: Apply deferred updates to ALL components in tree at frame boundary
            // This ensures temporal consistency - all state changes happen before any Update() logic runs
            foreach (var kvp in content)
            {
                var component = kvp.Value;
                
                // Remove unloaded components from the dictionary
                if (!component.IsLoaded)
                {
                    unloadedKeys.Add(kvp.Key);
                    continue;
                }
                
                // Only traverse from roots
                if (component.Parent == null)
                {
                    componentStack.Push(component);
                }
            }
            
            // Traverse entire tree applying updates
            while (componentStack.Count > 0)
            {
                var component = componentStack.Pop();
                
                // Apply updates to all Entity-based components
                if (component is Entity entity)
                {
                    entity.ApplyUpdates(deltaTime);
                }
                
                // Traverse all children
                foreach (var child in component.Children)
                {
                    componentStack.Push(child);
                }
            }
            
            // SECOND PASS: Update all active RuntimeComponents and rebuild visible drawables and cameras
            // Follow same pattern as Renderer: traverse tree, update IRuntimeComponents
            componentStack.Clear();
            _visibleDrawables.Clear();  // Clear each frame - will be repopulated during traversal
            _cameras.Clear();  // Also clear cameras - will be repopulated during traversal
            
            foreach (var kvp in content)
            {
                var component = kvp.Value;
                if (component.IsLoaded && component.Parent == null)
                {
                    componentStack.Push(component);
                }
            }
            
            // Traverse component tree, update components, collect visible drawables and active cameras
            while (componentStack.Count > 0)
            {
                var component = componentStack.Pop();
                
                // Collect visible drawables as we traverse
                if (component is IDrawable drawable && drawable.IsVisible())
                {
                    _visibleDrawables.Add(drawable);
                    Log.Debug($"  Added visible drawable: {component.GetType().Name}");
                }
                
                // Collect active cameras as we traverse
                if (component is ICamera camera)
                {
                    if (camera is IRuntimeComponent cameraRuntime && cameraRuntime.IsActive())
                    {
                        _cameras.Add(camera);
                    }
                }
                
                // Push children onto stack first (so we collect drawables from entire tree)
                foreach (var child in component.Children)
                {
                    componentStack.Push(child);
                }
                
                // Update if it's a RuntimeComponent and active
                // Note: We still traverse children above to collect drawables from entire tree
                if (component is IRuntimeComponent runtimeComponent)
                {
                    if (runtimeComponent.IsActive())
                    {
                        runtimeComponent.Update(deltaTime);
                    }
                }
            }
            
            // Note: _cameras is a SortedSet, so cameras are automatically sorted by RenderPriority
            
            Log.Debug($"Total visible drawables after update: {_visibleDrawables.Count}");
            Log.Debug($"Total active cameras after update: {_cameras.Count}");
            
            // Clean up unloaded components
            foreach (var key in unloadedKeys)
            {
                Log.Debug($"Removing unloaded component {key} from content manager");
                content.Remove(key);
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Exception occurred during Update loop");
        }
    }

    /// <summary>
    /// Initializes the ContentManager and creates the default camera.
    /// Should be called during application startup before any content is loaded.
    /// </summary>
    public void Initialize()
    {
        // ContentManager initialization - camera discovery happens during Update()
        Log.Debug("ContentManager initialized");
    }

    /// <summary>
    /// Refreshes the camera list by walking the content tree and finding all active ICamera instances.
    /// Cameras are automatically sorted by RenderPriority (ascending).
    /// Called automatically during Load() and can be called manually after adding/removing cameras.
    /// </summary>
    public void RefreshCameras()
    {
        _cameras.Clear();

        // Walk all content trees looking for active cameras
        var componentStack = new Stack<IComponent>();
        foreach (var kvp in content)
        {
            var component = kvp.Value;
            if (component.IsLoaded && component.Parent == null)
            {
                componentStack.Push(component);
            }
        }

        // Traverse trees and collect active cameras
        while (componentStack.Count > 0)
        {
            var component = componentStack.Pop();

            // Check if this component is an active camera
            if (component is ICamera camera)
            {
                if (camera is IRuntimeComponent runtimeComponent && runtimeComponent.IsActive())
                {
                    _cameras.Add(camera);
                }
            }

            // Traverse children
            foreach (var child in component.Children)
            {
                componentStack.Push(child);
            }
        }

        // Note: _cameras is a SortedSet, so cameras are automatically sorted by RenderPriority

        Log.Debug($"Refreshed camera list: {_cameras.Count} active cameras found");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        // Dispose all managed content instances
        foreach (var component in content.Values)
        {
            component.Dispose();
        }

        content.Clear();

        _disposed = true;
    }
}
