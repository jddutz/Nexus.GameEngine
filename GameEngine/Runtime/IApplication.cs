using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Defines the contract for applications that orchestrate the lifecycle
/// of game services and manage the main application loop.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Determines the template loaded when the application first starts.
    /// </summary>
    IComponentTemplate? StartupTemplate { get; set; }

    /// <summary>
    /// Starts the application and runs the main application loop.
    /// This method will block until the application is terminated.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task that completes when the application shuts down</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}
