namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Behavior interface for components that can play animations.
/// Implement this interface for components with time-based visual changes.
/// </summary>
public interface IAnimatable
{
    /// <summary>
    /// The currently playing animation, if any.
    /// Null if no animation is currently playing.
    /// </summary>
    string? CurrentAnimation { get; }

    /// <summary>
    /// Whether an animation is currently playing.
    /// </summary>
    bool IsAnimating { get; }

    /// <summary>
    /// The current playback speed multiplier for animations.
    /// 1.0 is normal speed, 2.0 is double speed, 0.5 is half speed.
    /// </summary>
    float AnimationSpeed { get; set; }

    /// <summary>
    /// Whether the current animation should loop when it reaches the end.
    /// </summary>
    bool LoopAnimation { get; set; }

    /// <summary>
    /// Start playing the specified animation.
    /// </summary>
    /// <param name="animationName">The name of the animation to play</param>
    /// <param name="loop">Whether the animation should loop</param>
    void PlayAnimation(string animationName, bool loop = false);

    /// <summary>
    /// Stop the currently playing animation.
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Pause the currently playing animation.
    /// Can be resumed with ResumeAnimation().
    /// </summary>
    void PauseAnimation();

    /// <summary>
    /// Resume a paused animation.
    /// </summary>
    void ResumeAnimation();

    /// <summary>
    /// Event raised when an animation completes (reaches the end without looping).
    /// </summary>
    event Action<string>? AnimationCompleted;
}