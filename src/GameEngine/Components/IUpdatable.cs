using System;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that participate in frame-by-frame updates.
/// Updates are temporal changes driven by elapsed time (deltaTime).
/// Typical uses: animations, physics, input processing, deferred property interpolation.
/// </summary>
public interface IUpdatable
{
    // Lifecycle
    void Update(double deltaTime);
    
    // Child Management
    void UpdateChildren(double deltaTime);
    
    // Events
    event EventHandler<EventArgs>? Updating;
    event EventHandler<EventArgs>? Updated;
}
