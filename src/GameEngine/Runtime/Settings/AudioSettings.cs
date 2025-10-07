namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// Audio settings and preferences.
/// </summary>
public class AudioSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public float VoiceVolume { get; set; } = 1.0f;
    public bool Muted { get; set; } = false;
    public string AudioDevice { get; set; } = "Default";
}