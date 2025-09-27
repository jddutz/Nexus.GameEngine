using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Lighting;

namespace Tests.Graphics.Rendering.Lighting;

public class PBRMaterialTests
{
    [Fact]
    public void CreateDefault_ShouldReturnValidMaterial()
    {
        // Act
        var material = PBRMaterial.CreateDefault();

        // Assert
        Assert.Equal(new Vector4D<float>(0.8f, 0.8f, 0.8f, 1.0f), material.AlbedoFactor);
        Assert.Equal(0.0f, material.MetallicFactor);
        Assert.Equal(0.5f, material.RoughnessFactor);
        Assert.Equal(1.0f, material.NormalScale);
        Assert.Equal(1.0f, material.OcclusionStrength);
        Assert.Equal(Vector3D<float>.Zero, material.EmissiveFactor);
        Assert.Equal(0.5f, material.AlphaCutoff);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
        Assert.False(material.DoubleSided);
    }

    [Fact]
    public void CreateMetallic_ShouldSetMetallicProperties()
    {
        // Arrange
        var baseColor = new Vector3D<float>(0.7f, 0.7f, 0.7f);
        var roughness = 0.1f;

        // Act
        var material = PBRMaterial.CreateMetallic(baseColor, roughness);

        // Assert
        Assert.Equal(new Vector4D<float>(baseColor, 1.0f), material.AlbedoFactor);
        Assert.Equal(1.0f, material.MetallicFactor);
        Assert.Equal(roughness, material.RoughnessFactor);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
    }

    [Fact]
    public void CreateMetallic_WithZeroRoughness_ShouldClampToMinimum()
    {
        // Arrange
        var baseColor = Vector3D<float>.One;
        var roughness = 0.0f;

        // Act
        var material = PBRMaterial.CreateMetallic(baseColor, roughness);

        // Assert
        Assert.Equal(0.04f, material.RoughnessFactor); // Should be clamped to minimum
    }

    [Fact]
    public void CreateDielectric_ShouldSetDielectricProperties()
    {
        // Arrange
        var baseColor = new Vector3D<float>(0.2f, 0.5f, 0.8f);
        var roughness = 0.3f;

        // Act
        var material = PBRMaterial.CreateDielectric(baseColor, roughness);

        // Assert
        Assert.Equal(new Vector4D<float>(baseColor, 1.0f), material.AlbedoFactor);
        Assert.Equal(0.0f, material.MetallicFactor);
        Assert.Equal(roughness, material.RoughnessFactor);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
    }

    [Fact]
    public void CreateEmissive_ShouldSetEmissiveProperties()
    {
        // Arrange
        var emissiveColor = new Vector3D<float>(1.0f, 0.5f, 0.0f);
        var intensity = 2.0f;

        // Act
        var material = PBRMaterial.CreateEmissive(emissiveColor, intensity);

        // Assert
        Assert.Equal(Vector4D<float>.One, material.AlbedoFactor);
        Assert.Equal(0.0f, material.MetallicFactor);
        Assert.Equal(1.0f, material.RoughnessFactor);
        Assert.Equal(emissiveColor * intensity, material.EmissiveFactor);
        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
    }

    [Theory]
    [InlineData(AlphaMode.Opaque)]
    [InlineData(AlphaMode.Mask)]
    [InlineData(AlphaMode.Blend)]
    public void AlphaMode_ShouldBeSettable(AlphaMode alphaMode)
    {
        // Arrange
        var material = PBRMaterial.CreateDefault();

        // Act
        material.AlphaMode = alphaMode;

        // Assert
        Assert.Equal(alphaMode, material.AlphaMode);
    }

    [Fact]
    public void TextureProperties_ShouldBeNullByDefault()
    {
        // Act
        var material = PBRMaterial.CreateDefault();

        // Assert
        Assert.Null(material.AlbedoTextureId);
        Assert.Null(material.NormalTextureId);
        Assert.Null(material.MetallicRoughnessTextureId);
        Assert.Null(material.OcclusionTextureId);
        Assert.Null(material.EmissiveTextureId);
    }

    [Fact]
    public void TextureProperties_ShouldBeSettable()
    {
        // Arrange
        var material = PBRMaterial.CreateDefault();

        // Act
        material.AlbedoTextureId = 1;
        material.NormalTextureId = 2;
        material.MetallicRoughnessTextureId = 3;
        material.OcclusionTextureId = 4;
        material.EmissiveTextureId = 5;

        // Assert
        Assert.Equal(1u, material.AlbedoTextureId);
        Assert.Equal(2u, material.NormalTextureId);
        Assert.Equal(3u, material.MetallicRoughnessTextureId);
        Assert.Equal(4u, material.OcclusionTextureId);
        Assert.Equal(5u, material.EmissiveTextureId);
    }
}