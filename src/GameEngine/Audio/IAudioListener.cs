using Silk.NET.Maths;

namespace Nexus.GameEngine.Audio;

public interface IAudioListener
{
    Vector3D<float> Position { get; set; }
    Vector3D<float> Velocity { get; set; }
    Vector3D<float> Forward { get; set; }
    Vector3D<float> Up { get; set; }
    float MasterVolume { get; set; }
    float DopplerScale { get; set; }
    float SpeedOfSound { get; set; }
    float DistanceFactor { get; set; }
    float RolloffScale { get; set; }
    bool IsPrimaryListener { get; set; }
    AudioProcessingMode ProcessingMode { get; set; }
    HRTFProfile HRTFProfile { get; set; }
    bool EnvironmentalEffectsEnabled { get; set; }
    EnvironmentalPreset EnvironmentalPreset { get; set; }
    Matrix4X4<float> OrientationMatrix { get; }
    bool IsActive { get; }
    event EventHandler<ListenerPositionChangedEventArgs> PositionChanged;
    event EventHandler<ListenerOrientationChangedEventArgs> OrientationChanged;
    event EventHandler<AudioListenerEventArgs> BecamePrimary;
    event EventHandler<AudioListenerEventArgs> LostPrimary;
    event EventHandler<EnvironmentalChangedEventArgs> EnvironmentalChanged;
    void SetOrientation(Vector3D<float> forward, Vector3D<float> up);
    void SetOrientation(Matrix4X4<float> rotation);
    void SetOrientation(float pitch, float yaw, float roll);
    void SetTransform(Vector3D<float> position, Vector3D<float> forward, Vector3D<float> up);
    float CalculatePerceivedVolume(Vector3D<float> sourcePosition, float sourceVolume, float minDistance, float maxDistance, float rolloffFactor);
    float CalculatePerceivedPan(Vector3D<float> sourcePosition);
    float CalculateDopplerShift(Vector3D<float> sourcePosition, Vector3D<float> sourceVelocity);
    void MakePrimary();
    void ApplyEnvironmentalEffects();
    void UpdateHRTF();
}

