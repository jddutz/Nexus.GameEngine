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
    /// Creates and activates components from a template. This is the primary method for creating
    /// main content that will be rendered immediately.
    /// 
    /// Process: Creates → Configures (IsLoaded=true) → Activates all components → Caches
    /// 
    /// Use Load() when you want content ready to render the next frame.
    /// Use Create() when you need more control over activation timing.
    /// </summary>
    /// <param name="template">The template describing the content to load.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Load(Template template);

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
    IComponent? Create(Type componentType, Template template);

    /// <summary>
    /// Creates and configures a component from a template without activation.
    /// </summary>
    /// <typeparam name="T">The component type to create.</typeparam>
    /// <param name="template">The template to use for configuration.</param>
    /// <returns>The created <see cref="IComponent"/>, or null if creation failed.</returns>
    IComponent? Create<T>(Template template) where T : IComponent;

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
    IComponent? CreateInstance(Template template);

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

    /// <summary>
    /// Gets all active cameras in the content tree, sorted by RenderPriority (ascending).
    /// Cameras are automatically discovered by walking the content tree.
    /// If no cameras are found, returns a single default StaticCamera.
    /// </summary>
    IEnumerable<ICamera> ActiveCameras { get; }

    /// <summary>
    /// Gets all loaded root content components.
    /// Used by the Renderer to collect draw commands from all content trees.
    /// </summary>
    IEnumerable<IComponent> LoadedContent { get; }

    /// <summary>
    /// Gets all visible drawable components discovered during the last Update cycle.
    /// This cached list is built during OnUpdate to move tree traversal out of the render hot path.
    /// </summary>
    IEnumerable<IDrawable> VisibleDrawables { get; }

    /// <summary>
    /// Refreshes the camera list by walking the content tree and finding all active ICamera instances.
    /// Cameras are automatically sorted by RenderPriority (ascending).
    /// Called automatically during Load() and can be called manually after adding/removing cameras.
    /// </summary>
    void RefreshCameras();
}

