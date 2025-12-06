using System.Linq;

namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Lookup strategy that searches for an immediate child component of type T.
/// </summary>
/// <typeparam name="T">The type of child component to find.</typeparam>
public class ChildLookup<T> : ILookupStrategy where T : IComponent
{
    public IComponent? Resolve(IComponent targetComponent)
    {
        if (targetComponent == null) return null;

        return targetComponent.Children
            .OfType<T>()
            .FirstOrDefault();
    }
}
