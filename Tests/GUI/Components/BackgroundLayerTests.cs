using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.Graphics.Resources;
using Nexus.GameEngine.GUI.Components;
using Silk.NET.Maths;
using static Tests.Components.RuntimeComponentTestHelpers;
using ComponentTextureWrapMode = Nexus.GameEngine.GUI.Components.TextureWrapMode;

namespace Tests.GUI.Components;

/// <summary>
/// Tests for BackgroundLayer component covering template configuration, validation, and rendering integration.
/// </summary>
public class BackgroundLayerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IResourceManager> _mockResourceManager;
    private readonly BackgroundLayer _backgroundLayer;

    public BackgroundLayerTests()
    {
        _mockLogger = CreateMockLogger();
        _mockResourceManager = new Mock<IResourceManager>();

        _backgroundLayer = new BackgroundLayer(_mockResourceManager.Object)
        {
            Logger = _mockLogger.Object
        };
    }

    #region Template Configuration Tests

    [Fact]
    public void Template_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var template = new BackgroundLayer.Template();

        // Assert
        Assert.Equal(MaterialType.SolidColor, template.MaterialType);
        Assert.Equal(new Vector4D<float>(0.0f, 0.0f, 0.0f, 1.0f), template.BackgroundColor);
        Assert.Null(template.ImageAsset);
        Assert.Null(template.ProceduralParameters);
        Assert.Equal(new Vector4D<float>(1.0f, 1.0f, 1.0f, 1.0f), template.Tint);
        Assert.Equal(1.0f, template.Saturation);
        Assert.Equal(1.0f, template.Fade);
        Assert.Equal(BlendMode.Replace, template.BlendMode);
        Assert.Equal(ComponentTextureWrapMode.Clamp, template.TextureWrapMode);
        Assert.Equal(new Vector2D<float>(1.0f, 1.0f), template.TextureScale);
        Assert.Equal(new Vector2D<float>(0.0f, 0.0f), template.TextureOffset);
    }

    [Fact]
    public void Configure_WithValidTemplate_SetsPropertiesCorrectly()
    {
        // Arrange
        var template = new BackgroundLayer.Template
        {
            MaterialType = MaterialType.ImageAsset,
            BackgroundColor = new Vector4D<float>(0.5f, 0.2f, 0.8f, 0.9f),
            Tint = new Vector4D<float>(0.8f, 0.9f, 1.0f, 1.0f),
            Saturation = 1.5f,
            Fade = 0.7f,
            BlendMode = BlendMode.Alpha,
            TextureWrapMode = ComponentTextureWrapMode.Repeat,
            TextureScale = new Vector2D<float>(2.0f, 3.0f),
            TextureOffset = new Vector2D<float>(0.1f, 0.2f)
        };

        // Act
        _backgroundLayer.Configure(template);

        // Assert - Test that internal template state is updated via getter methods
        Assert.Equal(MaterialType.ImageAsset, _backgroundLayer.GetMaterialType());
        Assert.Equal(new Vector4D<float>(0.5f, 0.2f, 0.8f, 0.9f), _backgroundLayer.GetBackgroundColor());
        Assert.Equal(1.5f, _backgroundLayer.GetSaturation());
        Assert.Equal(0.7f, _backgroundLayer.GetFade());
    }

    [Fact]
    public void Configure_WithInvalidTemplate_ThrowsArgumentException()
    {
        // Arrange
        var invalidTemplate = new RuntimeComponent.Template();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _backgroundLayer.Configure(invalidTemplate));
        Assert.Contains("Expected Template", exception.Message);
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(MaterialType.SolidColor)]
    [InlineData(MaterialType.ImageAsset)]
    [InlineData(MaterialType.ProceduralTexture)]
    public void GetMaterialType_ReturnsConfiguredValue(MaterialType materialType)
    {
        // Arrange
        var template = new BackgroundLayer.Template { MaterialType = materialType };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.Equal(materialType, _backgroundLayer.GetMaterialType());
    }

    [Theory]
    [InlineData(0.0f, 0.0f, 0.0f, 1.0f)]
    [InlineData(1.0f, 0.5f, 0.2f, 0.8f)]
    [InlineData(0.3f, 0.7f, 0.9f, 1.0f)]
    public void GetBackgroundColor_ReturnsConfiguredValue(float r, float g, float b, float a)
    {
        // Arrange
        var color = new Vector4D<float>(r, g, b, a);
        var template = new BackgroundLayer.Template { BackgroundColor = color };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.Equal(color, _backgroundLayer.GetBackgroundColor());
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void GetSaturation_ReturnsConfiguredValue(float saturation)
    {
        // Arrange
        var template = new BackgroundLayer.Template { Saturation = saturation };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.Equal(saturation, _backgroundLayer.GetSaturation());
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    [InlineData(1.0f)]
    public void GetFade_ReturnsConfiguredValue(float fade)
    {
        // Arrange
        var template = new BackgroundLayer.Template { Fade = fade };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.Equal(fade, _backgroundLayer.GetFade());
    }

    #endregion

    #region IRenderable Interface Tests

    [Theory]
    [InlineData(true, 1.0f, true)]
    [InlineData(true, 0.5f, true)]
    [InlineData(true, 0.01f, true)]
    [InlineData(true, 0.0f, false)]
    [InlineData(false, 1.0f, false)]
    [InlineData(false, 0.0f, false)]
    public void ShouldRender_WithDifferentVisibilityAndFade_ReturnsExpected(bool isVisible, float fade, bool expectedShouldRender)
    {
        // Arrange
        var template = new BackgroundLayer.Template { Fade = fade };
        _backgroundLayer.Configure(template);
        _backgroundLayer.IsVisible = isVisible;

        // Act & Assert
        Assert.Equal(expectedShouldRender, _backgroundLayer.ShouldRender);
    }

    [Fact]
    public void RenderPriority_Returns_BackgroundPriority()
    {
        // Act & Assert
        Assert.Equal(0, _backgroundLayer.RenderPriority);
    }

    [Fact]
    public void BoundingBox_Returns_EmptyBox()
    {
        // Act & Assert
        var expected = new Box3D<float>(Vector3D<float>.Zero, Vector3D<float>.Zero);
        Assert.Equal(expected, _backgroundLayer.BoundingBox);
    }

    [Fact]
    public void RenderPassFlags_Returns_AllPasses()
    {
        // Act & Assert
        Assert.Equal(0xFFFFFFFFu, _backgroundLayer.RenderPassFlags);
    }

    [Fact]
    public void ShouldRenderChildren_Returns_True()
    {
        // Act & Assert
        Assert.True(_backgroundLayer.ShouldRenderChildren);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithValidSolidColorConfiguration_ReturnsTrue()
    {
        // Arrange
        var template = new BackgroundLayer.Template
        {
            MaterialType = MaterialType.SolidColor,
            BackgroundColor = new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f),
            Saturation = 1.0f,
            Fade = 1.0f
        };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.True(_backgroundLayer.Validate());
    }

    [Theory]
    [InlineData(-1.0f)]
    [InlineData(-0.1f)]
    public void Validate_WithInvalidSaturation_ReturnsFalse(float invalidSaturation)
    {
        // Arrange
        var template = new BackgroundLayer.Template { Saturation = invalidSaturation };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.False(_backgroundLayer.Validate());
        Assert.Contains(_backgroundLayer.ValidationErrors, e => e.Message.Contains("Saturation"));
    }

    [Theory]
    [InlineData(-1.0f)]
    [InlineData(1.1f)]
    public void Validate_WithInvalidFade_ReturnsFalse(float invalidFade)
    {
        // Arrange
        var template = new BackgroundLayer.Template { Fade = invalidFade };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.False(_backgroundLayer.Validate());
        Assert.Contains(_backgroundLayer.ValidationErrors, e => e.Message.Contains("Fade"));
    }

    [Fact]
    public void Validate_WithImageAssetButNoAssetReference_ReturnsFalse()
    {
        // Arrange
        var template = new BackgroundLayer.Template
        {
            MaterialType = MaterialType.ImageAsset,
            ImageAsset = null! // Missing required asset reference
        };
        _backgroundLayer.Configure(template);

        // Act & Assert
        Assert.False(_backgroundLayer.Validate());
        Assert.Contains(_backgroundLayer.ValidationErrors, e => e.Message.Contains("ImageAsset"));
    }

    #endregion

    #region Component Lifecycle Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsVisible_SetAndGet_WorksCorrectly(bool visibility)
    {
        // Act
        _backgroundLayer.IsVisible = visibility;

        // Assert
        Assert.Equal(visibility, _backgroundLayer.IsVisible);
    }

    [Fact]
    public void Dispose_CleansUpComponentScopedResources()
    {
        // Arrange
        var template = new BackgroundLayer.Template { MaterialType = MaterialType.SolidColor };
        _backgroundLayer.Configure(template);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _backgroundLayer.Dispose());
        Assert.Null(exception);
    }

    #endregion


}