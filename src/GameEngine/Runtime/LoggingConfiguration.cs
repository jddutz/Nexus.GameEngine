using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Configuration options for the console logger.
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level to output. Logs below this level will be filtered out.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to include timestamps in log output.
    /// </summary>
    public bool ShowTimestamp { get; set; } = true;

    /// <summary>
    /// Whether to include context in log output.
    /// </summary>
    public bool ShowContext { get; set; } = true;

    /// <summary>
    /// Whether to use color coding for different log levels.
    /// </summary>
    public bool UseColors { get; set; } = true;
}