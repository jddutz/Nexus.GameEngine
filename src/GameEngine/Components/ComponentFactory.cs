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
        if (component is IDrawable drawable)
        {
            var drawableType = drawable.GetType();
            
            // Set ResourceManager property if it exists
            var resourceManagerProp = drawableType.GetProperty("ResourceManager");
            if (resourceManagerProp != null && resourceManagerProp.CanWrite)
            {
                resourceManagerProp.SetValue(drawable, resourceManager);
            }
            
            // Set PipelineManager property if it exists
            var pipelineManagerProp = drawableType.GetProperty("PipelineManager");
            if (pipelineManagerProp != null && pipelineManagerProp.CanWrite)
            {
                pipelineManagerProp.SetValue(drawable, pipelineManager);
            }
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
    public IComponent? Create(Type componentType, Configurable.Template template)
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
    public IComponent? Create<T>(Configurable.Template template) where T : IComponent
        => Create(typeof(T), template);

    /// <inheritdoc/>
    public IComponent? CreateInstance(Configurable.Template template)
    {
        if (template == null)
        {
            Log.Debug("CreateInstance called with null template");
            return null;
        }

        // Try to get the component type from the template's containing class
        var componentType = template.GetType().DeclaringType;

        Log.Debug($"Inferred component type: {componentType?.Name ?? "null"}");

        // Type-safety: Ensure the type implements IComponent
        if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
        {
            Log.Debug("Component type is null or doesn't implement IComponent");
            return null;
        }

        // Check if the type can be instantiated
        if (componentType.IsAbstract)
        {
            Log.Warning($"Cannot create instance of abstract type: {componentType.Name}. Use a concrete implementation instead.");
            return null;
        }

        if (componentType.IsInterface)
        {
            Log.Warning($"Cannot create instance of interface type: {componentType.Name}. Use a concrete implementation instead.");
            return null;
        }

        if (componentType.IsGenericTypeDefinition)
        {
            Log.Warning($"Cannot create instance of generic type definition: {componentType.Name}. Specify concrete type arguments.");
            return null;
        }

        if (componentType.IsSealed && componentType.IsAbstract) // static class
        {
            Log.Warning($"Cannot create instance of static class: {componentType.Name}");
            return null;
        }

        Log.Debug($"Creating {template.Name ?? "unnamed"} component ({componentType.Name})");

        // Create and configure the component
        var result = Create(componentType, template);

        Log.Debug($"Component creation result: {(result != null ? "SUCCESS" : "FAILED")}");

        return result;
    }
}
