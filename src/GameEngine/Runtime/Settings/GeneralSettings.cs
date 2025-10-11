namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// General application settings.
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// The name of the application. Used for window title, logging, and Vulkan ApplicationInfo.
    /// </summary>
    public string ApplicationName { get; set; } = "Nexus Game Engine App";

    /// <summary>
    /// The version of the application. Used for logging and Vulkan ApplicationInfo.
    /// </summary>
    public string ApplicationVersion { get; set; } = "1.0.0";

    /// <summary>
    /// The name of the game engine. Used for Vulkan ApplicationInfo and debugging.
    /// </summary>
    public string EngineName { get; set; } = "Nexus Game Engine";

    /// <summary>
    /// The version of the game engine. Used for Vulkan ApplicationInfo and debugging.
    /// </summary>
    public string EngineVersion { get; set; } = "1.0.0";

    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool CheckForUpdates { get; set; } = true;
    public bool SendAnonymousUsageData { get; set; } = false;
    public string LastPlayedVersion { get; set; } = "1.0.0";
}