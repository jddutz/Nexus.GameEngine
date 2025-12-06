# Property Binding System

The Property Binding System allows you to declaratively link properties between components. This eliminates manual event subscription boilerplate and enables reactive UI patterns where components automatically update when their data sources change.

## Overview

Bindings are defined in the `OnLoad` method of your components or via Templates. A binding consists of:
1. **Target**: The component property that receives the value.
2. **Source**: The component property that provides the value.
3. **Lookup Strategy**: How to find the source component relative to the target.
4. **Converter** (Optional): Logic to transform the value (e.g., `float` -> `string`).
5. **Mode**: `OneWay` (default) or `TwoWay`.

## Basic Usage

The `Binding` static class provides a fluent API for creating bindings.

```csharp
public override void OnLoad(Template? template)
{
    base.OnLoad(template);

    // Bind this.CurrentHealth to Parent.Health
    Bindings.Add(
        nameof(CurrentHealth), 
        Binding.FromParent<PlayerCharacter>(p => p.Health)
    );
}
```

## Lookup Strategies

The system supports several strategies to locate the source component:

### 1. From Parent (`Binding.FromParent<T>`)
Searches for the first parent component of the specified type.
*   **Use Case**: Child components binding to their container's data (e.g., HealthBar inside a PlayerFrame).

```csharp
Binding.FromParent<PlayerCharacter>(p => p.Health)
```

### 2. From Sibling (`Binding.FromSibling<T>`)
Searches the parent's children for the first sibling of the specified type.
*   **Use Case**: Coordinated components (e.g., a Label binding to a Slider in the same container).

```csharp
Binding.FromSibling<Slider>(s => s.Value)
```

### 3. From Child (`Binding.FromChild<T>`)
Searches the component's own children for the first child of the specified type.
*   **Use Case**: A container binding to the state of one of its children.

```csharp
Binding.FromChild<InputField>(i => i.Text)
```

### 4. From Named Object (`Binding.FromNamedObject<T>`)
Searches the entire component tree for a component with a specific name.
*   **Use Case**: Binding to a global or well-known component (e.g., "GameStateManager").

```csharp
Binding.FromNamedObject<GameState>("GlobalState", s => s.Score)
```

### 5. From Context (`Binding.FromContext<T>`)
Searches up the ancestor tree (parent, grandparent, etc.) for the first component of the specified type.
*   **Use Case**: Theming or shared context (e.g., finding a `Theme` provider anywhere up the hierarchy).

```csharp
Binding.FromContext<Theme>(t => t.PrimaryColor)
```

## Value Converters

Converters transform values as they pass from source to target.

### String Formatting
Use `.AsFormattedString()` for simple text formatting.

```csharp
// Displays "Health: 75%"
Binding.FromParent<Player>(p => p.Health)
       .AsFormattedString("Health: {0:F0}%")
```

### Custom Converters
Implement `IValueConverter` for custom logic.

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter)
    {
        return (bool)value! ? Color.Green : Color.Red;
    }
}

// Usage
Binding.FromParent<Status>(s => s.IsReady)
       .WithConverter(new BoolToColorConverter())
```

## Two-Way Bindings

Two-way bindings synchronize values in both directions. If the target property changes, the source is updated.

*   **Requirement**: To use a converter with TwoWay bindings, it must implement `IBidirectionalConverter`.
*   **Loop Prevention**: The system automatically prevents infinite update loops.

```csharp
// Slider controls Volume, and Volume updates Slider
Binding.TwoWay<AudioSettings>(s => s.MasterVolume)
```

## Best Practices

1.  **Prefer `FromParent`**: It creates the most coupled but predictable relationships.
2.  **Use `FromContext` for Styling**: Great for cascading styles without passing data through every layer.
3.  **Avoid Deep Trees with `FromNamedObject`**: Searching the entire tree can be expensive if overused; prefer relative lookups.
4.  **Type Safety**: Always use the generic `Binding.FromX<T>(x => x.Prop)` methods to ensure compile-time safety for property names.
