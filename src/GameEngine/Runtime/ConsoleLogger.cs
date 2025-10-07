using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// ILogger implementation that writes to the console with color-coded output.
/// Works in both Debug and Release configurations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConsoleLogger class.
/// </remarks>
/// <param name="context">The context for this logger, typically the full name of the class or component that will be generating log messages. This appears in log output when ShowContext is enabled.</param>
/// <param name="configuration">Optional logging configuration. If not provided, uses default configuration settings.</param>
public class ConsoleLogger(string context, LoggingConfiguration? configuration = null) : ILogger
{
    private readonly string _context = context;
    private readonly LoggingConfiguration _configuration = configuration ?? new LoggingConfiguration();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _configuration.MinimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var fgColor = Console.ForegroundColor;
        Console.ForegroundColor = GetLogLevelColor(logLevel);

        var message = formatter(state, exception);
        var formattedMessage = FormatMessage(logLevel, message);

        Console.WriteLine(formattedMessage);

        if (exception != null)
        {
            var exceptionMessage = FormatException(exception);
            Console.WriteLine(exceptionMessage);
        }

        Console.ForegroundColor = fgColor;
    }

    private string FormatMessage(LogLevel logLevel, string message)
    {
        var parts = new List<string>();

        if (_configuration.ShowTimestamp)
        {
            parts.Add($"[{DateTime.Now:HH:mm:ss}]");
        }

        parts.Add($"[{GetLogLevelString(logLevel)}]");

        if (_configuration.ShowContext)
        {
            parts.Add($"{_context}:");
        }

        parts.Add(message);

        return string.Join(" ", parts);
    }

    private string FormatException(Exception exception)
    {
        return $"Exception: {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "NONE"
        };
    }

    private static ConsoleColor GetLogLevelColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.DarkGray,
            LogLevel.Information => ConsoleColor.Blue,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }
}

