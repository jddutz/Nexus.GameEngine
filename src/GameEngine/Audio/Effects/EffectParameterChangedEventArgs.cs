namespace Nexus.GameEngine.Audio;

/// <summary>
/// Provides data for effect parameter changed events.
/// </summary>
public class EffectParameterChangedEventArgs(string parameterName, float oldValue, float newValue, IAudioEffect audioEffect) : EventArgs
{
    /// <summary>
    /// Gets the name of the parameter that changed.
    /// </summary>
    public string ParameterName { get; } = parameterName;

    /// <summary>
    /// Gets the previous value of the parameter.
    /// </summary>
    public float OldValue { get; } = oldValue;

    /// <summary>
    /// Gets the new value of the parameter.
    /// </summary>
    public float NewValue { get; } = newValue;

    /// <summary>
    /// Gets the audio effect that owns the parameter.
    /// </summary>
    public IAudioEffect AudioEffect { get; } = audioEffect;

    /// <summary>
    /// Gets the timestamp of the parameter change.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}