namespace Nexus.GameEngine.Components;

/// <summary>
/// Base template for all auto-generated component templates.
/// Provides common properties: ComponentType, Name, and Subcomponents.
/// Derived templates are auto-generated from [ComponentProperty] and [TemplateProperty] attributes.
/// </summary>
public record Template
{
    /// <summary>
    /// Gets the component type that should be instantiated from this template.
    /// Override in derived templates to return the specific component type.
    /// ComponentFactory uses this property to determine which component to create.
    /// Can be overridden at runtime for polymorphic component creation:
    /// new ElementTemplate { ComponentType = typeof(CustomButton) }
    /// </summary>
    public virtual Type? ComponentType { get; init; } = null;

    /// <summary>
    /// Gets or sets the name of the component instance.
    /// Used for component identification and caching in ContentManager.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the array of child component templates.
    /// Subcomponents are created recursively by ContentManager after parent configuration.
    /// </summary>
    public Template[] Subcomponents { get; set; } = [];
}
