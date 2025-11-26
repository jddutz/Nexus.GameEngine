using Microsoft.Extensions.DependencyInjection;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Factory for creating and configuring components via dependency injection.
/// Handles component instantiation, logger setup, and configuration application,
/// but does NOT manage caching or lifecycle (which belongs to ContentManager).
/// </summary>
public class ComponentFactory(
    IServiceProvider serviceProvider) : IComponentFactory
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
        if (componentType == null) throw new ArgumentNullException(nameof(componentType));

        if (componentType.IsAbstract || componentType.IsInterface)
            throw new InvalidOperationException($"Cannot create component for abstract or interface type '{componentType.FullName}'. Provide a concrete component type.");

        // Create instance via DI (constructor injection). ActivatorUtilities will resolve
        // constructor services from the IServiceProvider and call the appropriate ctor.
        var obj = ActivatorUtilities.CreateInstance(serviceProvider, componentType);
        if (obj is not IComponent component)
            throw new InvalidOperationException($"Type '{componentType.FullName}' created by DI does not implement IComponent.");

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
    /// The component is configured (Loaded) but NOT activated.
    /// ContentManager is responsible for setting the ContentManager reference before calling this.
    /// </summary>
    /// <inheritdoc/>
    public IComponent? Create(Type componentType, Template template)
    {
        var component = Create(componentType);
        if (component == null) return null;

        // Configure the component if it supports configuration
        if (component is ILoadable loadable)
        {
            loadable.Load(template);
        }

        return component;
    }

    /// <inheritdoc/>
    public IComponent? Create<T>(Template template) where T : IComponent
        => Create(typeof(T), template);

    /// <inheritdoc/>
    public IComponent? CreateInstance(Template template)
    {
        ArgumentNullException.ThrowIfNull(template);

        // Get the component type from the template's ComponentType property
        var componentType = template.ComponentType 
            ?? throw new InvalidOperationException($"Template '{template.GetType().Name}' does not specify a ComponentType.");

        // Type-safety: Ensure the type implements IComponent
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new InvalidOperationException($"ComponentType '{componentType.FullName}' does not implement IComponent.");

        // Check if the type can be instantiated
        if (componentType.IsAbstract || componentType.IsInterface)
            throw new InvalidOperationException($"ComponentType '{componentType.FullName}' is abstract or an interface and cannot be instantiated.");

        if (componentType.IsGenericTypeDefinition)
            throw new InvalidOperationException($"ComponentType '{componentType.FullName}' is an open generic type and cannot be instantiated.");

        // Create and configure the component
        var result = Create(componentType, template);

        return result;
    }
}
