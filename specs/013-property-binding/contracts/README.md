# Property Binding API Contracts

**Feature**: Property Binding System  
**Branch**: 013-property-binding  
**Date**: December 6, 2025

## Overview

This directory contains the API contract definitions for the Property Binding System. These interfaces and classes define the public API surface that developers will interact with when configuring property bindings in their component templates.

## Contract Files

### Core Binding Contracts

- **PropertyBinding.cs** - Core binding class with fluent API for configuration and internal lifecycle methods
- **PropertyBindings.cs** - Abstract base class for source-generated binding configuration classes
- **BindingMode.cs** - Enum defining OneWay vs TwoWay binding modes

### Lookup Strategy Contracts

- **ILookupStrategy.cs** - Interface for component resolution strategies (parent, sibling, child, context, named)

### Value Conversion Contracts

- **IValueConverter.cs** - One-way value transformation interface
- **IBidirectionalConverter.cs** - Two-way value transformation interface extending IValueConverter

### Event Contracts

- **PropertyChangedEventArgs.cs** - Generic event arguments for property change notifications

## Usage Examples

### Basic Parent-to-Child Binding

```csharp
new HealthBarTemplate()
{
    Bindings = 
    {
        CurrentHealth = PropertyBinding
            .FromParent<PlayerCharacter>()
            .GetPropertyValue(nameof(PlayerCharacter.Health))
    }
}
```

### Binding with Value Conversion

```csharp
new TextDisplayTemplate()
{
    Bindings = 
    {
        Text = PropertyBinding
            .FromParent<PlayerCharacter>()
            .GetPropertyValue(nameof(PlayerCharacter.Health))
            .AsFormattedString("Health: {0:F1}")
    }
}
```

### Two-Way Binding with Custom Converter

```csharp
new SliderTemplate()
{
    Bindings = 
    {
        Value = PropertyBinding
            .FromContext<AudioSettings>()
            .GetPropertyValue(nameof(AudioSettings.MasterVolume))
            .WithConverter(new PercentageConverter())
            .TwoWay()
    }
}
```

### Named Component Binding

```csharp
new HealthBarTemplate()
{
    Bindings = 
    {
        CurrentHealth = PropertyBinding
            .FromNamedObject("PlayerCharacter")
            .GetPropertyValue(nameof(PlayerCharacter.Health))
    }
}
```

## Design Principles

1. **Fluent API**: Chainable methods for readable binding configuration
2. **Type Safety**: Generic constraints ensure compile-time type checking where possible
3. **Fail-Safe**: Missing components return null (no exceptions)
4. **Explicit**: All binding configuration visible in templates
5. **Lifecycle-Aware**: Bindings activate/deactivate with component lifecycle
6. **Performance**: Zero allocations after activation, cached reflection results

## Implementation Notes

### PropertyBinding Lifecycle

1. **Configuration** (fluent API calls)
   - `FromParent<T>()` / `FromSibling<T>()` / etc. → sets `ILookupStrategy`
   - `GetPropertyValue(name)` → sets `SourcePropertyName`
   - `WithConverter()` / `AsFormattedString()` → sets `IValueConverter`
   - `TwoWay()` → sets `Mode = BindingMode.TwoWay`

2. **Activation** (Component.OnActivate)
   - `Activate(targetComponent, targetPropertyName)` called
   - Resolve source component using `LookupStrategy.Resolve()`
   - Reflect and cache source/target PropertyChanged events
   - Subscribe to source PropertyChanged event
   - Perform initial sync (source → target)
   - If TwoWay, subscribe to target PropertyChanged event

3. **Runtime** (event-driven updates)
   - Source PropertyChanged fires → handler invoked
   - If converter, call `converter.Convert(newValue)`
   - Call target's `SetCurrent{PropertyName}(convertedValue)`
   - If TwoWay, check `_isUpdating` flag to prevent cycles

4. **Deactivation** (Component.OnDeactivate)
   - `Deactivate()` called
   - Unsubscribe from source PropertyChanged
   - Unsubscribe from target PropertyChanged (TwoWay)
   - Clear cached references

### Source Generator Integration

**ComponentPropertyGenerator** modifications:
- Add `NotifyChange` parameter to `[ComponentProperty]` attribute
- Generate `{PropertyName}Changed` event for properties with `NotifyChange = true`
- Generate `On{PropertyName}Changed` partial method that invokes the event

**TemplateGenerator** modifications:
- Add `Bindings` property to each generated template record
- Type is `{ComponentName}PropertyBindings`
- Initialized with `new()` by default

**PropertyBindingsGenerator** (new):
- Generate `{ComponentName}PropertyBindings` class for each component with `[ComponentProperty]` attributes
- One nullable `PropertyBinding?` property per component property
- Override `GetEnumerator()` to yield non-null bindings

## Validation Requirements

| Contract | Validation |
|----------|------------|
| ILookupStrategy | MUST return null for not-found (no exceptions) |
| IValueConverter | MUST NOT throw exceptions (caught and logged) |
| IBidirectionalConverter | ConvertBack MUST be inverse of Convert |
| PropertyBinding | LookupStrategy and SourcePropertyName required before Activate |
| BindingMode.TwoWay | Requires IBidirectionalConverter if converter used |

## Extension Points

Developers can extend the binding system by:

1. **Custom Lookup Strategies**: Implement `ILookupStrategy` for domain-specific resolution
2. **Custom Converters**: Implement `IValueConverter` or `IBidirectionalConverter` for specialized transformations
3. **Custom Binding Modes**: Future enhancement could add additional modes (OneTime, OneWayToSource)
