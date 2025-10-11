using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TestApp;

/// <summary>
/// Provides a simple logger implementation for integration tests.
/// Captures log history and outputs to Debug and Console.
/// Maintains a fixed-size history stack.
/// </summary>
public sealed class TestLogger(
    string categoryName,
    TestLoggerConfiguration config)
    : ILogger
{
    private readonly Queue<string> _history = new();

    /// <summary>
    /// Determines whether the specified log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if the log level is enabled; otherwise, false.</returns>
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= config.MinimumLevel;

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>An IDisposable that ends the scope on disposal.</returns>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => default!;

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log entry.</param>
    /// <param name="state">The entry to be written.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a string message of the state and exception.</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        var tag = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Critical => "CRT",
            LogLevel.Error => "ERR",
            _ => "INF"
        };

        var logMessage = $"{timestamp}|{tag}|{categoryName}|{formatter(state, exception)}";

        // Output to Debug Console when debugging, Console otherwise
        if (Debugger.IsAttached)
        {
            Debug.WriteLine(logMessage);
        }
        else
        {
            Console.WriteLine(logMessage);
        }

        // Maintain fixed-size history
        if (_history.Count >= config.HistoryLimit)
            _history.Dequeue();
        _history.Enqueue(logMessage);
    }

    /// <summary>
    /// Gets the log history.
    /// </summary>
    public IEnumerable<string> GetHistory() => _history.ToList();

    /// <summary>
    /// Clears the log history.
    /// </summary>
    public void ClearHistory() => _history.Clear();
}