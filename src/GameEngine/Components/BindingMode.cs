namespace Nexus.GameEngine.Components;

/// <summary>
/// Defines the direction of data flow in a property binding.
/// </summary>
public enum BindingMode
{
    /// <summary>
    /// One-way binding: source → target only.
    /// Changes to the source property update the target property.
    /// Changes to the target property do not affect the source.
    /// </summary>
    OneWay = 0,

    /// <summary>
    /// Two-way binding: source ↔ target.
    /// Changes to either property update the other.
    /// Requires cycle prevention to avoid infinite loops.
    /// Requires IBidirectionalConverter if a converter is used.
    /// </summary>
    TwoWay = 1
}
