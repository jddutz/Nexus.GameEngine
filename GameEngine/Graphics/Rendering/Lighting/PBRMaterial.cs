using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Rendering.Lighting;

/// <summary>
/// Represents a physically-based material
/// </summary>
public class PBRMaterial
{
    /// <summary>
    /// Gets or sets the base color (albedo) texture ID
    /// </summary>
    public uint? AlbedoTextureId { get; set; }

    /// <summary>
    /// Gets or sets the base color factor
    /// </summary>
    public Vector4D<float> AlbedoFactor { get; set; } = Vector4D<float>.One;

    /// <summary>
    /// Gets or sets the metallic-roughness texture ID (R=unused, G=roughness, B=metallic)
    /// </summary>
    public uint? MetallicRoughnessTextureId { get; set; }

    /// <summary>
    /// Gets or sets the metallic factor (0.0 = dielectric, 1.0 = metallic)
    /// </summary>
    public float MetallicFactor { get; set; } = 0.0f;

    /// <summary>
    /// Gets or sets the roughness factor (0.0 = mirror, 1.0 = fully rough)
    /// </summary>
    public float RoughnessFactor { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the normal map texture ID
    /// </summary>
    public uint? NormalTextureId { get; set; }

    /// <summary>
    /// Gets or sets the normal map scale
    /// </summary>
    public float NormalScale { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the occlusion texture ID (R channel)
    /// </summary>
    public uint? OcclusionTextureId { get; set; }

    /// <summary>
    /// Gets or sets the occlusion strength
    /// </summary>
    public float OcclusionStrength { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the emissive texture ID
    /// </summary>
    public uint? EmissiveTextureId { get; set; }

    /// <summary>
    /// Gets or sets the emissive factor
    /// </summary>
    public Vector3D<float> EmissiveFactor { get; set; } = Vector3D<float>.Zero;

    /// <summary>
    /// Gets or sets the alpha cutoff for alpha testing
    /// </summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the alpha mode
    /// </summary>
    public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

    /// <summary>
    /// Gets or sets whether the material is double-sided
    /// </summary>
    public bool DoubleSided { get; set; } = false;

    /// <summary>
    /// Creates a default PBR material
    /// </summary>
    /// <returns>Default material with neutral properties</returns>
    public static PBRMaterial CreateDefault()
    {
        return new PBRMaterial
        {
            AlbedoFactor = new Vector4D<float>(0.8f, 0.8f, 0.8f, 1.0f),
            MetallicFactor = 0.0f,
            RoughnessFactor = 0.5f,
            NormalScale = 1.0f,
            OcclusionStrength = 1.0f,
            EmissiveFactor = Vector3D<float>.Zero,
            AlphaCutoff = 0.5f,
            AlphaMode = AlphaMode.Opaque,
            DoubleSided = false
        };
    }

    /// <summary>
    /// Creates a metallic material
    /// </summary>
    /// <param name="baseColor">Base color</param>
    /// <param name="roughness">Roughness factor</param>
    /// <returns>Metallic material</returns>
    public static PBRMaterial CreateMetallic(Vector3D<float> baseColor, float roughness = 0.1f)
    {
        return new PBRMaterial
        {
            AlbedoFactor = new Vector4D<float>(baseColor, 1.0f),
            MetallicFactor = 1.0f,
            RoughnessFactor = Math.Clamp(roughness, 0.04f, 1.0f), // Prevent fully smooth metals
            NormalScale = 1.0f,
            OcclusionStrength = 1.0f,
            EmissiveFactor = Vector3D<float>.Zero,
            AlphaMode = AlphaMode.Opaque
        };
    }

    /// <summary>
    /// Creates a dielectric material
    /// </summary>
    /// <param name="baseColor">Base color</param>
    /// <param name="roughness">Roughness factor</param>
    /// <returns>Dielectric material</returns>
    public static PBRMaterial CreateDielectric(Vector3D<float> baseColor, float roughness = 0.5f)
    {
        return new PBRMaterial
        {
            AlbedoFactor = new Vector4D<float>(baseColor, 1.0f),
            MetallicFactor = 0.0f,
            RoughnessFactor = Math.Clamp(roughness, 0.0f, 1.0f),
            NormalScale = 1.0f,
            OcclusionStrength = 1.0f,
            EmissiveFactor = Vector3D<float>.Zero,
            AlphaMode = AlphaMode.Opaque
        };
    }

    /// <summary>
    /// Creates an emissive material
    /// </summary>
    /// <param name="emissiveColor">Emissive color</param>
    /// <param name="intensity">Emissive intensity</param>
    /// <returns>Emissive material</returns>
    public static PBRMaterial CreateEmissive(Vector3D<float> emissiveColor, float intensity = 1.0f)
    {
        return new PBRMaterial
        {
            AlbedoFactor = Vector4D<float>.One,
            MetallicFactor = 0.0f,
            RoughnessFactor = 1.0f,
            EmissiveFactor = emissiveColor * intensity,
            AlphaMode = AlphaMode.Opaque
        };
    }
}

/// <summary>
/// Defines how alpha values are interpreted
/// </summary>
public enum AlphaMode
{
    /// <summary>
    /// Alpha value is ignored, material is fully opaque
    /// </summary>
    Opaque = 0,

    /// <summary>
    /// Alpha value is used for alpha testing with cutoff
    /// </summary>
    Mask = 1,

    /// <summary>
    /// Alpha value is used for alpha blending
    /// </summary>
    Blend = 2
}