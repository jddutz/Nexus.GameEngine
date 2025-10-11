using Nexus.GameEngine.Graphics;

namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// Application settings that can be configured by the user and persisted to storage.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// User interface preferences.
    /// </summary>
    public UiSettings Ui { get; set; } = new();

    /// <summary>
    /// Graphics and display preferences.
    /// </summary>
    public GraphicsSettings Graphics { get; set; } = new();

    /// <summary>
    /// Audio preferences.
    /// </summary>
    public AudioSettings Audio { get; set; } = new();

    /// <summary>
    /// Input mapping preferences.
    /// </summary>
    public InputSettings Input { get; set; } = new();

    /// <summary>
    /// General application preferences.
    /// </summary>
    public GeneralSettings General { get; set; } = new();
}