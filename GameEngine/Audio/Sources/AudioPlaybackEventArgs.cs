namespace Nexus.GameEngine.Audio;

public class AudioPlaybackEventArgs : EventArgs
{
    public IAudioSource AudioSource { get; }
    public IAudioClip AudioClip { get; }
    public float Position { get; }
    public DateTime Timestamp { get; }

    public AudioPlaybackEventArgs(IAudioSource audioSource, IAudioClip audioClip, float position)
    {
        AudioSource = audioSource;
        AudioClip = audioClip;
        Position = position;
        Timestamp = DateTime.UtcNow;
    }
}
