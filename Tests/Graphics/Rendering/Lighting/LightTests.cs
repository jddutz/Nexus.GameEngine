using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Lighting;

namespace Tests.Graphics.Rendering.Lighting;

public class LightTests
{
    [Fact]
    public void CreateDirectional_ShouldSetCorrectProperties()
    {
        // Arrange
        var direction = new Vector3D<float>(0, -1, 0);
        var color = new Vector3D<float>(1, 1, 0.8f);
        var intensity = 2.0f;

        // Act
        var light = Light.CreateDirectional(direction, color, intensity);

        // Assert
        Assert.Equal(LightType.Directional, light.Type);
        Assert.Equal(Vector3D.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
        Assert.True(light.IsEnabled);
        Assert.True(light.CastsShadows);
    }

    [Fact]
    public void CreatePoint_ShouldSetCorrectProperties()
    {
        // Arrange
        var position = new Vector3D<float>(1, 2, 3);
        var color = new Vector3D<float>(1, 0, 0);
        var intensity = 1.5f;
        var range = 15.0f;

        // Act
        var light = Light.CreatePoint(position, color, intensity, range);

        // Assert
        Assert.Equal(LightType.Point, light.Type);
        Assert.Equal(position, light.Position);
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
        Assert.Equal(range, light.Range);
        Assert.True(light.IsEnabled);
    }

    [Fact]
    public void CreateSpot_ShouldSetCorrectProperties()
    {
        // Arrange
        var position = new Vector3D<float>(0, 5, 0);
        var direction = new Vector3D<float>(0, -1, 0);
        var color = new Vector3D<float>(0, 1, 0);
        var intensity = 3.0f;
        var range = 20.0f;
        var innerAngle = MathF.PI / 8.0f;
        var outerAngle = MathF.PI / 6.0f;

        // Act
        var light = Light.CreateSpot(position, direction, color, intensity, range, innerAngle, outerAngle);

        // Assert
        Assert.Equal(LightType.Spot, light.Type);
        Assert.Equal(position, light.Position);
        Assert.Equal(Vector3D.Normalize(direction), light.Direction);
        Assert.Equal(color, light.Color);
        Assert.Equal(intensity, light.Intensity);
        Assert.Equal(range, light.Range);
        Assert.Equal(innerAngle, light.InnerConeAngle);
        Assert.Equal(outerAngle, light.OuterConeAngle);
    }

    [Fact]
    public void AttenuationFactors_DirectionalLight_ShouldReturnNoAttenuation()
    {
        // Arrange
        var light = Light.CreateDirectional(Vector3D<float>.UnitY, Vector3D<float>.One);

        // Act
        var attenuation = light.AttenuationFactors;

        // Assert
        Assert.Equal(1.0f, attenuation.X); // Constant
        Assert.Equal(0.0f, attenuation.Y); // Linear
        Assert.Equal(0.0f, attenuation.Z); // Quadratic
    }

    [Fact]
    public void AttenuationFactors_PointLight_ShouldCalculateCorrectly()
    {
        // Arrange
        var range = 10.0f;
        var light = Light.CreatePoint(Vector3D<float>.Zero, Vector3D<float>.One, 1.0f, range);

        // Act
        var attenuation = light.AttenuationFactors;

        // Assert
        Assert.Equal(1.0f, attenuation.X); // Constant
        Assert.Equal(2.0f / range, attenuation.Y); // Linear
        Assert.Equal(1.0f / (range * range), attenuation.Z); // Quadratic
    }

    [Theory]
    [InlineData(LightType.Directional)]
    [InlineData(LightType.Point)]
    [InlineData(LightType.Spot)]
    public void Light_DefaultProperties_ShouldBeCorrect(LightType lightType)
    {
        // Arrange & Act
        var light = new Light { Type = lightType };

        // Assert
        Assert.Equal(Vector3D<float>.One, light.Color);
        Assert.Equal(1.0f, light.Intensity);
        Assert.Equal(10.0f, light.Range);
        Assert.Equal(MathF.PI / 6.0f, light.InnerConeAngle);
        Assert.Equal(MathF.PI / 4.0f, light.OuterConeAngle);
        Assert.True(light.CastsShadows);
        Assert.Equal(1024u, light.ShadowMapSize);
        Assert.Equal(0.001f, light.ShadowBias);
        Assert.True(light.IsEnabled);
    }
}