using System;
using System.Collections.Generic;

namespace Nexus.GameEngine.Components;

public partial class Component
{
    /// <summary>
    /// Gets the list of active property bindings for this component.
    /// </summary>
    protected List<IPropertyBinding> PropertyBindings { get; } = new();

    /// <summary>
    /// Loads property bindings from the template.
    /// </summary>
    protected virtual void LoadPropertyBindings(Template template)
    {
        if (template?.Bindings != null)
        {
            PropertyBindings.AddRange(template.Bindings);
        }
    }

    /// <summary>
    /// Activates all property bindings.
    /// </summary>
    protected virtual void ActivatePropertyBindings()
    {
        foreach (var binding in PropertyBindings)
        {
            binding.Activate(this);
        }
    }

    /// <summary>
    /// Deactivates all property bindings.
    /// </summary>
    protected virtual void DeactivatePropertyBindings()
    {
        foreach (var binding in PropertyBindings)
        {
            binding.Deactivate();
        }
    }
}
