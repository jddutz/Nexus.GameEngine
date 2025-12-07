using System.Collections;
using System.Collections.Generic;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Base class for source-generated PropertyBindings classes.
/// Provides enumeration of configured bindings for a component.
/// </summary>
/// <remarks>
/// Generated classes follow the pattern: {ComponentName}PropertyBindings
/// Example: HealthBarPropertyBindings, ButtonPropertyBindings
/// Properties hold PropertyBinding&lt;TSource, TValue&gt; instances.
/// </remarks>
public abstract class PropertyBindings : IEnumerable<(string propertyName, IPropertyBinding binding)>
{
    /// <summary>
    /// Enumerates all non-null property bindings configured for this component.
    /// </summary>
    /// <returns>Tuples of (property name, binding instance).</returns>
    public abstract IEnumerator<(string propertyName, IPropertyBinding binding)> GetEnumerator();

    /// <summary>
    /// Non-generic enumerator implementation.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
