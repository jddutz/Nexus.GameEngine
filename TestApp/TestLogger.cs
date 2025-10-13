using System.Diagnostics;
using System.Text.RegularExpressions;
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
    private static readonly Dictionary<string, List<string>> _capture = [];

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

        foreach (var key in _capture.Keys)
        {
            var match = Regex.Match(logMessage, key);
            if (match.Success) _capture[key].Add(logMessage);
        }
    }

    /// <summary>
    /// Captures log messages matching the specified pattern.
    /// </summary>
    /// <param name="regex">Pattern used to match log messages.</param>
    /// <returns>A list of messages captured since the first call.</returns>
    public static IEnumerable<string> Capture(string regex)
    {
        if (_capture.ContainsKey(regex)) return _capture[regex];

        _capture[regex] = [];
        return _capture[regex];
    }
    
    /// <summary>
    /// Stops capturing log messages matching the specified pattern.
    /// </summary>
    /// <param name="regex">Pattern used to match log messages.</param>
    /// <returns>The list of messages captured since the first Capture call.</returns>
    public static IEnumerable<string> StopCapture(string regex)
    {
        if (_capture.ContainsKey(regex))
        {
            var result = _capture[regex];
            _capture.Remove(regex);
            return result;
        }

        return [];
    }
}