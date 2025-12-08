using System;
using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Lookup strategy that searches up the component tree for the first parent of the specified type.
/// </summary>
/// <typeparam name="T">The type of parent component to find.</typeparam>
public class ParentLookup<T> : ILookupStrategy where T : class, IComponent
{
    public IComponent? Resolve(IComponent targetComponent)
    {
        ArgumentNullException.ThrowIfNull(targetComponent);

        var current = targetComponent.Parent;
        while (current != null)
        {
            if (current is T typed) return typed;
            current = current.Parent;
        }
        return null;
    }
}
