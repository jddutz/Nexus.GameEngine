using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics.Rendering;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Tests;

public class OpenGLTestApp : IDisposable
{
    public GL GL { get; }
    public IRenderer Renderer { get; }
    public IWindow GetWindow() => _window;
    private readonly IWindow _window;

    public OpenGLTestApp()
    {
        var options = WindowOptions.Default;
        options.IsVisible = false;
        _window = Window.Create(options);
        _window.Initialize();
        GL = _window.CreateOpenGL();

        // Use our new Renderer implementation instead of TestRenderer
        Renderer = new TestRenderer(GL);
    }

    public OpenGLTestApp(IRenderer renderer)
    {
        var options = WindowOptions.Default;
        options.IsVisible = false;
        _window = Window.Create(options);
        _window.Initialize();
        GL = _window.CreateOpenGL();

        // Use our new Renderer implementation instead of TestRenderer
        Renderer = renderer ?? new TestRenderer(GL);
    }

    public byte[] CaptureFrame()
    {
        // Get the current viewport dimensions
        Span<int> viewport = stackalloc int[4];
        GL.GetInteger(GetPName.Viewport, viewport);

        int width = viewport[2];
        int height = viewport[3];

        // Create buffer to hold pixel data (RGBA format)
        var pixels = new byte[width * height * 4];

        // Read pixels from the front buffer using span
        GL.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels.AsSpan());

        return pixels;
    }

    public void Dispose() => _window?.Dispose();
}