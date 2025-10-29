using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Implements universal lifecycle methods that all components must support.
/// </summary>
public interface IRuntimeComponent : IComponent
{
    /// <summary>
    /// Whether this component is currently active (successfully activated and valid).
    /// Components automatically become inactive when validation fails.
    /// This combines the component's Enabled and Active states for temporal consistency.
    /// </summary>
    bool IsActive();

    // Lifecycle Events
    event EventHandler<EventArgs>? Activating;
    event EventHandler<EventArgs>? Activated;
    event EventHandler<EventArgs>? Updating;
    event EventHandler<EventArgs>? Updated;
    event EventHandler<EventArgs>? Deactivating;
    event EventHandler<EventArgs>? Deactivated;

    /// <summary>
    /// Activate this component and all its subcomponents.
    /// Sets up event subscriptions and prepares for operation in root-to-leaf order.
    /// Parent components must be activated before children to provide proper event contexts.
    /// </summary>
    void Activate();

    /// <summary>
    /// Update this component and all its subcomponents.
    /// </summary>
    /// <param name="deltaTime">Time elapsed in seconds since the previous update.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Deactivate this component and all its subcomponents.
    /// Deactivates all subcomponents then calls OnDeactivate().
    /// Inactive components are excluded from the Update cycle
    /// but they can be re-activated.
    /// </summary>
    void Deactivate();
}