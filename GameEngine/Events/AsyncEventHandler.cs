namespace Nexus.GameEngine.Events;

/// <summary>
/// Delegate for async event handler methods.
/// </summary>
/// <typeparam name="T">Type of event being handled</typeparam>
/// <param name="eventArgs">The event to handle</param>
/// <returns>Task representing the async operation</returns>
public delegate Task AsyncEventHandler<in T>(T eventArgs) where T : IGameEvent;