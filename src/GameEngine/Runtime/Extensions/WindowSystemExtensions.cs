using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime.Extensions;

/// <summary>
/// Extension methods for the window system to provide easy access to common window operations.
/// </summary>
public static class WindowSystemExtensions
{
    /// <summary>
    /// Gets the underlying window instance.
    /// </summary>
    /// <param name="system">The window system.</param>
    /// <returns>The window instance.</returns>
    public static IWindow GetWindow(this IWindowSystem system)
    {
        return ((WindowSystem)system).Window;
    }

    /// <summary>
    /// Gets the current size of the window.
    /// </summary>
    /// <param name="system">The window system.</param>
    /// <returns>The window size as a Vector2D.</returns>
    public static Vector2D<int> GetSize(this IWindowSystem system)
    {
        return ((WindowSystem)system).Window.Size;
    }

    /// <summary>
    /// Gets the current position of the window.
    /// </summary>
    /// <param name="system">The window system.</param>
    /// <returns>The window position as a Vector2D.</returns>
    public static Vector2D<int> GetPosition(this IWindowSystem system)
    {
        return ((WindowSystem)system).Window.Position;
    }

    /// <summary>
    /// Sets the title of the window.
    /// </summary>
    /// <param name="system">The window system.</param>
    /// <param name="title">The new title.</param>
    public static void SetTitle(this IWindowSystem system, string title)
    {
        ((WindowSystem)system).Window.Title = title;
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    /// <param name="system">The window system.</param>
    public static void Close(this IWindowSystem system)
    {
        ((WindowSystem)system).Window.Close();
    }
}
