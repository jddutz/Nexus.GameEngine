namespace Nexus.GameEngine.Components;

public interface IEntity
{
    ComponentId Id { get; set; }
    string Name { get; set; }
    void ApplyUpdates(double deltaTime);
}