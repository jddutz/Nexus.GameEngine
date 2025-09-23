namespace Nexus.GameEngine.Audio;

public class EnvironmentalChangedEventArgs(EnvironmentalPreset oldPreset, EnvironmentalPreset newPreset) : EventArgs
{
    public EnvironmentalPreset OldPreset { get; } = oldPreset;
    public EnvironmentalPreset NewPreset { get; } = newPreset;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
