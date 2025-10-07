using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Extension methods for runtime components to help with viewport and content management.
/// </summary>
public static class ComponentExtensions
{
    /// <summary>
    /// Finds the parent viewport that contains this component.
    /// Walks up the component tree until a viewport is found.
    /// </summary>
    /// <param name="component">The component to start searching from</param>
    /// <returns>The parent viewport, or null if not found</returns>
    public static IViewport? FindParentViewport(this IRuntimeComponent component)
    {
        var current = component.Parent;
        while (current != null)
        {
            if (current is IViewport viewport)
                return viewport;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds a parent component of the specified type.
    /// Walks up the component tree until a component of type T is found.
    /// </summary>
    /// <typeparam name="T">The type of component to find</typeparam>
    /// <param name="component">The component to start searching from</param>
    /// <returns>The parent component of type T, or null if not found</returns>
    public static T? FindParent<T>(this IRuntimeComponent component) where T : class
    {
        var current = component.Parent;
        while (current != null)
        {
            if (current is T parent)
                return parent;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Schedules a content change for the viewport containing this component.
    /// The content change will be applied during the next update cycle.
    /// </summary>
    /// <param name="component">The component requesting the content change</param>
    /// <param name="newContent">The new content to display</param>
    /// <returns>True if a viewport was found and content change was scheduled, false otherwise</returns>
    public static bool ChangeViewportContent(this IRuntimeComponent component, IRuntimeComponent? newContent)
    {
        var viewport = component.FindParentViewport();
        if (viewport != null)
        {
            viewport.Content = newContent;
            return true;
        }
        return false;
    }
}