namespace Nexus.GameEngine.Audio;

public class AudioPlaybackEventArgs(IAudioSource audioSource, IAudioClip audioClip, float position) : EventArgs
{
    public IAudioSource AudioSource { get; } = audioSource;
    public IAudioClip AudioClip { get; } = audioClip;
    public float Position { get; } = position;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
