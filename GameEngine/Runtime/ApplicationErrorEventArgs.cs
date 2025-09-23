namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Event arguments for application error events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ApplicationErrorEventArgs class.
/// </remarks>
/// <param name="exception">The exception that occurred</param>
public class ApplicationErrorEventArgs(Exception exception) : ApplicationEventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    /// <summary>
    /// Gets or sets whether the error has been handled.
    /// If set to true, the application will attempt to continue running.
    /// </summary>
    public bool Handled { get; set; }
}
