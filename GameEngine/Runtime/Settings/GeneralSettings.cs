namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// General application settings.
/// </summary>
public class GeneralSettings
{
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool CheckForUpdates { get; set; } = true;
    public bool SendAnonymousUsageData { get; set; } = false;
    public string LastPlayedVersion { get; set; } = "1.0.0";
}