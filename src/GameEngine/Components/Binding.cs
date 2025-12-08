using System;
using Nexus.GameEngine.Components.Lookups;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Factory methods for creating lookup functions for property bindings.
/// </summary>
public static class Binding
{
    /// <summary>
    /// Creates a lookup function that resolves the source component from the parent hierarchy.
    /// </summary>
    /// <typeparam name="TSource">The type of the source component.</typeparam>
    public static Func<IComponent, TSource?> ParentLookup<TSource>() where TSource : class, IComponent
    {
        return c => new ParentLookup<TSource>().Resolve(c) as TSource;
    }

    /// <summary>
    /// Creates a lookup function that resolves the source component from siblings.
    /// </summary>
    /// <typeparam name="TSource">The type of the source component.</typeparam>
    public static Func<IComponent, TSource?> SiblingLookup<TSource>() where TSource : class, IComponent
    {
        return c => new SiblingLookup<TSource>().Resolve(c) as TSource;
    }

    /// <summary>
    /// Creates a lookup function that resolves the source component from children.
    /// </summary>
    /// <typeparam name="TSource">The type of the source component.</typeparam>
    public static Func<IComponent, TSource?> ChildLookup<TSource>() where TSource : class, IComponent
    {
        return c => new ChildLookup<TSource>().Resolve(c) as TSource;
    }
}
