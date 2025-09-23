namespace Nexus.GameEngine.Audio;

public class AudioErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; }
    public Exception? Exception { get; }
    public IAudioSource AudioSource { get; }
    public DateTime Timestamp { get; }

    public AudioErrorEventArgs(string errorMessage, IAudioSource audioSource, Exception? exception = null)
    {
        ErrorMessage = errorMessage;
        Exception = exception;
        AudioSource = audioSource;
        Timestamp = DateTime.UtcNow;
    }
}
