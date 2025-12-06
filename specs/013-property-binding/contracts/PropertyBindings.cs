namespace Nexus.GameEngine.Components;

/// <summary>
/// Base class for source-generated PropertyBindings classes.
/// Provides enumeration of configured bindings for a component.
/// </summary>
/// <remarks>
/// Generated classes follow the pattern: {ComponentName}PropertyBindings
/// Example: HealthBarPropertyBindings, ButtonPropertyBindings
/// </remarks>
public abstract class PropertyBindings : IEnumerable<(string propertyName, PropertyBinding binding)>
{
    /// <summary>
    /// Enumerates all non-null property bindings configured for this component.
    /// </summary>
    /// <returns>Tuples of (property name, binding instance).</returns>
    public abstract IEnumerator<(string propertyName, PropertyBinding binding)> GetEnumerator();

    /// <summary>
    /// Non-generic enumerator implementation.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
