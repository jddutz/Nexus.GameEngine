namespace Nexus.GameEngine.Runtime.Settings;

/// <summary>
/// Input settings and key mappings.
/// </summary>
public class InputSettings
{
    public Dictionary<string, string> KeyMappings { get; set; } = [];
    public float MouseSensitivity { get; set; } = 1.0f;
    public bool InvertMouseY { get; set; } = false;
    public bool InvertMouseX { get; set; } = false;
    public float GamepadSensitivity { get; set; } = 1.0f;
    public float GamepadDeadzone { get; set; } = 0.1f;
}