namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Defines a strategy for resolving source components in the component tree
/// during property binding activation.
/// </summary>
public interface ILookupStrategy
{
    /// <summary>
    /// Resolves the source component relative to the target component.
    /// </summary>
    /// <param name="targetComponent">The component that owns the binding.</param>
    /// <returns>The resolved source component, or null if not found.</returns>
    /// <remarks>
    /// Implementations MUST return null if the source cannot be found.
    /// Do NOT throw exceptions for missing components.
    /// </remarks>
    IComponent? Resolve(IComponent targetComponent);
}
