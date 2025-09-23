namespace Nexus.GameEngine.Audio;

public interface IAudioEffect
{
    /// <summary>
    /// Gets or sets the intensity or wet/dry mix of the effect (0.0 to 1.0).
    /// </summary>
    float EffectIntensity { get; set; }

    /// <summary>
    /// Gets or sets the type of audio effect.
    /// </summary>
    AudioEffectType EffectType { get; set; }

    /// <summary>
    /// Gets or sets the priority of this effect in the processing chain.
    /// </summary>
    int EffectPriority { get; set; }

    /// <summary>
    /// Gets or sets whether this effect should bypass processing.
    /// </summary>
    bool Bypass { get; set; }

    /// <summary>
    /// Gets or sets the audio categories this effect applies to.
    /// </summary>
    AudioCategoryEnum TargetCategories { get; set; }

    /// <summary>
    /// Gets or sets custom parameters for the effect.
    /// </summary>
    Dictionary<string, float> Parameters { get; set; }

    /// <summary>
    /// Gets whether the effect is currently processing audio.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Gets the current CPU usage of this effect as a percentage.
    /// </summary>
    float CPUUsage { get; }

    /// <summary>
    /// Gets the latency introduced by this effect in milliseconds.
    /// </summary>
    float Latency { get; }

    /// <summary>
    /// Occurs when the effect starts processing.
    /// </summary>
    event EventHandler<AudioEffectEventArgs> EffectStarted;

    /// <summary>
    /// Occurs when the effect stops processing.
    /// </summary>
    event EventHandler<AudioEffectEventArgs> EffectStopped;

    /// <summary>
    /// Occurs when effect parameters change.
    /// </summary>
    event EventHandler<EffectParameterChangedEventArgs> ParameterChanged;

    /// <summary>
    /// Occurs when there's an error in effect processing.
    /// </summary>
    event EventHandler<AudioEffectErrorEventArgs> EffectError;

    /// <summary>
    /// Applies the effect to the provided audio data.
    /// </summary>
    /// <param name="inputData">The input audio data.</param>
    /// <param name="outputData">The output buffer for processed audio.</param>
    /// <param name="sampleCount">The number of samples to process.</param>
    void ProcessAudio(float[] inputData, float[] outputData, int sampleCount);

    /// <summary>
    /// Applies the effect to stereo audio data.
    /// </summary>
    /// <param name="leftInput">The left channel input data.</param>
    /// <param name="rightInput">The right channel input data.</param>
    /// <param name="leftOutput">The left channel output buffer.</param>
    /// <param name="rightOutput">The right channel output buffer.</param>
    /// <param name="sampleCount">The number of samples to process.</param>
    void ProcessStereoAudio(float[] leftInput, float[] rightInput,
                           float[] leftOutput, float[] rightOutput, int sampleCount);

    /// <summary>
    /// Sets a parameter value for the effect.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The value to set.</param>
    void SetParameter(string parameterName, float value);

    /// <summary>
    /// Gets a parameter value from the effect.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The current value of the parameter.</returns>
    float GetParameter(string parameterName);

    /// <summary>
    /// Resets the effect to its default state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Loads a preset configuration for the effect.
    /// </summary>
    /// <param name="presetName">The name of the preset to load.</param>
    void LoadPreset(string presetName);

    /// <summary>
    /// Saves the current effect configuration as a preset.
    /// </summary>
    /// <param name="presetName">The name to save the preset as.</param>
    void SavePreset(string presetName);

    /// <summary>
    /// Gets the available presets for this effect.
    /// </summary>
    /// <returns>An array of preset names.</returns>
    string[] GetAvailablePresets();

    /// <summary>
    /// Validates that all effect parameters are within acceptable ranges.
    /// </summary>
    /// <returns>True if all parameters are valid, false otherwise.</returns>
    bool ValidateParameters();
}