namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Interface for fluent pipeline configuration.
/// Implement this interface to create custom pipeline builders.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// Builds the pipeline using the configured settings and returns the pipeline handle.
    /// The pipeline is registered with the pipeline manager and cached for reuse.
    /// </summary>
    /// <returns>Pipeline handle containing Pipeline and PipelineLayout, ready for use in draw commands.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required fields are missing.</exception>
    PipelineHandle Build(string name);
}
