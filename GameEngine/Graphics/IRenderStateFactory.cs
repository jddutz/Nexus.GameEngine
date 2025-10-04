using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public interface IRenderStateFactory
{
    GLState Create(GL gl);
}