using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Graphics and display settings.
/// </summary>
public class GraphicsSettings
{
    public bool Fullscreen { get; set; } = true;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public bool VSync { get; set; } = true;
    public int TargetFramerate { get; set; } = 60;
    public string GraphicsQuality { get; set; } = "High";
    public bool ShowParticleEffects { get; set; } = true;
    public float RenderScale { get; set; } = 1.0f;
    public VulkanSettings Vulkan { get; set; } = new();
    public Vector4D<float>? BackgroundColor { get; set; }
}