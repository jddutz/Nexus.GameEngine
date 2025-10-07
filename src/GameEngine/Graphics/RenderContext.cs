using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public class RenderContext
{
    public required GL GL { get; set; }
    public required IViewport Viewport { get; set; }
    public double ElapsedSeconds { get; set; } = 0.0;
}