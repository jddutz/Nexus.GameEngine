using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <inheritdoc cref="IDisposable"/>
/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Content can include UI screens, game levels, menus, or any component hierarchy.
/// </summary>
public interface IContentManager : IDisposable
{
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