namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Defines a reusable graphics pipeline configuration.
/// Pipeline definitions are static, immutable, and shared across component instances.
/// Use via PipelineDefinitions static class (e.g., PipelineDefinitions.UIElement).
/// </summary>
public sealed class PipelineDefinition
{
    /// <summary>
    /// Unique name for this pipeline (used as cache key).
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Configuration function that sets up a pipeline builder.
    /// </summary>
    private readonly Func<IPipelineBuilder, IPipelineBuilder> _configure;
    
    /// <summary>
    /// Creates a new pipeline definition.
    /// </summary>
    /// <param name="name">Unique name for caching and debugging.</param>
    /// <param name="configure">Function to configure the pipeline builder.</param>
    public PipelineDefinition(
        string name, 
        Func<IPipelineBuilder, IPipelineBuilder> configure)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }
    
    /// <summary>
    /// Applies this definition's configuration to a pipeline builder.
    /// </summary>
    internal IPipelineBuilder ConfigureBuilder(IPipelineBuilder builder) => _configure(builder);
}
