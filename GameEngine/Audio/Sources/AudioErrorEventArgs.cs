namespace Nexus.GameEngine.Audio;

public class AudioErrorEventArgs(string errorMessage, IAudioSource audioSource, Exception? exception = null) : EventArgs
{
    public string ErrorMessage { get; } = errorMessage;
    public Exception? Exception { get; } = exception;
    public IAudioSource AudioSource { get; } = audioSource;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
