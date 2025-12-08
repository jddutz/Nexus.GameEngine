# Quick Start: Property Binding Framework

**Feature**: `016-property-binding-v2`  
**Date**: December 7, 2025  
**Audience**: Developers implementing property bindings in component templates

## 5-Minute Overview

Property bindings enable declarative synchronization between component properties using fluent template syntax. Instead of manually subscribing to property change events, configure bindings in templates and let the framework handle lifecycle management.

### Before (Manual Event Subscription)

```csharp
public class HealthBar : Component
{
    private PlayerComponent? _player;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Find parent manually
        _player = FindParent<PlayerComponent>();
        if (_player != null)
        {
            // Subscribe manually
            _player.HealthChanged += OnPlayerHealthChanged;
            
            // Initial sync
            SetText($"Health: {_player.Health:F0}");
        }
    }
    
    private void OnPlayerHealthChanged(object? sender, PropertyChangedEventArgs<float> e)
    {
        SetText($"Health: {e.NewValue:F0}");
    }
    
    protected override void OnDeactivate()
    {
        // Cleanup manually
        if (_player != null)
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
        }
        base.OnDeactivate();
    }
}
```

### After (Property Binding)

```csharp
public class HealthBarTemplate : TextRendererTemplate
{
    public HealthBarTemplate()
    {
        Bindings = new[]
        {
            Binding.FromParent<PlayerComponent>()
                .GetPropertyValue(p => p.Health)
                .AsFormattedString("Health: {0:F0}")
                .Set(SetText)
        };
    }
}

// Usage:
ContentManager.CreateInstance(new HealthBarTemplate());
```

**Key Benefits**:
- ✅ No manual event subscription/cleanup
- ✅ Type-safe property access
- ✅ Automatic lifecycle management
- ✅ Declarative and readable

---

## Common Patterns

### Pattern 1: Parent-to-Child Property Flow

**Scenario**: Child component displays data from parent component.

```csharp
// Parent component with property
public class PlayerComponent : Component
{
    [ComponentProperty]
    public float Health { get; set; } = 100f;
}

// Child component template
public class HealthDisplayTemplate : TextRendererTemplate
{
    public HealthDisplayTemplate()
    {
        Bindings = new[]
        {
            Binding.FromParent<PlayerComponent>()
                .GetPropertyValue(p => p.Health)
                .AsFormattedString("HP: {0:F0}")
                .Set(SetText)
        };
    }
}
```

---

### Pattern 2: Sibling Communication

**Scenario**: Two sibling components synchronize state.

```csharp
// Sibling A: Data provider
public class ScoreComponent : Component
{
    [ComponentProperty]
    public int Points { get; set; }
}

// Sibling B: Data consumer
public class ScoreDisplayTemplate : TextRendererTemplate
{
    public ScoreDisplayTemplate()
    {
        Bindings = new[]
        {
            Binding.FromSibling<ScoreComponent>()
                .GetPropertyValue(s => s.Points)
                .AsFormattedString("Score: {0:N0}")
                .Set(SetText)
        };
    }
}
```

---

### Pattern 3: Global State Access

**Scenario**: Component binds to a named global state component.

```csharp
// Global singleton component
public class GameStateComponent : Component
{
    public GameStateComponent()
    {
        Name = "GameState";  // Must set name for lookup
    }
    
    [ComponentProperty]
    public int Level { get; set; } = 1;
}

// Any component can bind to it
public class LevelDisplayTemplate : TextRendererTemplate
{
    public LevelDisplayTemplate()
    {
        Bindings = new[]
        {
            Binding.FromNamedObject<GameStateComponent>("GameState")
                .GetPropertyValue(g => g.Level)
                .AsFormattedString("Level {0}")
                .Set(SetText)
        };
    }
}
```

---

### Pattern 4: Custom Type Conversion

**Scenario**: Convert property value using custom logic.

```csharp
// Custom converter
public class HealthPercentageConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        if (value is not float health) return null;
        return (health / 100f) * 100f;  // Normalize to percentage
    }
}

// Template using custom converter
public class HealthBarTemplate : UIElementTemplate
{
    public HealthBarTemplate()
    {
        Bindings = new[]
        {
            Binding.FromParent<PlayerComponent>()
                .GetPropertyValue(p => p.Health)
                .WithConverter(new HealthPercentageConverter())
                .Set(SetWidth)  // Set width as percentage
        };
    }
}
```

---

### Pattern 5: Multiple Bindings per Component

**Scenario**: Component binds to multiple source properties.

```csharp
public class PlayerHUDTemplate : UIElementTemplate
{
    public PlayerHUDTemplate()
    {
        Children = new Template[]
        {
            new TextRendererTemplate
            {
                Bindings = new[]
                {
                    Binding.FromContext<PlayerComponent>()
                        .GetPropertyValue(p => p.Health)
                        .AsFormattedString("Health: {0:F0}")
                        .Set(SetText)
                }
            },
            new TextRendererTemplate
            {
                Bindings = new[]
                {
                    Binding.FromContext<PlayerComponent>()
                        .GetPropertyValue(p => p.Mana)
                        .AsFormattedString("Mana: {0:F0}")
                        .Set(SetText)
                }
            },
            new TextRendererTemplate
            {
                Bindings = new[]
                {
                    Binding.FromContext<PlayerComponent>()
                        .GetPropertyValue(p => p.Experience)
                        .AsFormattedString("XP: {0:N0}")
                        .Set(SetText)
                }
            }
        };
    }
}
```

---

## Binding Configuration Methods

### Source Resolution (Choose One)

| Method | Description | Use Case |
|--------|-------------|----------|
| `FromParent<T>()` | Find nearest parent of type T | Parent-child data flow (default) |
| `FromSibling<T>()` | Find sibling of type T | Sibling communication |
| `FromChild<T>()` | Find immediate child of type T | Parent reads child state |
| `FromNamedObject<T>(name)` | Find component by name | Global singletons |
| `FromContext<T>()` | Find ancestor of type T | Context providers |

### Property Extraction (Required)

```csharp
.GetPropertyValue(component => component.PropertyName)
```

Extracts the property value from the source component. Type is inferred from the lambda expression.

### Value Conversion (Optional)

```csharp
// Built-in string formatting
.AsFormattedString("Format: {0:F2}")

// Custom converter
.WithConverter(new CustomConverter())
```

### Target Setter (Required)

```csharp
.Set(SetPropertyName)
```

Specifies the method to call when the source property changes. Must be a public method on the component.

---

## Fluent API Type Flow

The generic type parameter changes as you chain methods:

```csharp
// Start: PropertyBinding<PlayerComponent, PlayerComponent>
Binding.FromParent<PlayerComponent>()

    // After GetPropertyValue: PropertyBinding<PlayerComponent, float>
    .GetPropertyValue(p => p.Health)
    
    // After AsFormattedString: PropertyBinding<PlayerComponent, string>
    .AsFormattedString("Health: {0:F0}")
    
    // Terminal: Returns PropertyBinding<PlayerComponent, string>
    .Set(SetText)
```

This ensures type safety: the setter must accept the converted type.

---

## Lifecycle Integration

### Component Load Phase

```csharp
protected override void OnLoad(Template template)
{
    base.OnLoad(template);
    
    // Framework calls LoadPropertyBindings() automatically
    // Bindings added to Component.PropertyBindings collection
}
```

### Component Activation Phase

```csharp
protected override void OnActivate()
{
    base.OnActivate();
    
    // Framework calls ActivatePropertyBindings() automatically
    // Each binding:
    //   1. Resolves source component
    //   2. Subscribes to property change event
    //   3. Performs initial sync
}
```

### Component Deactivation Phase

```csharp
protected override void OnDeactivate()
{
    // Framework calls DeactivatePropertyBindings() automatically
    // Each binding:
    //   1. Unsubscribes from events
    //   2. Clears component references
    
    base.OnDeactivate();
}
```

**You don't need to call these methods manually** - the framework handles lifecycle automatically.

---

## Error Handling

### Configuration Errors (Fail Fast)

```csharp
// Invalid property selector - throws ArgumentException
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.SomeMethod())  // ❌ Not a property access
    .Set(SetText);

// Missing .Set() - binding silently ignored
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health);  // ❌ No target setter
```

### Runtime Errors (Log and Skip)

```csharp
// Source component not found - logs warning, skips activation
Binding.FromParent<NonExistentComponent>()  // ⚠️ Parent doesn't exist
    .GetPropertyValue(p => p.Value)
    .Set(SetValue);

// Property event not found - logs warning, binding won't update
Binding.FromParent<ComponentWithoutEvent>()  // ⚠️ HealthChanged event doesn't exist
    .GetPropertyValue(p => p.Health)
    .Set(SetText);
```

**Best Practice**: Check logs during development to catch configuration issues early.

---

## Performance Tips

1. **Prefer ParentLookup**: 90% faster than NamedObjectLookup
2. **Cache converters**: Reuse converter instances across bindings
3. **Simple format strings**: Complex formatting adds overhead
4. **Limit bindings**: Each binding adds ~1KB memory per active component

```csharp
// ✅ Good: Reuse converter
private static readonly IValueConverter _healthFormatter = 
    new StringFormatConverter("Health: {0:F0}");

public HealthBarTemplate()
{
    Bindings = new[]
    {
        Binding.FromParent<PlayerComponent>()
            .GetPropertyValue(p => p.Health)
            .WithConverter(_healthFormatter)  // Reused instance
            .Set(SetText)
    };
}

// ❌ Bad: New converter per instance
public HealthBarTemplate()
{
    Bindings = new[]
    {
        Binding.FromParent<PlayerComponent>()
            .GetPropertyValue(p => p.Health)
            .AsFormattedString("Health: {0:F0}")  // Creates new converter
            .Set(SetText)
    };
}
```

---

## Troubleshooting

### Binding Not Updating

**Check**:
1. Source component has `{PropertyName}Changed` event
2. Source property raises event when changed
3. Binding successfully activated (check logs)
4. Target setter method is public and accessible

### Memory Leaks

**Check**:
1. Component.OnDeactivate() is being called
2. DeactivatePropertyBindings() is called in OnDeactivate()
3. No circular references between components

### Type Mismatch Errors

**Check**:
1. Converter output type matches setter input type
2. Property type matches GetPropertyValue<T> type parameter
3. Custom converter returns correct type

---

## Next Steps

1. **Read API Contracts**: See `contracts/` folder for detailed API documentation
2. **Review Data Model**: See `data-model.md` for entity relationships
3. **Check Research**: See `research.md` for architectural decisions
4. **Write Tests**: Follow TDD workflow in `.github/copilot-instructions.md`

---

## Full Example: Complete Component

```csharp
// Player.cs - Data source
public class PlayerComponent : Component
{
    [ComponentProperty]
    public float Health { get; set; } = 100f;
    
    [ComponentProperty]
    public float Mana { get; set; } = 50f;
    
    [ComponentProperty]
    public string Name { get; set; } = "Player";
}

// PlayerHUD.cs - UI display
public class PlayerHUDTemplate : UIElementTemplate
{
    public PlayerHUDTemplate()
    {
        Position = new Vector2D<float>(10, 10);
        
        Children = new Template[]
        {
            // Player name
            new TextRendererTemplate
            {
                Position = new Vector2D<float>(0, 0),
                Bindings = new[]
                {
                    Binding.FromParent<PlayerComponent>()
                        .GetPropertyValue(p => p.Name)
                        .Set(SetText)
                }
            },
            
            // Health bar
            new TextRendererTemplate
            {
                Position = new Vector2D<float>(0, 20),
                Bindings = new[]
                {
                    Binding.FromParent<PlayerComponent>()
                        .GetPropertyValue(p => p.Health)
                        .AsFormattedString("Health: {0:F0}")
                        .Set(SetText)
                }
            },
            
            // Mana bar
            new TextRendererTemplate
            {
                Position = new Vector2D<float>(0, 40),
                Bindings = new[]
                {
                    Binding.FromParent<PlayerComponent>()
                        .GetPropertyValue(p => p.Mana)
                        .AsFormattedString("Mana: {0:F0}")
                        .Set(SetText)
                }
            }
        };
    }
}

// Usage in game
var player = ContentManager.CreateInstance(new PlayerComponentTemplate
{
    Health = 100f,
    Mana = 50f,
    Children = new[]
    {
        new PlayerHUDTemplate()  // HUD automatically binds to parent player
    }
});

// Update player - HUD updates automatically
player.Health = 75f;  // Raises HealthChanged event → HUD text updates
```

---

## Summary

- ✅ Use `Binding.FromParent<T>()` for most scenarios
- ✅ Chain `.GetPropertyValue()` to extract property
- ✅ Add `.AsFormattedString()` for text formatting
- ✅ End with `.Set()` to specify target setter
- ✅ Framework handles lifecycle automatically
- ✅ Check logs for configuration issues
