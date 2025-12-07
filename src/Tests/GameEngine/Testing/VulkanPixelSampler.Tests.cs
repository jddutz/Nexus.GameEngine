using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace Tests.GameEngine.Testing;

public class VulkanPixelSamplerTests
{
    private readonly Mock<IGraphicsContext> _graphicsContextMock;
    private readonly Mock<ISwapChain> _swapChainMock;
    private readonly Mock<ICommandPoolManager> _commandPoolManagerMock;
    private readonly VulkanPixelSampler _sampler;

    public VulkanPixelSamplerTests()
    {
        _graphicsContextMock = new Mock<IGraphicsContext>();
        _swapChainMock = new Mock<ISwapChain>();
        _commandPoolManagerMock = new Mock<ICommandPoolManager>();

        _sampler = new VulkanPixelSampler(
            _graphicsContextMock.Object,
            _swapChainMock.Object,
            _commandPoolManagerMock.Object);
    }

    [Fact]
    public void PixelSampler_IsAvailable_ReturnsFalseWhenDisabled()
    {
        // Arrange
        _sampler.Enabled = false;

        // Act
        bool isAvailable = _sampler.IsAvailable;

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public void PixelSampler_SampleCoordinates_CanBeSetAndRetrieved()
    {
        // Arrange
        var coordinates = new[]
        {
            new Vector2D<int>(10, 20),
            new Vector2D<int>(30, 40)
        };

        // Act
        _sampler.SampleCoordinates = coordinates;

        // Assert
        Assert.Equal(coordinates, _sampler.SampleCoordinates);
    }

    [Fact]
    public void PixelSampler_GetResults_ReturnsEmptyArrayWhenNotActive()
    {
        // Arrange
        // Don't enable sampler to avoid Vulkan resource creation in unit tests
        _sampler.SampleCoordinates = [new Vector2D<int>(10, 10)];

        // Act
        var results = _sampler.GetResults();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void PixelSampler_Activate_Deactivate_WorkWithoutEnabled()
    {
        // Arrange
        // Don't enable sampler to avoid Vulkan resource creation

        // Act
        _sampler.Activate();
        // Deactivate should work without throwing
        _sampler.Deactivate();

        // Assert
        // No exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void PixelSampler_SampleCoordinates_SetNull_MakesEmpty()
    {
        // Arrange
        _sampler.SampleCoordinates = null!;

        // Act
        var coords = _sampler.SampleCoordinates;

        // Assert
        Assert.NotNull(coords);
        Assert.Empty(coords);
    }

    [Fact]
    public void PixelSampler_OnBeforePresent_CapturesResultsWhenActive()
    {
        // Arrange
        _sampler.SampleCoordinates = [new Vector2D<int>(0, 0)];
        _sampler.Activate();

        // Act: call the private OnBeforePresent handler via reflection (avoid subscribing to the real event)
        var args = new Nexus.GameEngine.Graphics.PresentEventArgs { Image = default, ImageIndex = 0 };
        var onBefore = typeof(VulkanPixelSampler).GetMethod("OnBeforePresent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        onBefore.Invoke(_sampler, [this, args]);

        // Assert: results should contain one captured frame with the same length as sample coords
        var results = _sampler.GetResults();
        Assert.Single(results);
        Assert.Single(results[0]);
        // Since sampler isn't enabled (no Vulkan resources), the pixel value should be null
        Assert.Null(results[0][0]);
    }

    [Fact]
    public void PixelSampler_SrgbConversion_PrivateMethods_BehaveAsExpected()
    {
        // Use reflection to access the private static methods
        var type = typeof(VulkanPixelSampler);
    var channelMethod = type.GetMethod("SrgbChannelToLinear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
    var vectorMethod = type.GetMethod("SrgbToLinear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        // low value -> linear = srgb/12.92 branch
        var low = (float)channelMethod.Invoke(null, [0.02f])!;
        Assert.Equal(0.02f / 12.92f, low, 6);

        // high value -> gamma branch (approx > 0.04045)
        var high = (float)channelMethod.Invoke(null, [0.5f])!;
        Assert.True(high > 0.0f);

        // vector conversion should preserve alpha and convert channels
        var vec = new Silk.NET.Maths.Vector4D<float>(0.5f, 0.02f, 0.5f, 0.75f);
        var converted = (Silk.NET.Maths.Vector4D<float>)vectorMethod.Invoke(null, [vec])!;
        Assert.Equal(0.75f, converted.W);
        Assert.NotEqual(vec.X, converted.X);
    }

    [Fact]
    public void PixelSampler_Dispose_CanBeCalledMultipleTimes()
    {
        // Act/Assert: calling Dispose multiple times should not throw
        _sampler.Dispose();
        _sampler.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void PixelSampler_Enabled_Property_RemoveSubscription_WhenPreviouslyEnabled()
    {
        // Arrange: capture remove handler via Moq
        EventHandler<Nexus.GameEngine.Graphics.PresentEventArgs>? removed = null;
        _swapChainMock.SetupRemove(s => s.BeforePresent -= It.IsAny<EventHandler<Nexus.GameEngine.Graphics.PresentEventArgs>>())
            .Callback<EventHandler<Nexus.GameEngine.Graphics.PresentEventArgs>>(h => removed = h);

        // Set private _enabled to true so the setter will run the 'disable' branch
    var enabledField = typeof(VulkanPixelSampler).GetField("_enabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        enabledField.SetValue(_sampler, true);

        // Act: set Enabled to false which should trigger removal
        _sampler.Enabled = false;

        // Assert
        Assert.NotNull(removed);
    }
}