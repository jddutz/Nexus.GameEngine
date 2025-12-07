namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for graphics system capabilities.
/// Access graphics services (rendering, pipelines, swap chain, etc.) through extension methods.
/// </summary>
/// <remarks>
/// This is an empty marker interface - all functionality is provided through extension methods
/// in <see cref="GraphicsSystemExtensions"/>. This pattern eliminates coupling to implementation
/// details while providing excellent IntelliSense discoverability.
/// 
/// Framework classes access graphics capabilities via this system instead of constructor injection:
/// <code>
/// public class Renderer : IRenderer, IRequiresSystems
/// {
///     public IGraphicsSystem Graphics { get; internal set; } = null!;
///     
///     public void RenderFrame()
///     {
///         Graphics.BeginFrame();
///         Graphics.DrawQuad(position, size, color);
///         Graphics.EndFrame();
///     }
/// }
/// </code>
/// </remarks>
public interface IGraphicsSystem
{
    // Intentionally empty - all functionality provided via extension methods
}
