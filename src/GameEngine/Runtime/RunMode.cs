namespace Nexus.GameEngine.Runtime;

/// <summary>
/// The mode the application should run in
/// </summary>
public enum RunMode
{
    /// <summary>
    /// Standard console application
    /// </summary>
    Console,

    /// <summary>
    /// Windows Service
    /// </summary>
    WindowsService,

    /// <summary>
    /// Linux/Unix daemon
    /// </summary>
    Daemon,

    /// <summary>
    /// macOS app bundle
    /// </summary>
    AppBundle
}
