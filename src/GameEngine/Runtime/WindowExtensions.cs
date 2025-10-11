using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime;

public static class WindowExtensions
{
    public static void ToggleFullscreen(this IWindow window)
    {
        if (window != null)
        {
            var isFullscreen = window.WindowState == WindowState.Fullscreen;

            if (isFullscreen)
            {
                // Switch to windowed mode - set properties in correct order
                window.WindowState = WindowState.Normal;
                window.WindowBorder = WindowBorder.Resizable;
                window.Size = new Vector2D<int>(1280, 720);
            }
            else
            {
                // Switch to fullscreen mode - set border first, then state
                window.WindowBorder = WindowBorder.Hidden;
                window.WindowState = WindowState.Fullscreen;
            }
        }
    }

    public static void SetFullscreen(this IWindow window, bool fullscreen)
    {
        if (window != null)
        {
            if (fullscreen)
            {
                window.WindowBorder = WindowBorder.Hidden;
                window.WindowState = WindowState.Fullscreen;
            }
            else
            {
                window.Size = new Vector2D<int>(1280, 720);
                window.WindowBorder = WindowBorder.Resizable;
                window.WindowState = WindowState.Normal;
            }
        }
    }
}