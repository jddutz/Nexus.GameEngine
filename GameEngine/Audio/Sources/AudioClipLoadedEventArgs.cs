namespace Nexus.GameEngine.Audio;

public class AudioClipLoadedEventArgs(IAudioClip audioClip, bool success, string? errorMessage = null) : EventArgs
{
    public IAudioClip AudioClip { get; } = audioClip;
    public bool Success { get; } = success;
    public string ErrorMessage { get; } = errorMessage ?? string.Empty;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
