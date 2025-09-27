using Silk.NET.OpenGL;
using Xunit;

namespace OpenGLTests;

/// <summary>
/// Tests to verify that OpenGL state is properly managed between tests.
/// These tests demonstrate that each test starts with a clean OpenGL state.
/// </summary>
public class StateManagementTests : OpenGLTestBase
{
    public StateManagementTests(OpenGLContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void FirstTest_ModifiesState_IsolatedFromOtherTests()
    {
        // Arrange & Act - Modify OpenGL state
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.ClearColor(1.0f, 0.5f, 0.0f, 1.0f); // Orange

        // Assert - Verify our changes took effect
        var depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
        var blendEnabled = GL.IsEnabled(EnableCap.Blend);

        Assert.True(depthTestEnabled);
        Assert.True(blendEnabled);
        AssertNoGLErrors();

        // Note: State will be automatically reset before the next test runs
    }

    [Fact]
    public void SecondTest_StateIsResetFromPreviousTest()
    {
        // Arrange & Act - Check initial state (should be reset from previous test)
        var depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
        var blendEnabled = GL.IsEnabled(EnableCap.Blend);

        // Assert - These should be false because state was reset
        Assert.False(depthTestEnabled);
        Assert.False(blendEnabled);

        // Verify clear color was reset to black
        Span<float> clearColor = stackalloc float[4];
        GL.GetFloat(GetPName.ColorClearValue, clearColor);

        Assert.Equal(0.0f, clearColor[0], precision: 3); // Red
        Assert.Equal(0.0f, clearColor[1], precision: 3); // Green  
        Assert.Equal(0.0f, clearColor[2], precision: 3); // Blue
        Assert.InRange(clearColor[3], 0.0f, 1.0f); // Alpha

        AssertNoGLErrors();
    }

    [Fact]
    public void ThirdTest_RendererAlsoStartsWithCleanState()
    {
        // Arrange - First modify state via GL
        GL.Enable(EnableCap.CullFace);
        Assert.True(GL.IsEnabled(EnableCap.CullFace));

        // Act - Render a frame (this shouldn't affect state reset between tests)
        RenderTestFrame();

        // Assert - Verify rendering worked without error
        AssertNoGLErrors();
    }

    [Fact]
    public void FourthTest_StateWasResetAfterRendering()
    {
        // Arrange & Act - Check that cull face is disabled (should be reset from previous test)
        var cullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);

        // Assert - Should be disabled due to state reset
        Assert.False(cullFaceEnabled);
        AssertNoGLErrors();
    }

    [Fact]
    public void ManualStateReset_WorksWithinSameTest()
    {
        // Arrange & Act - Make some changes
        GL.Enable(EnableCap.StencilTest);
        Assert.True(GL.IsEnabled(EnableCap.StencilTest));

        // Act - Manual reset within the same test
        ResetState();

        // Assert - State should be reset
        var stencilTestEnabled = GL.IsEnabled(EnableCap.StencilTest);
        Assert.False(stencilTestEnabled);
        AssertNoGLErrors();
    }

    [Fact]
    public void FrameCapture_WorksWithCurrentContext()
    {
        // Arrange - Render something to capture
        RenderTestFrame();

        // Act - Capture frame
        var frameData = CaptureFrame();

        // Assert - Should have captured pixel data
        Assert.NotNull(frameData);
        Assert.True(frameData.Length > 0);
        Assert.True(frameData.Length % 4 == 0); // Should be RGBA format

        AssertNoGLErrors();
    }
}