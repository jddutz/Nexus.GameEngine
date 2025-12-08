using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime.Systems;

namespace Nexus.GameEngine.Runtime.Extensions;

/// <summary>
/// Extension methods for the content system to provide easy access to common content operations.
/// </summary>
public static class ContentSystemExtensions
{
    /// <summary>
    /// Loads content from a template.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="template">The template to load.</param>
    /// <returns>The loaded component, or null if loading failed.</returns>
    public static IComponent? Load(this IContentSystem system, ComponentTemplate template)
    {
        return ((ContentSystem)system).ContentManager.Load(template);
    }

    /// <summary>
    /// Unloads a component.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="component">The component to unload.</param>
    public static void Unload(this IContentSystem system, IComponent component)
    {
        component.Unload();
    }

    /// <summary>
    /// Creates a component instance from a template without loading it.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="template">The template to create an instance from.</param>
    /// <returns>The created component instance, or null if creation failed.</returns>
    public static IComponent? CreateInstance(this IContentSystem system, ComponentTemplate template)
    {
        return ((ContentSystem)system).ContentManager.CreateInstance(template);
    }

    /// <summary>
    /// Activates a component.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="component">The component to activate.</param>
    public static void Activate(this IContentSystem system, IComponent component)
    {
        component.Activate();
    }

    /// <summary>
    /// Deactivates a component.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="component">The component to deactivate.</param>
    public static void Deactivate(this IContentSystem system, IComponent component)
    {
        component.Deactivate();
    }

    /// <summary>
    /// Updates a component.
    /// </summary>
    /// <param name="system">The content system.</param>
    /// <param name="component">The component to update.</param>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public static void Update(this IContentSystem system, IComponent component, double deltaTime)
    {
        if (component is IUpdatable updatable)
        {
            updatable.Update(deltaTime);
        }
    }
}
