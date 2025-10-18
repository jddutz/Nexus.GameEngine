using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace Nexus.GameEngine.Runtime;

/// <inheritdoc cref="IDisposable"/>
/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Content can include UI screens, game levels, menus, or any component hierarchy.
/// </summary>
public interface IContentManager : IDisposable
{
    /// <summary>
    /// Main viewport, defines how components should be displayed on the screen.
    /// </summary>
    IViewport Viewport { get; }

    /// <summary>
    /// Loads content from a template. If the template is a Viewport.Template, creates a new viewport
    /// from it. Otherwise, creates a default viewport and assigns the created content to it.
    /// This method can be called to switch game modes or scenes.
    /// </summary>
    /// <param name="template">The template to load. Can be a Viewport.Template or any other component template.</param>
    void Load(IComponentTemplate template);

    /// <summary>
    /// Creates a new runtime component from the specified template.
    /// </summary>
    /// <param name="template">The component template to instantiate.</param>
    /// <param name="activate">Whether to activate the component after creation.</param>
    /// <returns>The created <see cref="IRuntimeComponent"/>, or null if creation failed.</returns>
    IRuntimeComponent? Create(IComponentTemplate template, string id = "", bool activate = true);

    /// <summary>
    /// Gets an existing component by ID or creates a new one from the template if not found.
    /// </summary>
    /// <param name="template">The component template to instantiate if needed.</param>
    /// <param name="id">The unique identifier for the component.</param>
    /// <param name="activate">Whether to activate the component after creation.</param>
    /// <returns>The existing or newly created <see cref="IRuntimeComponent"/>, or null if creation failed.</returns>
    IRuntimeComponent? GetOrCreate(IComponentTemplate template, string id, bool activate = true);

    /// <summary>
    /// Updates all managed content components for the current frame.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the last update, in seconds.</param>
    void OnUpdate(double deltaTime);
}