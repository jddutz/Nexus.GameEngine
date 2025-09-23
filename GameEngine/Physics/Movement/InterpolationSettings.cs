namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Defines interpolation settings for smooth movement.
/// </summary>
public struct InterpolationSettings
{
    public bool Enabled { get; set; }
    public InterpolationTypeEnum Type { get; set; }
    public float Speed { get; set; }
    public AnimationCurve Curve { get; set; }
    public bool UseTimeBasedInterpolation { get; set; }
    public static InterpolationSettings Default => new InterpolationSettings
    {
        Enabled = true,
        Type = InterpolationTypeEnum.Linear,
        Speed = 5.0f,
        UseTimeBasedInterpolation = true
    };
}
