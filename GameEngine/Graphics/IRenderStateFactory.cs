using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public interface IRenderStateFactory
{
    RenderState Create(GL gl);
}