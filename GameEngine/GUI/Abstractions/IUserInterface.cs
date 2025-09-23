using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.GUI.Abstractions;

public interface IUserInterface : IRuntimeComponent
{
    void Render();
}