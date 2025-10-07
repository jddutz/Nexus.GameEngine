namespace Nexus.GameEngine.Audio;

/// <summary>
/// Provides data for audio effect error events.
/// </summary>
public class AudioEffectErrorEventArgs(string errorMessage, IAudioEffect audioEffect, Exception? exception = null) : EventArgs
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; } = errorMessage;

    /// <summary>
    /// Gets the exception that caused the error (if any).
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// Gets the audio effect that encountered the error.
    /// </summary>
    public IAudioEffect AudioEffect { get; } = audioEffect;

    /// <summary>
    /// Gets the timestamp of the error.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}