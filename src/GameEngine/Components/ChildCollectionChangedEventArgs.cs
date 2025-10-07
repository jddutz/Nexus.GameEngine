
namespace Nexus.GameEngine.Components;

public class ChildCollectionChangedEventArgs
{
    public IEnumerable<IRuntimeComponent> Added { get; set; } = [];
    public IEnumerable<IRuntimeComponent> Removed { get; set; } = [];
}