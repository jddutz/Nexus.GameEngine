using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

public interface IRenderStateFactory
{
    ElementData Create(GL gl);
}