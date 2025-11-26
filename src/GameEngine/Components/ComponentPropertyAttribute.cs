namespace Nexus.GameEngine.Components;

/// <summary>
/// Marks a private field for source-generated deferred property support.
/// Apply this attribute to a private field to generate a public property and a Set{Property} method.
/// The generated property supports deferred updates (applied on the next ApplyUpdates call),
/// and the Set method allows specifying an optional interpolation function.
/// </summary>
/// <example>
/// [ComponentProperty]
/// private float _fontSize = 12f;
/// // Generates:
/// //   public float FontSize { get; }
/// //   public void SetFontSize(float value, InterpolationFunction<float> interpolator = null)
/// // Usage:
/// //   component.SetFontSize(24f);
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ComponentPropertyAttribute : Attribute
{
    /// <summary>
    /// Optional custom name for the generated property.
    /// If not specified, the property name is derived from the field name (e.g., _fontSize → FontSize).
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Optional hook method name to call before a property change is queued.
    /// The method should have signature: void MethodName(ref T newValue)
    /// Use this to modify the value before it is set.
    /// </summary>
    public string? BeforeChange { get; set; }
}

