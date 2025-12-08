namespace Nexus.GameEngine.Components;

/// <summary>
/// Non-generic interface for property bindings to enable collection storage and lifecycle management.
/// </summary>
public interface IPropertyBinding
{
    /// <summary>
    /// Activates the binding by resolving source component and subscribing to property changes.
    /// </summary>
    /// <param name="target">The component that owns this binding.</param>
    void Activate(IComponent target);

    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    void Deactivate();
}
