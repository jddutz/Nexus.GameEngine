using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Rendering.Lighting;

/// <summary>
/// Represents a light source in the scene
/// </summary>
public class Light
{
    /// <summary>
    /// Gets or sets the unique identifier for this light
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the type of light
    /// </summary>
    public LightType Type { get; set; }

    /// <summary>
    /// Gets or sets the light position (for point and spot lights)
    /// </summary>
    public Vector3D<float> Position { get; set; }

    /// <summary>
    /// Gets or sets the light direction (for directional and spot lights)
    /// </summary>
    public Vector3D<float> Direction { get; set; }

    /// <summary>
    /// Gets or sets the light color
    /// </summary>
    public Vector3D<float> Color { get; set; } = Vector3D<float>.One;

    /// <summary>
    /// Gets or sets the light intensity
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the light range (for point and spot lights)
    /// </summary>
    public float Range { get; set; } = 10.0f;

    /// <summary>
    /// Gets or sets the inner cone angle in radians (for spot lights)
    /// </summary>
    public float InnerConeAngle { get; set; } = MathF.PI / 6.0f; // 30 degrees

    /// <summary>
    /// Gets or sets the outer cone angle in radians (for spot lights)
    /// </summary>
    public float OuterConeAngle { get; set; } = MathF.PI / 4.0f; // 45 degrees

    /// <summary>
    /// Gets or sets whether this light casts shadows
    /// </summary>
    public bool CastsShadows { get; set; } = true;

    /// <summary>
    /// Gets or sets the shadow map size (power of 2)
    /// </summary>
    public uint ShadowMapSize { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the shadow bias to prevent shadow acne
    /// </summary>
    public float ShadowBias { get; set; } = 0.001f;

    /// <summary>
    /// Gets or sets whether this light is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the calculated attenuation factors for distance-based falloff
    /// </summary>
    public Vector3D<float> AttenuationFactors => CalculateAttenuationFactors();

    /// <summary>
    /// Creates a new directional light
    /// </summary>
    /// <param name="direction">Light direction</param>
    /// <param name="color">Light color</param>
    /// <param name="intensity">Light intensity</param>
    /// <returns>Configured directional light</returns>
    public static Light CreateDirectional(Vector3D<float> direction, Vector3D<float> color, float intensity = 1.0f)
    {
        return new Light
        {
            Type = LightType.Directional,
            Direction = Vector3D.Normalize(direction),
            Color = color,
            Intensity = intensity
        };
    }

    /// <summary>
    /// Creates a new point light
    /// </summary>
    /// <param name="position">Light position</param>
    /// <param name="color">Light color</param>
    /// <param name="intensity">Light intensity</param>
    /// <param name="range">Light range</param>
    /// <returns>Configured point light</returns>
    public static Light CreatePoint(Vector3D<float> position, Vector3D<float> color, float intensity = 1.0f, float range = 10.0f)
    {
        return new Light
        {
            Type = LightType.Point,
            Position = position,
            Color = color,
            Intensity = intensity,
            Range = range
        };
    }

    /// <summary>
    /// Creates a new spot light
    /// </summary>
    /// <param name="position">Light position</param>
    /// <param name="direction">Light direction</param>
    /// <param name="color">Light color</param>
    /// <param name="intensity">Light intensity</param>
    /// <param name="range">Light range</param>
    /// <param name="innerAngle">Inner cone angle in radians</param>
    /// <param name="outerAngle">Outer cone angle in radians</param>
    /// <returns>Configured spot light</returns>
    public static Light CreateSpot(Vector3D<float> position, Vector3D<float> direction, Vector3D<float> color,
        float intensity = 1.0f, float range = 10.0f,
        float innerAngle = MathF.PI / 6.0f, float outerAngle = MathF.PI / 4.0f)
    {
        return new Light
        {
            Type = LightType.Spot,
            Position = position,
            Direction = Vector3D.Normalize(direction),
            Color = color,
            Intensity = intensity,
            Range = range,
            InnerConeAngle = innerAngle,
            OuterConeAngle = outerAngle
        };
    }

    private Vector3D<float> CalculateAttenuationFactors()
    {
        if (Type == LightType.Directional)
            return new Vector3D<float>(1.0f, 0.0f, 0.0f); // No attenuation for directional lights

        // Calculate attenuation factors based on range
        // Using physically-based inverse square law with linear and quadratic components
        float constant = 1.0f;
        float linear = 2.0f / Range;
        float quadratic = 1.0f / (Range * Range);

        return new Vector3D<float>(constant, linear, quadratic);
    }
}