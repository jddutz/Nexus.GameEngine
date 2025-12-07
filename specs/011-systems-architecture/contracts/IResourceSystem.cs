namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for resource system capabilities.
/// Access resource management (buffers, geometry, shaders, etc.) through extension methods.
/// </summary>
/// <remarks>
/// This is an empty marker interface - all functionality is provided through extension methods
/// in <see cref="ResourceSystemExtensions"/>. This pattern eliminates coupling to implementation
/// details while providing excellent IntelliSense discoverability.
/// 
/// Framework classes access resource capabilities via this system instead of constructor injection:
/// <code>
/// public class PipelineManager : IPipelineManager, IRequiresSystems
/// {
///     public IResourceSystem Resources { get; internal set; } = null!;
///     
///     public IPipeline CreatePipeline(PipelineConfig config)
///     {
///         var shader = Resources.LoadShader(config.ShaderPath);
///         // ... use shader
///     }
/// }
/// </code>
/// </remarks>
public interface IResourceSystem
{
    // Intentionally empty - all functionality provided via extension methods
}
