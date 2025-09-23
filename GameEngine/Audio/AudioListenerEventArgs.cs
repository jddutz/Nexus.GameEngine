namespace Nexus.GameEngine.Audio;

public class AudioListenerEventArgs(IAudioListener listener) : EventArgs
{
    public IAudioListener Listener { get; } = listener;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
