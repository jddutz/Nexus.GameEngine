namespace Nexus.GameEngine.Components;

/// <summary>
/// Provides an interface for creating and configuring components via dependency injection.
/// The factory is responsible for component instantiation, configuration, and setup,
/// but NOT for caching or lifecycle management (which belongs to ContentManager).
/// </summary>
public interface IComponentFactory
{
    /// <summary>
    /// Creates a component instance via dependency injection without configuration.
    /// 
    /// Process: Creates via DI → Sets ComponentFactory and Logger
    /// 
    /// The component is NOT configured (IsLoaded=false) or activated.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Create(Type componentType);

    /// <summary>
    /// Creates a component instance via dependency injection without configuration.
    /// </summary>
    /// <typeparam name="T">The component type to create.</typeparam>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Create<T>() where T : IComponent;

    /// <summary>
    /// Creates and configures a component from a template without activation.
    /// 
    /// Process: Creates via DI → Configures (IsLoaded=true) → Creates subcomponents recursively
    /// 
    /// The component is configured but NOT activated. Activation is the caller's responsibility.
    /// </summary>
    /// <param name="componentType">The type of component to create.</param>
    /// <param name="template">The template to use for configuration.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Create(Type componentType, Configurable.Template template);

    /// <summary>
    /// Creates and configures a component from a template without activation.
    /// </summary>
    /// <typeparam name="T">The component type to create.</typeparam>
    /// <param name="template">The template to use for configuration.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Create<T>(Configurable.Template template) where T : IComponent;

    /// <summary>
    /// Creates a component instance from a template by inferring the type from the template's declaring type.
    /// 
    /// Process: Infers type → Creates via DI → Configures (IsLoaded=true) → Creates subcomponents
    /// 
    /// The component is configured but NOT activated. Activation is the caller's responsibility.
    /// </summary>
    /// <param name="template">The template to use for instantiation and configuration.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? CreateInstance(Configurable.Template template);
}
