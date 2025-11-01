namespace Nexus.GameEngine.Components;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Provides efficient caching, lifecycle management, and disposal of component hierarchies.
/// Delegates component creation to IComponentFactory.
/// </summary>
public class ContentManager(
    IComponentFactory componentFactory) 
    : IContentManager
{
    private readonly Dictionary<string, IComponent> content = [];
    private bool _disposed;

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
    public IComponent? Load(Configurable.Template template, bool activate = true)
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
    public IComponent? Create(Type componentType, Configurable.Template template)
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
        if (component is Component componentWithChildren && template is Component.Template componentTemplate)
        {
            foreach (var subTemplate in componentTemplate.Subcomponents)
            {
                componentWithChildren.CreateChild(subTemplate);
            }
        }

        return component;
    }

    /// <inheritdoc/>
    public IComponent? Create<T>(Configurable.Template template) where T : IComponent
        => Create(typeof(T), template);

    /// <summary>
    /// Creates a component instance from a template.
    /// Infers type, creates component, sets ContentManager, configures, and creates subcomponents.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? CreateInstance(Configurable.Template template)
    {
        if (template == null) return null;

        // Infer component type from the template's declaring type
        var componentType = template.GetType().DeclaringType;
        if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
        {
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
            
            // SECOND PASS: Update all active RuntimeComponents
            // Follow same pattern as Renderer: traverse tree, update IRuntimeComponents
            componentStack.Clear();
            
            foreach (var kvp in content)
            {
                var component = kvp.Value;
                if (component.IsLoaded && component.Parent == null)
                {
                    componentStack.Push(component);
                }
            }
            
            // Traverse component tree and update IRuntimeComponents
            while (componentStack.Count > 0)
            {
                var component = componentStack.Pop();
                
                // Update if it's a RuntimeComponent and active
                if (component is IRuntimeComponent runtimeComponent)
                {
                    if (runtimeComponent.IsActive())
                    {
                        runtimeComponent.Update(deltaTime);
                        // Don't traverse children - RuntimeComponent.Update() already does this recursively
                        continue;
                    }
                }
                
                // For non-RuntimeComponents (plain containers), traverse their children
                foreach (var child in component.Children)
                {
                    componentStack.Push(child);
                }
            }
            
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
