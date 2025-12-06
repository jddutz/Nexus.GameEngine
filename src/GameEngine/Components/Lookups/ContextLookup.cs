namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Lookup strategy that searches up the component tree for an ancestor of type T.
/// </summary>
/// <typeparam name="T">The type of ancestor component to find.</typeparam>
public class ContextLookup<T> : ILookupStrategy where T : IComponent
{
    public IComponent? Resolve(IComponent targetComponent)
    {
        if (targetComponent == null) return null;

        var current = targetComponent.Parent;
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }
            current = current.Parent;
        }

        return null;
    }
}
