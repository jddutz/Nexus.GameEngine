using Silk.NET.Maths;

namespace Nexus.GameEngine.Audio;

public interface IAudioSource
{
    string? ErrorMessage { get; }
    float Volume { get; set; }
    float Pitch { get; set; }
    float Pan { get; set; }
    bool Loop { get; set; }
    bool Is3D { get; set; }
    Vector3D<float> Position { get; set; }
    Vector3D<float> Velocity { get; set; }
    float MinDistance { get; set; }
    float MaxDistance { get; set; }
    float RolloffFactor { get; set; }
    int Priority { get; set; }
    AudioCategoryEnum Category { get; set; }
    AudioPlaybackStateEnum PlaybackState { get; }
    float PlaybackPosition { get; }
    float Duration { get; }
    IAudioClip CurrentClip { get; }
    bool IsPlaying { get; }
    bool IsPaused { get; }
    bool IsStopped { get; }
    event EventHandler<AudioPlaybackEventArgs> PlaybackStarted;
    event EventHandler<AudioPlaybackEventArgs> PlaybackPaused;
    event EventHandler<AudioPlaybackEventArgs> PlaybackResumed;
    event EventHandler<AudioPlaybackEventArgs> PlaybackStopped;
    event EventHandler<AudioPlaybackEventArgs> PlaybackCompleted;
    event EventHandler<AudioClipLoadedEventArgs> ClipLoaded;
    event EventHandler<AudioErrorEventArgs> PlaybackError;
    void LoadClip(IAudioClip clip);
    void LoadClip(string filePath);
    void Play();
    void Play(IAudioClip clip);
    void Play(float position);
    void Pause();
    void Resume();
    void Stop();
    void Seek(float position);
    void FadeVolume(float targetVolume, float duration);
    void FadeIn(float duration);
    void FadeOut(float duration);
    void PlayOneShot(IAudioClip clip, float volume = 1.0f);
}
