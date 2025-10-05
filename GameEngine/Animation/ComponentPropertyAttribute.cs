namespace Nexus.GameEngine.Animation;

/// <summary>
/// Marks a backing field for property generation with optional animation support.
/// Apply this attribute to private fields to generate public properties with deferred updates.
/// </summary>
/// <example>
/// [ComponentProperty]
/// private float _fontSize = 12f;
/// // Generates: public float FontSize { get; set; }
/// 
/// [ComponentProperty(Duration = 0.3f, Interpolation = InterpolationMode.Cubic)]
/// private Vector4D&lt;float&gt; _color = Colors.White;
/// // Generates animated property with smooth interpolation
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ComponentPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the animation duration in seconds.
    /// Default is 0 (instant update between frames).
    /// </summary>
    public float Duration { get; set; } = 0f;

    /// <summary>
    /// Gets or sets the interpolation mode for the animation.
    /// Default is Linear.
    /// </summary>
    public InterpolationMode Interpolation { get; set; } = InterpolationMode.Linear;
}
