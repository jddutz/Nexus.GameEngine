using Microsoft.Extensions.Configuration;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Represents the startup context translated from OS-specific parameters to application parameters
/// </summary>
public class ApplicationContext
{
    /// <summary>
    /// The application configuration built from platform-specific sources
    /// </summary>
    public IConfiguration Configuration { get; set; } = null!;

    /// <summary>
    /// The mode the application should run in (Console, Service, Daemon, etc.)
    /// </summary>
    public RunMode RunMode { get; set; } = RunMode.Console;

    /// <summary>
    /// Platform-specific logging provider identifier
    /// </summary>
    public string LoggingProvider { get; set; } = "Console";

    /// <summary>
    /// Original command line arguments for application-level processing if needed
    /// </summary>
    public string[] CommandLineArguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Platform identifier for application to handle any platform-specific logic
    /// </summary>
    public PlatformType Platform { get; set; }
}
