namespace Nexus.GameEngine.Actions;

/// <summary>
/// Result of an action execution, indicating success/failure and carrying optional data.
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Whether the action executed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Optional message describing the result (especially useful for failures).
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Optional data returned by the action execution.
    /// For example, selected items, calculated values, etc.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Optional exception if the action failed due to an error.
    /// </summary>
    public Exception? Exception { get; }

    private ActionResult(bool success, string? message = null, object? data = null, Exception? exception = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful action result.
    /// </summary>
    public static ActionResult Successful(object? data = null, string? message = null)
        => new(true, message, data);

    /// <summary>
    /// Creates a failed action result.
    /// </summary>
    public static ActionResult Failed(string message, Exception? exception = null)
        => new(false, message, null, exception);

    /// <summary>
    /// Creates a failed action result from an exception.
    /// </summary>
    public static ActionResult Failed(Exception exception)
        => new(false, exception.Message, null, exception);

    public override string ToString()
    {
        var status = Success ? "Success" : "Failed";
        return string.IsNullOrEmpty(Message) ? status : $"{status}: {Message}";
    }
}
