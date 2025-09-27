using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Lighting;

namespace Tests.Graphics.Rendering.Lighting;

public class LightingDataTests
{
    [Fact]
    public void LightData_FromLight_DirectionalLight_ShouldConvertCorrectly()
    {
        // Arrange
        var light = Light.CreateDirectional(new Vector3D<float>(0, -1, 0), new Vector3D<float>(1, 1, 0.8f), 2.0f);
        var shadowMatrix = Matrix4X4<float>.Identity;

        // Act
        var lightData = LightData.FromLight(light, shadowMatrix);

        // Assert
        Assert.Equal(new Vector4D<float>(light.Position, (float)LightType.Directional), lightData.Position);
        Assert.Equal(new Vector4D<float>(light.Direction, light.Range), lightData.Direction);
        Assert.Equal(new Vector4D<float>(light.Color, light.Intensity), lightData.ColorIntensity);
        Assert.Equal(new Vector4D<float>(light.AttenuationFactors, 0.0f), lightData.Attenuation);
        Assert.Equal(shadowMatrix, lightData.ShadowMatrix);
    }

    [Fact]
    public void LightData_FromLight_PointLight_ShouldConvertCorrectly()
    {
        // Arrange
        var light = Light.CreatePoint(new Vector3D<float>(1, 2, 3), new Vector3D<float>(1, 0, 0), 1.5f, 15.0f);

        // Act
        var lightData = LightData.FromLight(light);

        // Assert
        Assert.Equal(new Vector4D<float>(light.Position, (float)LightType.Point), lightData.Position);
        Assert.Equal(new Vector4D<float>(light.Direction, light.Range), lightData.Direction);
        Assert.Equal(new Vector4D<float>(light.Color, light.Intensity), lightData.ColorIntensity);

        var expectedAttenuation = light.AttenuationFactors;
        Assert.Equal(new Vector4D<float>(expectedAttenuation, 0.0f), lightData.Attenuation);
    }

    [Fact]
    public void LightData_FromLight_SpotLight_ShouldConvertCorrectly()
    {
        // Arrange
        var light = Light.CreateSpot(
            new Vector3D<float>(0, 5, 0),
            new Vector3D<float>(0, -1, 0),
            new Vector3D<float>(0, 1, 0),
            intensity: 3.0f,
            range: 20.0f,
            innerAngle: MathF.PI / 8.0f,
            outerAngle: MathF.PI / 6.0f
        );

        // Act
        var lightData = LightData.FromLight(light);

        // Assert
        Assert.Equal(new Vector4D<float>(light.Position, (float)LightType.Spot), lightData.Position);
        Assert.Equal(new Vector4D<float>(light.Direction, light.Range), lightData.Direction);
        Assert.Equal(new Vector4D<float>(light.Color, light.Intensity), lightData.ColorIntensity);

        var expectedSpotParams = new Vector4D<float>(
            MathF.Cos(light.InnerConeAngle),
            MathF.Cos(light.OuterConeAngle),
            light.ShadowBias,
            1.0f // enabled
        );
        Assert.Equal(expectedSpotParams, lightData.SpotParams);
    }

    [Fact]
    public void LightData_FromLight_DisabledLight_ShouldSetEnabledFlag()
    {
        // Arrange
        var light = Light.CreatePoint(Vector3D<float>.Zero, Vector3D<float>.One);
        light.IsEnabled = false;

        // Act
        var lightData = LightData.FromLight(light);

        // Assert
        Assert.Equal(0.0f, lightData.SpotParams.W); // Should be 0 for disabled
    }

    [Fact]
    public void SceneLightingData_CreateDefault_ShouldInitializeCorrectly()
    {
        // Act
        var sceneLighting = SceneLightingData.CreateDefault();

        // Assert
        Assert.Equal(new Vector4D<float>(0.1f, 0.1f, 0.1f, 1.0f), sceneLighting.AmbientLight);
        Assert.Equal(Vector4D<float>.Zero, sceneLighting.CameraPosition);
        Assert.Equal(0, sceneLighting.ActiveLightCount);
        Assert.Equal(1, sceneLighting.ShadowsEnabled);
        Assert.Equal(0, sceneLighting.IBLEnabled);
        Assert.NotNull(sceneLighting.Lights);
        Assert.Equal(SceneLightingData.MaxLights, sceneLighting.Lights.Length);
    }

    [Fact]
    public void MaterialData_FromPBRMaterial_ShouldConvertCorrectly()
    {
        // Arrange
        var material = new PBRMaterial
        {
            AlbedoFactor = new Vector4D<float>(0.8f, 0.6f, 0.4f, 1.0f),
            MetallicFactor = 0.2f,
            RoughnessFactor = 0.7f,
            NormalScale = 1.5f,
            OcclusionStrength = 0.8f,
            EmissiveFactor = new Vector3D<float>(0.1f, 0.2f, 0.0f),
            AlphaCutoff = 0.3f,
            AlphaMode = AlphaMode.Mask,
            DoubleSided = true,
            AlbedoTextureId = 1,
            NormalTextureId = 2
        };

        // Act
        var materialData = MaterialData.FromPBRMaterial(material);

        // Assert
        Assert.Equal(material.AlbedoFactor, materialData.AlbedoFactor);
        Assert.Equal(new Vector4D<float>(material.EmissiveFactor, 0.0f), materialData.EmissiveFactor);

        var expectedMaterialParams = new Vector4D<float>(
            material.MetallicFactor,
            material.RoughnessFactor,
            material.NormalScale,
            material.OcclusionStrength
        );
        Assert.Equal(expectedMaterialParams, materialData.MaterialParams);

        var expectedAlphaParams = new Vector4D<float>(
            material.AlphaCutoff,
            (float)material.AlphaMode,
            1.0f, // double-sided
            0.0f
        );
        Assert.Equal(expectedAlphaParams, materialData.AlphaParams);

        // Check texture flags
        Assert.Equal(1.0f, materialData.TextureFlags1.X); // albedo texture present
        Assert.Equal(1.0f, materialData.TextureFlags1.Y); // normal texture present
        Assert.Equal(0.0f, materialData.TextureFlags1.Z); // metallic-roughness texture not present
        Assert.Equal(0.0f, materialData.TextureFlags1.W); // occlusion texture not present
    }

    [Fact]
    public void MaterialData_FromPBRMaterial_NoTextures_ShouldSetFlagsCorrectly()
    {
        // Arrange
        var material = PBRMaterial.CreateDefault();

        // Act
        var materialData = MaterialData.FromPBRMaterial(material);

        // Assert
        Assert.Equal(Vector4D<float>.Zero, materialData.TextureFlags1);
        Assert.Equal(Vector4D<float>.Zero, materialData.TextureFlags2);
    }

    [Fact]
    public void LightData_SizeInBytes_ShouldBeCorrect()
    {
        // Act & Assert
        Assert.True(LightData.SizeInBytes > 0);
        Assert.True(LightData.SizeInBytes % 16 == 0); // Should be 16-byte aligned for GPU
    }

    [Fact]
    public void SceneLightingData_SizeInBytes_ShouldBeCorrect()
    {
        // Act & Assert
        Assert.True(SceneLightingData.SizeInBytes > 0);
        Assert.Equal(32, SceneLightingData.MaxLights);
    }

    [Fact]
    public void MaterialData_SizeInBytes_ShouldBeCorrect()
    {
        // Act & Assert
        Assert.True(MaterialData.SizeInBytes > 0);
        Assert.True(MaterialData.SizeInBytes % 16 == 0); // Should be 16-byte aligned for GPU
    }
}