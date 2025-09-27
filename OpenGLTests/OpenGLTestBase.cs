using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Nexus.GameEngine.Graphics;
using Xunit;

namespace OpenGLTests;

/// <summary>
/// Base class for OpenGL tests that provides access to the shared OpenGL context.
/// All OpenGL tests should inherit from this class and be in the "OpenGL" collection.
/// </summary>
[Collection("OpenGL")]
public abstract class OpenGLTestBase : IDisposable
{
    protected readonly OpenGLContextFixture _fixture;
    protected GL GL => _fixture.GL;
    protected IRenderer Renderer => _fixture.Renderer;
    protected IWindow Window => _fixture.Window;

    protected OpenGLTestBase(OpenGLContextFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

        // Reset OpenGL state before each test to ensure clean state
        _fixture.ResetState();
    }

    /// <summary>
    /// Captures the current frame buffer as pixel data.
    /// </summary>
    protected byte[] CaptureFrame() => _fixture.CaptureFrame();

    /// <summary>
    /// Manually reset OpenGL state if needed during a test.
    /// </summary>
    protected void ResetState() => _fixture.ResetState();

    /// <summary>
    /// Asserts that no OpenGL errors have occurred.
    /// </summary>
    protected void AssertNoGLErrors()
    {
        var error = GL.GetError();
        Assert.Equal(GLEnum.NoError, error);
    }

    /// <summary>
    /// Renders a simple test frame for visual verification.
    /// </summary>
    protected void RenderTestFrame()
    {
        Renderer.RenderFrame(16.67); // ~60 FPS delta time
        AssertNoGLErrors();
    }

    public virtual void Dispose()
    {
        // Individual tests don't dispose the fixture - it's shared across the collection
        // But we can do test-specific cleanup here if needed
    }
}