using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Behavior interface for components that emit light in 3D scenes.
/// Implement this interface for light sources like point lights, directional lights, spotlights, etc.
/// </summary>
public interface ILightSource
{
    /// <summary>
    /// The type of light source.
    /// </summary>
    LightTypeEnum LightType { get; set; }

    /// <summary>
    /// The color of the light.
    /// </summary>
    Vector4D<float> LightColor { get; set; }

    /// <summary>
    /// The intensity/brightness of the light.
    /// 1.0 is normal intensity, higher values create brighter light.
    /// </summary>
    float Intensity { get; set; }

    /// <summary>
    /// The range/distance the light affects (for point and spot lights).
    /// Objects beyond this distance are not lit by this light source.
    /// </summary>
    float Range { get; set; }

    /// <summary>
    /// The direction the light is pointing (for directional and spot lights).
    /// Not used for point lights.
    /// </summary>
    Vector3D<float> Direction { get; set; }

    /// <summary>
    /// The inner cone angle for spot lights (in radians).
    /// Not used for other light types.
    /// </summary>
    float SpotInnerAngle { get; set; }

    /// <summary>
    /// The outer cone angle for spot lights (in radians).
    /// Not used for other light types.
    /// </summary>
    float SpotOuterAngle { get; set; }

    /// <summary>
    /// Whether this light casts shadows.
    /// </summary>
    bool CastsShadows { get; set; }

    /// <summary>
    /// The attenuation factors for distance-based light falloff.
    /// X = constant, Y = linear, Z = quadratic attenuation.
    /// </summary>
    Vector3D<float> Attenuation { get; set; }

    /// <summary>
    /// Whether the light source is currently enabled and should contribute to lighting.
    /// </summary>
    bool IsEnabled { get; set; }
}