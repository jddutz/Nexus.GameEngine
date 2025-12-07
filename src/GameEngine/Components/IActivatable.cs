using System;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that can be activated and deactivated.
/// Activation is the setup/initialization phase (property bindings, event subscriptions, resource preparation).
/// Deactivation is the teardown/cleanup phase (unbind properties, unsubscribe events, release resources).
/// </summary>
public interface IActivatable
{
    // State
    bool IsActive();
    
    // Lifecycle
    void Activate();
    void Deactivate();
    
    // Child Management
    void ActivateChildren();
    void ActivateChildren<TChild>() where TChild : IActivatable;
    void DeactivateChildren();
    
    // Events
    event EventHandler<EventArgs>? Activating;
    event EventHandler<EventArgs>? Activated;
    event EventHandler<EventArgs>? Deactivating;
    event EventHandler<EventArgs>? Deactivated;
}
