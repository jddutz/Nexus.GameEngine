namespace Nexus.GameEngine.Graphics;

public class RenderEventArgs(RenderContext context) : EventArgs
{
    public RenderContext Context { get; set; } = context;
}