using Nexus.GameEngine.Graphics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics;
using Xunit;

namespace OpenGLTests;

/// <summary>
/// OpenGL Context Fixture for sequential OpenGL tests.
/// This ensures all OpenGL tests run sequentially with a shared, properly managed context.
/// </summary>
public class OpenGLContextFixture : IDisposable
{
    private readonly IWindow _window;
    private readonly GL _gl;
    private readonly IRenderer _renderer;
    private bool _disposed = false;

    public GL GL => _gl;
    public IRenderer Renderer => _renderer;
    public IWindow Window => _window;

    public OpenGLContextFixture()
    {
        Debug.WriteLine("Initializing OpenGL Context Fixture");

        // Create window for OpenGL context
        var options = WindowOptions.Default;
        options.IsVisible = false;
        options.Title = "OpenGL Test Context";
        options.Size = new(800, 600);

        _window = Silk.NET.Windowing.Window.Create(options);
        _window.Initialize();

        // Make context current and create GL instance
        _window.MakeCurrent();
        _gl = _window.CreateOpenGL();

        // Verify OpenGL context is working
        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            throw new InvalidOperationException($"OpenGL context creation failed with error: {error}");
        }

        // Create test renderer
        _renderer = new TestRenderer(_gl);

        Debug.WriteLine("OpenGL Context Fixture initialized successfully.");
    }

    /// <summary>
    /// Resets OpenGL state to a clean, known state between tests.
    /// </summary>
    public void ResetState()
    {
        try
        {
            Debug.WriteLine("Resetting OpenGL state");

            // Ensure context is current
            _window.MakeCurrent();

            // Clear all buffers with neutral values
            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // Reset common OpenGL state
            _gl.Disable(EnableCap.DepthTest);
            _gl.Disable(EnableCap.Blend);
            _gl.Disable(EnableCap.CullFace);
            _gl.Disable(EnableCap.ScissorTest);
            _gl.Disable(EnableCap.StencilTest);

            // Reset to default values
            _gl.DepthFunc(DepthFunction.Less);
            _gl.DepthMask(true);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.CullFace(TriangleFace.Back);
            _gl.FrontFace(FrontFaceDirection.Ccw);
            _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

            // Reset viewport to window size
            var size = _window.Size;
            _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);

            // Check for errors after reset
            var error = _gl.GetError();
            if (error != GLEnum.NoError)
            {
                Debug.WriteLine($"Warning: OpenGL error after state reset: {error}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during OpenGL state reset: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Captures the current frame buffer as pixel data.
    /// </summary>
    public byte[] CaptureFrame()
    {
        // Ensure context is current
        _window.MakeCurrent();

        // Get viewport dimensions
        Span<int> viewport = stackalloc int[4];
        _gl.GetInteger(GetPName.Viewport, viewport);

        int width = viewport[2];
        int height = viewport[3];

        // Create buffer for pixel data (RGBA format)
        var pixels = new byte[width * height * 4];

        // Read pixels from framebuffer
        _gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels.AsSpan());

        return pixels;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Debug.WriteLine("Disposing OpenGL Context Fixture");

            try
            {
                if (_renderer is IDisposable disposableRenderer)
                {
                    disposableRenderer.Dispose();
                }
                _window?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during OpenGL fixture disposal: {ex.Message}");
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Test collection definition for OpenGL tests.
/// All tests in this collection will run sequentially and share the same OpenGL context.
/// </summary>
[CollectionDefinition("OpenGL")]
public class OpenGLCollection : ICollectionFixture<OpenGLContextFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}