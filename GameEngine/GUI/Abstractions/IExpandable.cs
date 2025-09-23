namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Interface for components that can expand to fill available space in layouts.
/// </summary>
public interface IExpandable
{
    /// <summary>
    /// Whether this component should expand to fill available space.
    /// </summary>
    bool ShouldExpand { get; }

    /// <summary>
    /// The priority for expansion when multiple expandable components are present.
    /// Higher values get more space.
    /// </summary>
    int ExpansionPriority { get; }
}