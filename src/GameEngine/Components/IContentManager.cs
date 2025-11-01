namespace Nexus.GameEngine.Components;

/// <summary>
/// Provides an interface for managing reusable content trees that can be assigned to viewports.
/// Content may include UI screens, game levels, menus, or any component hierarchy.
/// Supports loading, creating, caching, retrieving, and updating content components.
/// Internally delegates component creation to IComponentFactory while managing the lifecycle.
/// </summary>
public interface IContentManager : IDisposable
{
    /// <summary>
    /// Loads and creates content from a template. This is the primary method for creating
    /// main content that will be rendered immediately.
    /// 
    /// Process: Creates → Configures (IsLoaded=true) → Activates all IRuntimeComponents → Caches
    /// 
    /// Use Load() when you want content ready to render the next frame.
    /// Use Create() when you need more control over activation timing.
    /// </summary>
    /// <param name="template">The template describing the content to load.</param>
    /// <param name="activate">If true (default), activates all IRuntimeComponents in the tree.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Load(Configurable.Template template, bool activate = true);

    /// <summary>
    /// Creates a component instance via dependency injection without configuration or activation.
    /// 
    /// Process: Creates via DI → Sets ContentManager reference → Registers if named
    /// 
    /// The component is NOT configured (IsLoaded=false) or activated.
    /// Used by components to create children that need to be managed.
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
    /// Process: Creates via DI → Configures (IsLoaded=true) → Creates subcomponents
    /// 
    /// The component is configured but NOT activated. Use when you need to control activation timing.
    /// Used by components to create configured children.
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
    /// Used by components to create children from templates.
    /// </summary>
    /// <param name="template">The template to use for instantiation and configuration.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? CreateInstance(Configurable.Template template);

    /// <summary>
    /// Retrieves a component with the specified identifier, or null if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the component to retrieve.</param>
    /// <returns>The <see cref="IComponent"/> with the given id, or null if not found.</returns>
    IComponent? Get(string id);

    /// <summary>
    /// Updates all managed content components for the current frame.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update, in seconds.</param>
    void OnUpdate(double deltaTime);
}