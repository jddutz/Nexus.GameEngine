namespace Nexus.GameEngine.Audio;

/// <summary>
/// Represents different types of audio effects.
/// </summary>
public enum AudioEffectType
{
    /// <summary>
    /// No effect or passthrough.
    /// </summary>
    None,

    /// <summary>
    /// Reverb effect for spatial ambience.
    /// </summary>
    Reverb,

    /// <summary>
    /// Echo effect with delay and feedback.
    /// </summary>
    Echo,

    /// <summary>
    /// Delay effect with configurable timing.
    /// </summary>
    Delay,

    /// <summary>
    /// Chorus effect for thickening sound.
    /// </summary>
    Chorus,

    /// <summary>
    /// Flanger effect for sweeping frequency modulation.
    /// </summary>
    Flanger,

    /// <summary>
    /// Phaser effect for phase shifting.
    /// </summary>
    Phaser,

    /// <summary>
    /// Distortion effect for overdrive and clipping.
    /// </summary>
    Distortion,

    /// <summary>
    /// Compressor for dynamic range control.
    /// </summary>
    Compressor,

    /// <summary>
    /// Limiter for preventing audio clipping.
    /// </summary>
    Limiter,

    /// <summary>
    /// Equalizer for frequency shaping.
    /// </summary>
    Equalizer,

    /// <summary>
    /// Low-pass filter.
    /// </summary>
    LowPassFilter,

    /// <summary>
    /// High-pass filter.
    /// </summary>
    HighPassFilter,

    /// <summary>
    /// Band-pass filter.
    /// </summary>
    BandPassFilter,

    /// <summary>
    /// Notch filter for removing specific frequencies.
    /// </summary>
    NotchFilter,

    /// <summary>
    /// Bitcrusher for digital distortion.
    /// </summary>
    Bitcrusher,

    /// <summary>
    /// Ring modulator for frequency multiplication.
    /// </summary>
    RingModulator,

    /// <summary>
    /// Tremolo for amplitude modulation.
    /// </summary>
    Tremolo,

    /// <summary>
    /// Vibrato for pitch modulation.
    /// </summary>
    Vibrato,

    /// <summary>
    /// Auto-wah effect.
    /// </summary>
    AutoWah,

    /// <summary>
    /// Pitch shifter for changing pitch without affecting speed.
    /// </summary>
    PitchShifter,

    /// <summary>
    /// Time stretcher for changing speed without affecting pitch.
    /// </summary>
    TimeStretcher,

    /// <summary>
    /// Granular synthesis effect.
    /// </summary>
    Granular,

    /// <summary>
    /// Convolution reverb using impulse responses.
    /// </summary>
    ConvolutionReverb,

    /// <summary>
    /// 3D spatialization effect.
    /// </summary>
    Spatialization,

    /// <summary>
    /// Custom effect with user-defined processing.
    /// </summary>
    Custom
}