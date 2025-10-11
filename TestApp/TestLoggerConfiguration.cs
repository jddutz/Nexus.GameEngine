using Microsoft.Extensions.Logging;

namespace TestApp;

/// <summary>
/// Configuration options for the console logger.
/// </summary>
public class TestLoggerConfiguration
{
    /// <summary>
    /// Minimum log level to output. Logs below this level will be filtered out.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Maximum number of historical log entries to retain.
    /// </summary>
    public int HistoryLimit { get; set; } = 100;

    /// <summary>
    /// Specifies the length of the category name.
    /// If the category name is shorter, it is left-padded.
    /// If the category name is longer, it is truncated.
    /// </summary>
    public int NameLength { get; set; } = 16;
}