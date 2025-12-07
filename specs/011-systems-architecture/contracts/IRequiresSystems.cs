namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for framework classes that require system initialization.
/// </summary>
/// <remarks>
/// Framework classes that use system properties (Graphics, Resources, etc.) must implement
/// this interface. The DI container calls <see cref="InitializeSystems"/> after construction
/// to assign system instances.
/// 
/// Example implementation:
/// <code>
/// public class Renderer : IRenderer, IRequiresSystems
/// {
///     public IGraphicsSystem Graphics { get; internal set; } = null!;
///     public IResourceSystem Resources { get; internal set; } = null!;
///     
///     public Renderer() { } // Parameterless constructor
///     
///     public void InitializeSystems(IServiceProvider serviceProvider)
///     {
///         Graphics = serviceProvider.GetRequiredService&lt;IGraphicsSystem&gt;();
///         Resources = serviceProvider.GetRequiredService&lt;IResourceSystem&gt;();
///     }
/// }
/// </code>
/// 
/// DI registration uses factory pattern:
/// <code>
/// services.AddSingleton&lt;IRenderer&gt;(sp =&gt;
/// {
///     var renderer = new Renderer();
///     renderer.InitializeSystems(sp);
///     return renderer;
/// });
/// </code>
/// </remarks>
public interface IRequiresSystems
{
    /// <summary>
    /// Initialize system properties from the service provider.
    /// Called by DI container after construction.
    /// </summary>
    /// <param name="serviceProvider">Service provider to resolve systems from.</param>
    void InitializeSystems(IServiceProvider serviceProvider);
}
