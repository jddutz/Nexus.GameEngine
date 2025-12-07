namespace Nexus.GameEngine.Systems;

/// <summary>
/// Marker interface for window system capabilities.
/// Access window services (window management, input context, etc.) through extension methods.
/// </summary>
/// <remarks>
/// This is an empty marker interface - all functionality is provided through extension methods
/// in <see cref="WindowSystemExtensions"/>. This pattern eliminates coupling to implementation
/// details while providing excellent IntelliSense discoverability.
/// 
/// Framework classes access window capabilities via this system instead of constructor injection:
/// <code>
/// public class InputManager : IInputManager, IRequiresSystems
/// {
///     public IWindowSystem Window { get; internal set; } = null!;
///     
///     public void ProcessInput()
///     {
///         var size = Window.GetSize();
///         var input = Window.GetInputContext();
///         // ... process input
///     }
/// }
/// </code>
/// </remarks>
public interface IWindowSystem
{
    // Intentionally empty - all functionality provided via extension methods
}
