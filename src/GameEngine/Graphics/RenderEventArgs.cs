using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public class RenderEventArgs(GL gl, IViewport viewport) : EventArgs
{
    public GL GL { get; set; } = gl;
    public IViewport Viewport { get; set; } = viewport;
}