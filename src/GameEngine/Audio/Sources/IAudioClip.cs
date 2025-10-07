namespace Nexus.GameEngine.Audio;

public interface IAudioClip
{
    string Name { get; }
    float Duration { get; }
    int SampleRate { get; }
    int Channels { get; }
    AudioFormatEnum Format { get; }
    bool IsLoaded { get; }
    string FilePath { get; }
}
