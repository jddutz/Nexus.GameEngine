namespace Nexus.GameEngine.Testing;

/// <summary>
/// Service for sampling pixel colors from the rendered output.
/// This is a testing-only service that impacts performance and should not be used in production.
/// </summary>
/// <remarks>
/// Architecture:
/// 1. Set Enabled = true to initialize resources and hook into renderer events
/// 2. Configure sample coordinates via SampleCoordinates property
/// 3. Call Activate() to start sampling frames
/// 4. Call Deactivate() to stop sampling
/// 5. Retrieve results via GetResults()
/// Each call to GetResults() returns all samples captured since last call.
/// </remarks>
public interface IPixelSampler
{
    /// <summary>
    /// Gets or sets the coordinates to sample on each frame.
    /// Changes take effect on the next frame.
    /// </summary>
    Vector2D<int>[] SampleCoordinates { get; set; }
    
    /// <summary>
    /// Activates pixel sampling. While active, samples pixels on every rendered frame.
    /// </summary>
    void Activate();
    
    /// <summary>
    /// Deactivates pixel sampling. Stops sampling frames.
    /// </summary>
    void Deactivate();

    /// <summary>
    /// Gets all pixel sample results captured since the last call to GetResults().
    /// Returns an array of sample sets, where each set corresponds to one captured frame.
    /// Clears the internal results buffer.
    /// </summary>
    /// <returns>Array of sample sets (one per captured frame)</returns>
    Vector4D<float>?[][] GetResults();

    /// <summary>
    /// Gets whether pixel sampling is currently enabled and available.
    /// Sampling may not be available during initialization or if disabled for performance.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Enables or disables pixel sampling.
    /// When disabled, performance impact is minimized.
    /// </summary>
    bool Enabled { get; set; }
}
