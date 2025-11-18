namespace Nexus.GameEngine.Components;

/// <summary>
/// Marks a private field or method to be included in the component's template as a property.
/// 
/// When applied to fields:
/// - Field is assigned once during OnLoad from the template
/// - Does not generate public properties
/// - Does not support deferred updates or animation
/// - Typically used for definition types that need conversion to resources
/// 
/// When applied to methods:
/// - Method must have exactly one parameter
/// - Method is called during Load() if template property is set
/// - Useful for computed setters that update multiple fields
/// - Template property will be nullable and only call method if value is provided
/// - Method should be a regular private method with implementation body inline
/// </summary>
/// <remarks>
/// Common pattern for fields: Use TemplateProperty for definitions (TextureDefinition, ShaderDefinition)
/// that need to be converted to resources (TextureResource, ShaderResource) in OnActivate.
/// 
/// Example with field:
/// <code>
/// [TemplateProperty(Name = "Texture")]
/// private TextureDefinition? _textureDefinition;
/// 
/// protected override void OnActivate()
/// {
///     if (_textureDefinition != null)
///         SetTexture(_textureDefinition); // Converts definition to resource
/// }
/// </code>
/// 
/// Example with method (implementation inline):
/// <code>
/// [ComponentProperty]
/// [TemplateProperty]
/// private Vector2D&lt;float&gt; _relativeSize = new(0f, 0f);
/// 
/// [TemplateProperty(Name = "RelativeWidth")]
/// private void SetRelativeWidth(float value)
/// {
///     _relativeSize = new Vector2D&lt;float&gt;(value, _relativeSize.Y);
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
public class TemplatePropertyAttribute : Attribute
{
    /// <summary>
    /// The name of the property in the generated template.
    /// If not specified, uses the field name with leading underscore removed and PascalCase,
    /// or for methods, removes "Set" prefix if present.
    /// </summary>
    public string? Name { get; set; }
}
