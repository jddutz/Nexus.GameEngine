using System;
using System.Collections.Generic;

namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Lookup strategy that searches for a component by name in the entire component tree.
/// </summary>
public class NamedObjectLookup : ILookupStrategy
{
    private readonly string _name;

    public NamedObjectLookup(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public IComponent? Resolve(IComponent targetComponent)
    {
        if (targetComponent == null) return null;

        // 1. Find the root of the tree
        var root = GetRoot(targetComponent);
        if (root == null) return null;

        // 2. Search the tree for the component with the specified name
        return FindComponentByName(root, _name);
    }

    private IComponent? GetRoot(IComponent component)
    {
        var current = component;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    private IComponent? FindComponentByName(IComponent current, string name)
    {
        // Check current component
        if (current.Name == name)
        {
            return current;
        }

        // Check children
        foreach (var child in current.Children)
        {
            var found = FindComponentByName(child, name);
            if (found != null) return found;
        }

        return null;
    }
}
