namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Implements universal lifecycle methods that all components must support.
/// </summary>
public interface IComponentTemplate
{
    /// <summary>
    /// Name given to the template, may or may not be used by the component itself.
    /// Does not need to be unique.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Determines whether or not the component is enabled by default.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the collection of child component templates that will be instantiated as children of the component.
    /// Override this property to declare subcomponent templates.
    /// </summary>
    /// <remarks>
    /// This property is evaluated once during template instantiation.
    /// </remarks>
    IComponentTemplate[] Subcomponents { get; set; }
}