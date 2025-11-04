using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace Tests;

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
        _sampler.SampleCoordinates = new[] { new Vector2D<int>(10, 10) };

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
}