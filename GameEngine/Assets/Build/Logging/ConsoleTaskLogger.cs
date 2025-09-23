using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Assets.Build.Logging;

/// <summary>
/// Simple console logger for build tasks.
/// </summary>
public class ConsoleTaskLogger : ILogger
{
    private readonly string _categoryName;

    /// <summary>
    /// Initializes a new console task logger.
    /// </summary>
    /// <param name="categoryName">Category name for the logger</param>
    public ConsoleTaskLogger(string categoryName = "Task")
    {
        _categoryName = categoryName;
    }

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for</typeparam>
    /// <param name="state">The identifier for the scope</param>
    /// <returns>A disposable scope object</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked</param>
    /// <returns>True if enabled</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written</typeparam>
    /// <param name="logLevel">Entry will be written on this level</param>
    /// <param name="eventId">Id of the event</param>
    /// <param name="state">The entry to be written</param>
    /// <param name="exception">The exception related to this entry</param>
    /// <param name="formatter">Function to create a string message of the state and exception</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var level = logLevel.ToString().ToUpper();

        Console.WriteLine($"[{timestamp}] [{level}] [{_categoryName}] {message}");

        if (exception != null)
        {
            Console.WriteLine($"[{timestamp}] [ERROR] [{_categoryName}] Exception: {exception}");
        }
    }
}