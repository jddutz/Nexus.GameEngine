namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for input system capabilities.
/// Access input services (keyboard, mouse, gamepad, etc.) through extension methods.
/// </summary>
/// <remarks>
/// This is an empty marker interface - all functionality is provided through extension methods
/// in <see cref="InputSystemExtensions"/>. This pattern eliminates coupling to implementation
/// details while providing excellent IntelliSense discoverability.
/// 
/// Framework classes access input capabilities via this system instead of constructor injection:
/// <code>
/// public class PlayerController : IPlayerController, IRequiresSystems
/// {
///     public IInputSystem Input { get; internal set; } = null!;
///     
///     public void Update(float deltaTime)
///     {
///         var moveX = Input.GetAxis("Horizontal");
///         var moveY = Input.GetAxis("Vertical");
///         // ... handle movement
///     }
/// }
/// </code>
/// </remarks>
public interface IInputSystem
{
    // Intentionally empty - all functionality provided via extension methods
}
