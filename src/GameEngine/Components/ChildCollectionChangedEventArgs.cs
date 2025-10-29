
namespace Nexus.GameEngine.Components;

public class ChildCollectionChangedEventArgs
{
    public IEnumerable<IComponent> Added { get; set; } = [];
    public IEnumerable<IComponent> Removed { get; set; } = [];
}