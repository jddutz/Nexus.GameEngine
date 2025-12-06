using System.Linq;

namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Lookup strategy that searches for a sibling component of type T.
/// </summary>
/// <typeparam name="T">The type of sibling component to find.</typeparam>
public class SiblingLookup<T> : ILookupStrategy where T : IComponent
{
    public IComponent? Resolve(IComponent targetComponent)
    {
        if (targetComponent?.Parent == null) return null;

        // Find first child of parent that is of type T and is not the target component
        return targetComponent.Parent.Children
            .OfType<T>()
            .FirstOrDefault(c => !ReferenceEquals(c, targetComponent));
    }
}
