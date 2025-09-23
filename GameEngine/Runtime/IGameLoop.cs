namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Defines the contract for the main game loop that handles update and render cycles.
/// </summary>
public interface IGameLoop
{
    /// <summary>
    /// Event raised when the game loop is starting.
    /// </summary>
    event EventHandler Started;

    /// <summary>
    /// Event raised when the game loop is stopping.
    /// </summary>
    event EventHandler Stopped;

    /// <summary>
    /// Gets a value indicating whether the game loop is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the game loop. This method will block until the loop is stopped.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task that completes when the game loop stops</returns>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the game loop to stop gracefully and waits for completion.
    /// </summary>
    /// <returns>A task that completes when the game loop has fully stopped</returns>
    Task StopAsync();
}