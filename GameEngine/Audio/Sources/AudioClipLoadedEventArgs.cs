namespace Nexus.GameEngine.Audio;

public class AudioClipLoadedEventArgs : EventArgs
{
    public IAudioClip AudioClip { get; }
    public bool Success { get; }
    public string ErrorMessage { get; }
    public DateTime Timestamp { get; }

    public AudioClipLoadedEventArgs(IAudioClip audioClip, bool success, string? errorMessage = null)
    {
        AudioClip = audioClip;
        Success = success;
        ErrorMessage = errorMessage ?? string.Empty;
        Timestamp = DateTime.UtcNow;
    }
}
