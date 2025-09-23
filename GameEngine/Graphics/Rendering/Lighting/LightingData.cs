using Silk.NET.Maths;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics.Rendering.Lighting;

/// <summary>
/// Uniform buffer data for a single light
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightData
{
    /// <summary>
    /// Light position (w component indicates light type)
    /// </summary>
    public Vector4D<float> Position;

    /// <summary>
    /// Light direction (w component is range for point/spot lights)
    /// </summary>
    public Vector4D<float> Direction;

    /// <summary>
    /// Light color (RGB) and intensity (A)
    /// </summary>
    public Vector4D<float> ColorIntensity;

    /// <summary>
    /// Attenuation factors (constant, linear, quadratic, unused)
    /// </summary>
    public Vector4D<float> Attenuation;

    /// <summary>
    /// Spot light parameters (inner angle, outer angle, shadow bias, enabled flag)
    /// </summary>
    public Vector4D<float> SpotParams;

    /// <summary>
    /// Shadow map matrix (view-projection for shadow mapping)
    /// </summary>
    public Matrix4X4<float> ShadowMatrix;

    /// <summary>
    /// Creates light data from a Light object
    /// </summary>
    /// <param name="light">Source light</param>
    /// <param name="shadowMatrix">Shadow mapping matrix</param>
    /// <returns>GPU-compatible light data</returns>
    public static LightData FromLight(Light light, Matrix4X4<float> shadowMatrix = default)
    {
        var position = new Vector4D<float>(light.Position, (float)light.Type);
        var direction = new Vector4D<float>(light.Direction, light.Range);
        var colorIntensity = new Vector4D<float>(light.Color, light.Intensity);
        var attenuation = new Vector4D<float>(light.AttenuationFactors, 0.0f);
        var spotParams = new Vector4D<float>(
            MathF.Cos(light.InnerConeAngle),
            MathF.Cos(light.OuterConeAngle),
            light.ShadowBias,
            light.IsEnabled ? 1.0f : 0.0f
        );

        return new LightData
        {
            Position = position,
            Direction = direction,
            ColorIntensity = colorIntensity,
            Attenuation = attenuation,
            SpotParams = spotParams,
            ShadowMatrix = shadowMatrix
        };
    }

    /// <summary>
    /// Size of the LightData structure in bytes
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf<LightData>();
}

/// <summary>
/// Uniform buffer data for scene lighting
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceneLightingData
{
    /// <summary>
    /// Maximum number of lights supported in shaders
    /// </summary>
    public const int MaxLights = 32;

    /// <summary>
    /// Ambient light color and intensity
    /// </summary>
    public Vector4D<float> AmbientLight;

    /// <summary>
    /// Camera position for specular calculations
    /// </summary>
    public Vector4D<float> CameraPosition;

    /// <summary>
    /// Number of active lights
    /// </summary>
    public int ActiveLightCount;

    /// <summary>
    /// Shadow mapping enabled flag
    /// </summary>
    public int ShadowsEnabled;

    /// <summary>
    /// IBL (Image-Based Lighting) enabled flag
    /// </summary>
    public int IBLEnabled;

    /// <summary>
    /// Padding for alignment
    /// </summary>
    public int Padding;

    /// <summary>
    /// Array of light data
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxLights)]
    public LightData[] Lights;

    /// <summary>
    /// Creates default scene lighting data
    /// </summary>
    /// <returns>Default lighting configuration</returns>
    public static SceneLightingData CreateDefault()
    {
        return new SceneLightingData
        {
            AmbientLight = new Vector4D<float>(0.1f, 0.1f, 0.1f, 1.0f),
            CameraPosition = Vector4D<float>.Zero,
            ActiveLightCount = 0,
            ShadowsEnabled = 1,
            IBLEnabled = 0,
            Padding = 0,
            Lights = new LightData[MaxLights]
        };
    }

    /// <summary>
    /// Size of the SceneLightingData structure in bytes
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf<SceneLightingData>();
}

/// <summary>
/// Uniform buffer data for PBR material properties
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MaterialData
{
    /// <summary>
    /// Base color factor (RGBA)
    /// </summary>
    public Vector4D<float> AlbedoFactor;

    /// <summary>
    /// Emissive color and unused component
    /// </summary>
    public Vector4D<float> EmissiveFactor;

    /// <summary>
    /// Material parameters (metallic, roughness, normal scale, occlusion strength)
    /// </summary>
    public Vector4D<float> MaterialParams;

    /// <summary>
    /// Alpha parameters (cutoff, mode, double-sided, unused)
    /// </summary>
    public Vector4D<float> AlphaParams;

    /// <summary>
    /// Texture flags (albedo, normal, metallic-roughness, occlusion, emissive, unused, unused, unused)
    /// </summary>
    public Vector4D<float> TextureFlags1;
    public Vector4D<float> TextureFlags2;

    /// <summary>
    /// Creates material data from a PBR material
    /// </summary>
    /// <param name="material">Source material</param>
    /// <returns>GPU-compatible material data</returns>
    public static MaterialData FromPBRMaterial(PBRMaterial material)
    {
        var materialParams = new Vector4D<float>(
            material.MetallicFactor,
            material.RoughnessFactor,
            material.NormalScale,
            material.OcclusionStrength
        );

        var alphaParams = new Vector4D<float>(
            material.AlphaCutoff,
            (float)material.AlphaMode,
            material.DoubleSided ? 1.0f : 0.0f,
            0.0f
        );

        var textureFlags1 = new Vector4D<float>(
            material.AlbedoTextureId.HasValue ? 1.0f : 0.0f,
            material.NormalTextureId.HasValue ? 1.0f : 0.0f,
            material.MetallicRoughnessTextureId.HasValue ? 1.0f : 0.0f,
            material.OcclusionTextureId.HasValue ? 1.0f : 0.0f
        );

        var textureFlags2 = new Vector4D<float>(
            material.EmissiveTextureId.HasValue ? 1.0f : 0.0f,
            0.0f, 0.0f, 0.0f
        );

        return new MaterialData
        {
            AlbedoFactor = material.AlbedoFactor,
            EmissiveFactor = new Vector4D<float>(material.EmissiveFactor, 0.0f),
            MaterialParams = materialParams,
            AlphaParams = alphaParams,
            TextureFlags1 = textureFlags1,
            TextureFlags2 = textureFlags2
        };
    }

    /// <summary>
    /// Size of the MaterialData structure in bytes
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf<MaterialData>();
}