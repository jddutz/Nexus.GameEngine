
using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Provides efficient creation, caching, and disposal of component hierarchies.
/// Also provides component creation logic previously in ComponentFactory.
/// </summary>
public class ContentManager(
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider) 
    : IContentManager
{
    private readonly ILogger logger = loggerFactory.CreateLogger<ContentManager>();
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
            logger.LogWarning("Cannot load content with empty template name");
            return null;
        }

        // Try to get existing content first
        if (content.TryGetValue(template.Name, out var existingComponent))
        {
            logger.LogDebug("Returning existing content '{ContentName}'", template.Name);
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

                logger.LogInformation("Loaded content '{ContentName}'", template.Name);
                return created;
            }
            else
            {
                logger.LogError("Failed to create content from template '{TemplateName}' - CreateInstance returned null", template.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while creating content from template '{TemplateName}'", template.Name);
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
    /// 
    /// This method:
    /// 1. Creates the component via DI
    /// 2. Sets ContentManager and Logger
    /// 3. Registers component in content dictionary if it has a Name
    /// 
    /// The component is NOT configured or activated. Use this when you need
    /// to create a component and manually control its configuration/activation.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType)
    {
        if (componentType == null) return null;

        var obj = serviceProvider.GetService(componentType);
        if (obj is not IComponent component)
            return null;

        component.ContentManager = this;
        component.Logger = loggerFactory.CreateLogger(componentType.Name);

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
    /// 1. Creates the component via DI
    /// 2. Configures the component with the template (sets IsLoaded = true)
    /// 3. Creates and configures all subcomponents recursively
    /// 
    /// The component is NOT activated. Use this when you want to create
    /// configured components but control activation timing yourself.
    /// For content that should be ready to render immediately, use Load() instead.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType, Configurable.Template template)
    {
        var component = Create(componentType);
        if (component == null) return null;

        // Configure the component if it supports configuration
        if (component is IConfigurable configurable)
        {
            configurable.Load(template);
        }

        // Create children from Subcomponents
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

    /// <inheritdoc/>
    public IComponent? CreateInstance(Configurable.Template template)
    {
        if (template == null)
        {
            logger?.LogDebug("CreateInstance called with null template");
            return null;
        }

        // Try to get the component type from the template's containing class
        var componentType = template.GetType().DeclaringType;

        logger.LogDebug("Inferred component type: {TypeName}", componentType?.Name ?? "null");

        // Type-safety: Ensure the type implements IComponent
        if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
        {
            logger.LogDebug("Component type is null or doesn't implement IComponent");
            return null;
        }

        // Check if the type can be instantiated
        if (componentType.IsAbstract)
        {
            logger.LogWarning("Cannot create instance of abstract type: {TypeName}. Use a concrete implementation instead.", componentType.Name);
            return null;
        }

        if (componentType.IsInterface)
        {
            logger.LogWarning("Cannot create instance of interface type: {TypeName}. Use a concrete implementation instead.", componentType.Name);
            return null;
        }

        if (componentType.IsGenericTypeDefinition)
        {
            logger.LogWarning("Cannot create instance of generic type definition: {TypeName}. Specify concrete type arguments.", componentType.Name);
            return null;
        }

        if (componentType.IsSealed && componentType.IsAbstract) // static class
        {
            logger.LogWarning("Cannot create instance of static class: {TypeName}", componentType.Name);
            return null;
        }

        logger.LogDebug("Creating {Name} component ({TypeName})", template.Name ?? "unnamed", componentType.Name);

        // Create and configure the component
        var result = Create(componentType, template);

        logger.LogDebug("Component creation result: {Result}", result != null ? "SUCCESS" : "FAILED");

        return result;
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
                logger.LogDebug("Activated component {Name} ({Type})", component.Name ?? "unnamed", component.GetType().Name);
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
                logger.LogDebug("Removing unloaded component {Name} from content manager", key);
                content.Remove(key);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during Update loop");
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
