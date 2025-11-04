namespace Nexus.GameEngine.Components;

/// <summary>
/// Factory for creating and configuring components via dependency injection.
/// Handles component instantiation, logger setup, and configuration application,
/// but does NOT manage caching or lifecycle (which belongs to ContentManager).
/// </summary>
public class ComponentFactory(
    IServiceProvider serviceProvider,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager) : IComponentFactory
{
    /// <summary>
    /// Creates a component instance via dependency injection.
    /// 
    /// This method:
    /// 1. Creates the component via DI
    /// 2. Sets ResourceManager and PipelineManager properties if component is IDrawable
    /// 
    /// The component is NOT configured or activated.
    /// ContentManager is responsible for setting the ContentManager reference.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType)
    {
        if (componentType == null) return null;

        var obj = serviceProvider.GetService(componentType);
        if (obj is not IComponent component)
            return null;

        // Set ResourceManager and PipelineManager for drawable components
        if (component is Drawable drawable)
        {
            drawable.ResourceManager = resourceManager;
            drawable.PipelineManager = pipelineManager;
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
    /// 
    /// The component is NOT configured or activated.
    /// ContentManager is responsible for setting the ContentManager reference before calling this.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType, Template template)
    {
        var component = Create(componentType);
        if (component == null) return null;

        // Configure the component if it supports configuration
        if (component is IConfigurable configurable)
        {
            configurable.Load(template);
        }

        return component;
    }

    /// <inheritdoc/>
    public IComponent? Create<T>(Template template) where T : IComponent
        => Create(typeof(T), template);

    /// <inheritdoc/>
    public IComponent? CreateInstance(Template template)
    {
        if (template == null) return null;

        // Get the component type from the template's ComponentType property
        var componentType = template.ComponentType;

        // Type-safety: Ensure the type implements IComponent
        if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType)) return null;

        // Check if the type can be instantiated
        if (componentType.IsAbstract) return null;

        if (componentType.IsInterface) return null;

        if (componentType.IsGenericTypeDefinition) return null;

        if (componentType.IsSealed && componentType.IsAbstract) return null;

        // Create and configure the component
        var result = Create(componentType, template);

        return result;
    }
}
