namespace Nexus.GameEngine.Audio;

/// <summary>
/// Provides common parameter names for audio effects.
/// </summary>
public static class AudioEffectParameters
{
    // Common parameters
    public const string WetDryMix = "WetDryMix";
    public const string Intensity = "Intensity";
    public const string Gain = "Gain";

    // Reverb parameters
    public const string RoomSize = "RoomSize";
    public const string DecayTime = "DecayTime";
    public const string Damping = "Damping";
    public const string EarlyReflections = "EarlyReflections";
    public const string LateReverb = "LateReverb";

    // Delay/Echo parameters
    public const string DelayTime = "DelayTime";
    public const string Feedback = "Feedback";
    public const string LowCutoff = "LowCutoff";
    public const string HighCutoff = "HighCutoff";

    // Modulation parameters
    public const string Rate = "Rate";
    public const string Depth = "Depth";
    public const string Shape = "Shape";

    // Filter parameters
    public const string Cutoff = "Cutoff";
    public const string Resonance = "Resonance";
    public const string FilterType = "FilterType";

    // Compressor parameters
    public const string Threshold = "Threshold";
    public const string Ratio = "Ratio";
    public const string Attack = "Attack";
    public const string Release = "Release";
    public const string MakeupGain = "MakeupGain";

    // Distortion parameters
    public const string Drive = "Drive";
    public const string Tone = "Tone";
    public const string Level = "Level";

    // EQ parameters
    public const string LowGain = "LowGain";
    public const string MidGain = "MidGain";
    public const string HighGain = "HighGain";
    public const string LowFreq = "LowFreq";
    public const string MidFreq = "MidFreq";
    public const string HighFreq = "HighFreq";
}