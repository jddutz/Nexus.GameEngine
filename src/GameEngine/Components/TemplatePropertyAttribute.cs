namespace Nexus.GameEngine.Components;

/// <summary>
/// Marks a private field to be included in the component's template as a property.
/// Unlike ComponentProperty, TemplateProperty fields:
/// - Are assigned once during OnLoad from the template
/// - Do not generate public properties
/// - Do not support deferred updates or animation
/// - Are typically used for definition types that need conversion to resources
/// </summary>
/// <remarks>
/// Common pattern: Use TemplateProperty for definitions (TextureDefinition, ShaderDefinition)
/// that need to be converted to resources (TextureResource, ShaderResource) in OnActivate.
/// 
/// Example:
/// <code>
/// [TemplateProperty(Name = "Texture")]
/// private TextureDefinition? _textureDefinition;
/// 
/// [TemplateProperty]
/// private TextureResource? _texture;
/// 
/// protected override void OnActivate()
/// {
///     if (_textureDefinition != null)
///         SetTexture(_textureDefinition); // Converts definition to resource
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TemplatePropertyAttribute : Attribute
{
    /// <summary>
    /// The name of the property in the generated template.
    /// If not specified, uses the field name with leading underscore removed and PascalCase.
    /// </summary>
    public string? Name { get; set; }
}
