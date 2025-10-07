namespace Nexus.GameEngine.Audio;

/// <summary>
/// Provides data for audio effect events.
/// </summary>
public class AudioEffectEventArgs(IAudioEffect audioEffect, AudioEffectType effectType) : EventArgs
{
    /// <summary>
    /// Gets the audio effect that triggered the event.
    /// </summary>
    public IAudioEffect AudioEffect { get; } = audioEffect;

    /// <summary>
    /// Gets the effect type.
    /// </summary>
    public AudioEffectType EffectType { get; } = effectType;

    /// <summary>
    /// Gets the timestamp of the effect event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}