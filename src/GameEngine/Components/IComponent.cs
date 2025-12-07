using System;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Unified interface representing all component capabilities.
/// Combines identity, configuration, validation, hierarchy, activation, and update concerns.
/// Components can be resolved as this unified interface or any constituent interface.
/// </summary>
public interface IComponent 
    : IEntity,              // Identity (Id, Name, ApplyUpdates)
      ILoadable,            // Configuration (Load, IsLoaded, events)
      IValidatable,         // Validation (Validate, IsValid, ValidationErrors)
      IHierarchical,  // Hierarchy (Parent, Children, navigation)
      IActivatable,         // Activation lifecycle (Activate, Deactivate, IsActive)
      IUpdatable,           // Update lifecycle (Update)
      IDisposable           // Resource cleanup
{
    // Composition only - no additional members
    // All functionality inherited from constituent interfaces
}
