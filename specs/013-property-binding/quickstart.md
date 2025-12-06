# Property Binding Quick Start Guide

**Feature**: Property Binding System  
**Branch**: 013-property-binding  
**Date**: December 6, 2025

## Introduction

Property bindings enable declarative synchronization of properties between components without writing manual event subscription code. This guide gets you started with the most common binding scenarios.

## Prerequisites

- Understanding of Nexus.GameEngine component system
- Familiarity with `[ComponentProperty]` attributes
- Basic knowledge of component templates

## 5-Minute Quick Start

### Step 1: Enable Property Change Notifications

Mark source properties with `NotifyChange = true`:

```csharp
public partial class PlayerCharacter : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]  // ← Enable change notifications
    private float _health = 100f;
}
```

This generates a `HealthChanged` event automatically.

### Step 2: Configure Binding in Template

Define bindings in your component template:

```csharp
new HealthBarTemplate()
{
    Bindings = 
    {
        // Bind HealthBar.CurrentHealth to PlayerCharacter.Health
        CurrentHealth = PropertyBinding
            .FromParent<PlayerCharacter>()
            .GetPropertyValue(nameof(PlayerCharacter.Health))
    }
}
```

### Step 3: Use in Component Tree

```csharp
new PlayerCharacter()
{
    Health = 100f,
    Subcomponents = 
    [
        new HealthBar()  // ← Automatically syncs with parent's Health
        {
            Bindings = 
            {
                CurrentHealth = PropertyBinding
                    .FromParent<PlayerCharacter>()
                    .GetPropertyValue(nameof(PlayerCharacter.Health))
            }
        }
    ]
}
```

**That's it!** When `PlayerCharacter.Health` changes, `HealthBar.CurrentHealth` updates automatically.

## Common Scenarios

### Scenario 1: Display Numeric Value as Text

**Goal**: Show health as "Health: 75.5"

```csharp
new TextDisplayTemplate()
{
    Bindings = 
    {
        Text = PropertyBinding
            .FromParent<PlayerCharacter>()
            .GetPropertyValue(nameof(PlayerCharacter.Health))
            .AsFormattedString("Health: {0:F1}")  // ← Format converter
    }
}
```

### Scenario 2: Bind to Named Component

**Goal**: Bind to a specific named component anywhere in the tree

```csharp
// Component definition
new PlayerCharacter()
{
    Name = "Player",  // ← Give it a name
    Health = 100f
}

// Binding to named component
new HealthBarTemplate()
{
    Bindings = 
    {
        CurrentHealth = PropertyBinding
            .FromNamedObject("Player")  // ← Look up by name
            .GetPropertyValue(nameof(PlayerCharacter.Health))
    }
}
```

### Scenario 3: Two-Way Binding for Controls

**Goal**: Slider that both displays and controls volume

```csharp
public partial class AudioSettings : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    private float _masterVolume = 0.75f;  // 0.0 - 1.0 range
}

new SliderTemplate()
{
    Bindings = 
    {
        Value = PropertyBinding
            .FromContext<AudioSettings>()  // ← Search up tree
            .GetPropertyValue(nameof(AudioSettings.MasterVolume))
            .WithConverter(new PercentageConverter())  // 0.75 ↔ 75
            .TwoWay()  // ← Bidirectional sync
    }
}
```

### Scenario 4: Bind Between Siblings

**Goal**: One sibling component reacts to another sibling's changes

```csharp
new Container()
{
    Subcomponents = 
    [
        new ScoreCounter()
        {
            Name = "Score",
            [ComponentProperty(NotifyChange = true)]
            private int _score = 0;
        },
        
        new ScoreDisplay()
        {
            Bindings = 
            {
                DisplayValue = PropertyBinding
                    .FromSibling<ScoreCounter>()  // ← Find sibling
                    .GetPropertyValue(nameof(ScoreCounter.Score))
            }
        }
    ]
}
```

### Scenario 5: Context-Based Theming

**Goal**: Multiple components share theme colors from a context provider

```csharp
new ThemeContext()
{
    PrimaryColor = new Color(255, 0, 0),
    Subcomponents = 
    [
        new Button()
        {
            Bindings = 
            {
                BackgroundColor = PropertyBinding
                    .FromContext<ThemeContext>()  // ← Search up tree
                    .GetPropertyValue(nameof(ThemeContext.PrimaryColor))
            }
        },
        
        new Panel()
        {
            Bindings = 
            {
                BorderColor = PropertyBinding
                    .FromContext<ThemeContext>()  // ← Reuse same context
                    .GetPropertyValue(nameof(ThemeContext.PrimaryColor))
            }
        }
    ]
}
```

## Lookup Strategies Cheat Sheet

| Strategy | Use When | Example |
|----------|----------|---------|
| `FromParent<T>()` | Immediate parent relationship | Child health bar reading parent player's health |
| `FromSibling<T>()` | Siblings need to communicate | Score display reading score counter |
| `FromChild<T>()` | Parent reads first child's property | Container reading first child's size |
| `FromContext<T>()` | Global/theme values needed | UI components reading theme context |
| `FromNamedObject(name)` | Specific named component | Binding to "Player" or "GameState" |

## Value Converters Cheat Sheet

### Built-In Converters

```csharp
// Format any value as string
.AsFormattedString("Score: {0:N0}")  // "Score: 1,234"
.AsFormattedString("Health: {0:F1}%")  // "Health: 75.5%"

// Custom multiply
.WithConverter(new MultiplyConverter { Factor = 0.01f })  // 75 → 0.75

// Percentage conversion (bidirectional)
.WithConverter(new PercentageConverter())  // 0.75 ↔ 75
```

### Custom Converter

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        return value is bool b && b 
            ? new Color(0, 255, 0)  // Green when true
            : new Color(255, 0, 0);  // Red when false
    }
}

// Usage
.WithConverter(new BoolToColorConverter())
```

### Custom Bidirectional Converter

```csharp
public class CelsiusToFahrenheitConverter : IBidirectionalConverter
{
    public object? Convert(object? value)
    {
        return value is float c ? (c * 9f / 5f) + 32f : null;
    }
    
    public object? ConvertBack(object? value)
    {
        return value is float f ? (f - 32f) * 5f / 9f : null;
    }
}

// Usage
.WithConverter(new CelsiusToFahrenheitConverter())
.TwoWay()
```

## Best Practices

### ✅ DO

- Use `NotifyChange = true` only on properties that need to notify others
- Define bindings in templates (composition time), not in component classes
- Use type-safe lookup methods (`FromParent<T>()` instead of string paths)
- Handle null gracefully in custom converters
- Use `FromContext<T>()` for truly global state (themes, settings)

### ❌ DON'T

- Don't create circular bindings without understanding cycle prevention
- Don't throw exceptions in custom converters (return null instead)
- Don't use bindings for high-frequency updates (e.g., every frame) - use direct calls
- Don't use `TwoWay` mode unless you need bidirectional sync
- Don't bind to properties without `NotifyChange = true` (binding won't update)

## Troubleshooting

### Binding Not Updating

**Symptom**: Target property doesn't change when source changes

**Solutions**:
1. Verify source property has `NotifyChange = true`
2. Check that source component is in the expected location (parent/sibling/etc.)
3. Verify property names match exactly (case-sensitive)
4. Check binding is configured before component activation

### Binding Throws Exception

**Symptom**: Exception during component activation

**Solutions**:
1. Verify source component type exists in the tree
2. Check converter doesn't throw exceptions (should return null)
3. Verify `TwoWay` bindings use `IBidirectionalConverter` if converter is set

### Memory Leak

**Symptom**: Components not being garbage collected

**Solutions**:
1. Verify component is properly deactivated (calls `OnDeactivate()`)
2. Check for manual event subscriptions that aren't cleaned up
3. Bindings automatically clean up - no manual unsubscribe needed

### Performance Issues

**Symptom**: Frame rate drops with many bindings

**Solutions**:
1. Reduce number of active bindings (only bind what's visible)
2. Avoid converters with expensive computations
3. Use direct property assignment for high-frequency updates
4. Profile to identify specific binding causing slowdown

## Advanced Topics

### Multiple Bindings on Same Property

Only the last binding wins. If you need to combine values, use a custom component:

```csharp
// ❌ DON'T: Second binding overwrites first
new Component()
{
    Bindings = 
    {
        Value = PropertyBinding.FromParent<A>().GetPropertyValue("X"),
        Value = PropertyBinding.FromParent<B>().GetPropertyValue("Y")  // Overwrites!
    }
}

// ✅ DO: Create combining component
public class CombinedValue : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    private float _result;
    
    [ComponentProperty]
    private float _valueA;
    partial void OnValueAChanged(float oldValue) => RecalculateResult();
    
    [ComponentProperty]
    private float _valueB;
    partial void OnValueBChanged(float oldValue) => RecalculateResult();
    
    private void RecalculateResult()
    {
        SetCurrentResult(ValueA + ValueB);  // Or any combination logic
    }
}
```

### Conditional Bindings

Use component visibility/activation to enable/disable bindings:

```csharp
// Binding only active when component is active
new ConditionalDisplay()
{
    IsActive = showDetails,  // Controls activation
    Bindings = 
    {
        // Only updates when IsActive = true
        Text = PropertyBinding
            .FromParent<Player>()
            .GetPropertyValue(nameof(Player.DetailedInfo))
    }
}
```

## Next Steps

- Read [data-model.md](./data-model.md) for detailed entity definitions
- Review [contracts/](./contracts/) for complete API reference
- See [research.md](./research.md) for design decisions and industry comparisons
- Check integration tests in `TestApp/` for working examples

## Questions?

Common questions answered:

**Q: Can I bind to non-ComponentProperty fields?**  
A: No, only properties with `[ComponentProperty]` and `NotifyChange = true` can be used as sources.

**Q: Can I bind to methods?**  
A: No, only properties are supported. Use computed properties instead.

**Q: Can I bind collections/arrays?**  
A: Not in the initial version. Future enhancement.

**Q: Can I animate bound properties?**  
A: Yes! Bindings use `SetCurrent{PropertyName}()` which supports interpolation if you pass an interpolation function.

**Q: What's the performance cost?**  
A: <5% overhead vs direct assignment. Uses reflection once during activation, then direct event subscription.
