using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Unified component class consolidating Entity, Configurable, Component, and RuntimeComponent.
/// Organized into partial classes for logical separation of concerns.
/// </summary>
public partial class Component : IComponent, IConfigurable
{
    public Component()
    {
        _name = GetType().Name;
    }

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public ComponentId Id { get; set; } = ComponentId.None;

    /// <summary>
    /// Human-readable name for this component instance.
    /// Changes are deferred until next ApplyUpdates() call.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected string _name = "Component";
}
