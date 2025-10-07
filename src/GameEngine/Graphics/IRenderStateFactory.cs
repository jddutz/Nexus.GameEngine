using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public interface IRenderStateFactory
{
    RenderData Create(GL gl);
}