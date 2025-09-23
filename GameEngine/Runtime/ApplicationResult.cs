namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Represents the result of application execution for OS-specific translation
/// </summary>
public class ApplicationResult
{
    /// <summary>
    /// The reason the application exited
    /// </summary>
    public ExitReason ExitReason { get; set; } = ExitReason.Success;

    /// <summary>
    /// Additional message about the exit reason
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Any exception that caused the application to exit
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a successful application result
    /// </summary>
    public static ApplicationResult Success() => new() { ExitReason = ExitReason.Success };

    /// <summary>
    /// Creates an error application result
    /// </summary>
    public static ApplicationResult Error(ExitReason reason, string? message = null, Exception? exception = null) =>
        new() { ExitReason = reason, Message = message, Exception = exception };
}
