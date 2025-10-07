namespace Nexus.GameEngine.Animation;

/// <summary>
/// Provides standard animation duration constants for consistent timing across the engine.
/// These values are compile-time constants and cannot be changed at runtime.
/// </summary>
public static class AnimationDuration
{
    /// <summary>
    /// Fast animation duration (150ms).
    /// Use for quick UI feedback, hover effects, or subtle transitions.
    /// </summary>
    public const float Fast = 0.15f;

    /// <summary>
    /// Normal animation duration (250ms).
    /// Use for standard UI animations, layout changes, and most visual transitions.
    /// This is the recommended default for most animated properties.
    /// </summary>
    public const float Normal = 0.25f;

    /// <summary>
    /// Slow animation duration (400ms).
    /// Use for dramatic effects, large movements, or emphasis.
    /// </summary>
    public const float Slow = 0.4f;

    /// <summary>
    /// Very slow animation duration (600ms).
    /// Use for cinematic camera movements or major scene transitions.
    /// </summary>
    public const float VerySlow = 0.6f;

    /// <summary>
    /// Instant update (0ms).
    /// No animation, property changes take effect immediately (but still deferred to next UpdateAnimations call).
    /// This is the default when Duration is not specified in [ComponentProperty].
    /// </summary>
    public const float Instant = 0f;
}
