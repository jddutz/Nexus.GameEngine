# API Contract: IValueConverter

**Namespace**: `Nexus.GameEngine.Data`  
**Purpose**: Interface for custom value transformation during property binding updates

## Interface Definition

```csharp
namespace Nexus.GameEngine.Data;

/// <summary>
/// Interface for converting values during property binding updates.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Converts a value from source type to target type.
    /// </summary>
    /// <param name="value">The source value to convert</param>
    /// <returns>The converted value, or null if conversion fails</returns>
    object? Convert(object? value);
}
```

## Built-In Implementation: StringFormatConverter

```csharp
namespace Nexus.GameEngine.Data;

/// <summary>
/// Converts values to formatted strings using .NET format strings.
/// </summary>
public class StringFormatConverter : IValueConverter
{
    private readonly string _format;
    
    /// <summary>
    /// Creates a string format converter with the specified format pattern.
    /// </summary>
    /// <param name="format">Format string (e.g., "Health: {0:F0}", "{0:P0}", "{0:C2}")</param>
    /// <exception cref="ArgumentNullException">Format is null</exception>
    public StringFormatConverter(string format)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
    }
    
    /// <summary>
    /// Formats the value using the configured format string.
    /// </summary>
    /// <param name="value">Value to format</param>
    /// <returns>Formatted string, or value.ToString() if formatting fails</returns>
    public object? Convert(object? value)
    {
        try
        {
            return string.Format(_format, value);
        }
        catch (FormatException)
        {
            // Log error if logger available
            return value?.ToString() ?? "";
        }
    }
}
```

## Usage Patterns

### Via AsFormattedString() (Recommended)

```csharp
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .AsFormattedString("Health: {0:F0}")  // Creates StringFormatConverter internally
    .Set(SetText);
```

### Via WithConverter() (Custom Converter)

```csharp
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .WithConverter(new StringFormatConverter("HP: {0:N0}"))
    .Set(SetText);
```

## Format String Examples

### Numeric Formatting

```csharp
// Integer with no decimals
"Health: {0:F0}"          // "Health: 75"
"Score: {0:N0}"           // "Score: 1,234,567"

// Floating-point with decimals
"Position: {0:F2}"        // "Position: 123.45"
"Percentage: {0:P0}"      // "Percentage: 75%"

// Currency
"Price: {0:C2}"           // "Price: $12.99"

// Custom format
"Value: {0:000.00}"       // "Value: 012.50"
```

### Composite Formatting

```csharp
// Multiple values (requires custom converter)
"Player: {0} - Health: {1:F0}"
"Position: ({0:F1}, {1:F1})"
```

## Custom Converter Examples

### Health Percentage Converter

```csharp
public class HealthPercentageConverter : IValueConverter
{
    private readonly float _maxHealth;
    
    public HealthPercentageConverter(float maxHealth)
    {
        _maxHealth = maxHealth;
    }
    
    public object? Convert(object? value)
    {
        if (value is not float health) return null;
        
        float percentage = (health / _maxHealth) * 100f;
        return $"{percentage:F0}%";
    }
}

// Usage:
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .WithConverter(new HealthPercentageConverter(100f))
    .Set(SetText);
```

### Color to Hex Converter

```csharp
public class ColorToHexConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        if (value is not Color color) return null;
        
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
```

### Enum to Display Name Converter

```csharp
public class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        if (value is not Enum enumValue) return null;
        
        var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<DisplayNameAttribute>();
        
        return attribute?.DisplayName ?? enumValue.ToString();
    }
}
```

### Multi-Value Converter (Composite)

```csharp
public class PositionConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        if (value is not Vector2D<float> pos) return null;
        
        return $"({pos.X:F1}, {pos.Y:F1})";
    }
}
```

## Bidirectional Converter Interface (Future)

```csharp
namespace Nexus.GameEngine.Data;

/// <summary>
/// Interface for bidirectional value conversion (two-way bindings).
/// </summary>
/// <remarks>Out of scope for v2, included for future compatibility</remarks>
public interface IBidirectionalConverter : IValueConverter
{
    /// <summary>
    /// Converts a value back from target type to source type.
    /// </summary>
    /// <param name="value">The target value to convert back</param>
    /// <returns>The converted source value, or null if conversion fails</returns>
    object? ConvertBack(object? value);
}
```

## Error Handling Contract

- **Null input**: Converters should handle null gracefully (return null or default)
- **Invalid type**: Return null or throw InvalidCastException (converter implementation choice)
- **Format errors**: Return fallback value (typically value.ToString() or empty string)
- **Exception propagation**: Binding catches exceptions, logs error, skips update

## Performance Guidelines

- ✅ **Stateless preferred**: Converters should be stateless for reusability
- ✅ **Allocation-conscious**: Avoid allocations in hot path (cache strings if possible)
- ⚠️ **Complex conversions**: Keep under 1ms per conversion
- ❌ **No I/O operations**: Converters should be synchronous and pure

## Thread Safety

- ✅ **Thread-safe**: Stateless converters are inherently thread-safe
- ⚠️ **Stateful converters**: Must synchronize access to mutable state
- ⚠️ **Event handlers**: Execute on source component's thread (typically main thread)

## Validation

```csharp
// Converter with validation
public class RangeConverter : IValueConverter
{
    private readonly float _min;
    private readonly float _max;
    
    public RangeConverter(float min, float max)
    {
        if (min > max) throw new ArgumentException("min must be <= max");
        _min = min;
        _max = max;
    }
    
    public object? Convert(object? value)
    {
        if (value is not float floatValue) return null;
        
        return Math.Clamp(floatValue, _min, _max);
    }
}
```

## Extension Points

### Converter Chaining (Not Built-In)

```csharp
public class ConverterChain : IValueConverter
{
    private readonly IValueConverter[] _converters;
    
    public ConverterChain(params IValueConverter[] converters)
    {
        _converters = converters;
    }
    
    public object? Convert(object? value)
    {
        foreach (var converter in _converters)
        {
            value = converter.Convert(value);
            if (value == null) return null;
        }
        return value;
    }
}

// Usage:
.WithConverter(new ConverterChain(
    new HealthPercentageConverter(100f),
    new StringFormatConverter("{0:F0}%")
))
```

### Localization Converter

```csharp
public class LocalizedStringConverter : IValueConverter
{
    private readonly ILocalizationService _localization;
    
    public LocalizedStringConverter(ILocalizationService localization)
    {
        _localization = localization;
    }
    
    public object? Convert(object? value)
    {
        if (value is not string key) return null;
        
        return _localization.GetString(key);
    }
}
```

## Known Limitations (v2)

1. **No type safety**: Input/output types are `object?`, runtime casting required
2. **No async conversion**: All conversions must be synchronous
3. **No converter composition**: Cannot chain converters declaratively
4. **No converter parameters**: Cannot parameterize converters from binding configuration
5. **No bidirectional conversion**: IBidirectionalConverter exists but not used in v2
