namespace Nexus.GameEngine.Components;

public interface IEntity
{
    ComponentId Id { get; }
    string Name { get; }
    void ApplyUpdates(double deltaTime);
}