namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for content system capabilities.
/// Access content management (component factory, content manager, etc.) through extension methods.
/// </summary>
/// <remarks>
/// This is an empty marker interface - all functionality is provided through extension methods
/// in <see cref="ContentSystemExtensions"/>. This pattern eliminates coupling to implementation
/// details while providing excellent IntelliSense discoverability.
/// 
/// Framework classes access content capabilities via this system instead of constructor injection:
/// <code>
/// public class Scene : IScene, IRequiresSystems
/// {
///     public IContentSystem Content { get; internal set; } = null!;
///     
///     public void LoadLevel(LevelTemplate template)
///     {
///         var level = Content.CreateInstance(template);
///         Content.Activate(level);
///     }
/// }
/// </code>
/// </remarks>
public interface IContentSystem
{
    // Intentionally empty - all functionality provided via extension methods
}
