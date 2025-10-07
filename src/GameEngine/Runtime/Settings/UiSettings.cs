namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// User interface settings and preferences.
/// </summary>
public class UiSettings
{
    public string Theme { get; set; } = "Default";
    public float UiScale { get; set; } = 1.0f;
    public string Language { get; set; } = "en-US";
    public bool ShowTooltips { get; set; } = true;
    public bool ShowFpsCounter { get; set; } = false;
    public bool ShowDebugInfo { get; set; } = false;
}