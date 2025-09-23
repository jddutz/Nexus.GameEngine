# NexusRealms Game Engine

A modern .NET 9 game engine built with Silk.NET, featuring a component-based architecture with dependency injection and template-driven configuration.

## Quick Start

```bash
# Build the solution
dotnet build NexusRealms.sln --configuration Debug

# Run tests
dotnet test
```

## RuntimeComponent System Design Summary

**Core Principle**: Everything is an IRuntimeComponent that can contain other components via a Children property. Components implement only the behavior interfaces they need.

### Architecture Overview

The RuntimeComponent system is built on a hierarchical composition model where:

1. **Universal Interface**: All game objects, UI elements, input handlers, and system components implement `IRuntimeComponent`
2. **Tree Structure**: Components form parent-child relationships through the `Children` property and `Parent` references
3. **Behavioral Composition**: Components implement specific behavior interfaces (`IRenderable`, `IUpdatable`, etc.) only when needed
4. **Template-Based Configuration**: Components are configured using strongly-typed template records
5. **Dependency Injection**: Components receive dependencies through constructor injection
6. **Lifecycle Management**: Automatic activation, validation, and disposal cascades through the component tree

### Core Interfaces and Types

See the actual interface definitions in the codebase:

- [`IRuntimeComponent`](GameEngine/Components/IRuntimeComponent.cs) - Base component interface
- [`IRuntimeComponent<T>`](GameEngine/Components/IRuntimeComponent.cs) - Generic typed component interface
- [`ComponentTemplate`](GameEngine/Components/ComponentTemplate.cs) - Base template class

### Component Categories

**System Components:**

- `InputMap`: Manages collections of input bindings
- `SceneGraph`: Manages spatial hierarchy of game objects
- `UserInterfaceManager`: Manages UI component trees

**Input Components:**

- `KeyBinding<TAction>`: Maps keyboard input to actions
- `MouseBinding<TAction>`: Maps mouse input to actions
- `GamepadBinding<TAction>`: Maps gamepad input to actions

**UI Components:**

- `Border`: Renders borders around content areas
- `TextElement`: Displays text content
- `VerticalLayout`: Arranges children vertically
- `HorizontalLayout`: Arranges children horizontally

**Game Components:**

- `GameObject`: Base for game entities
- `Transform`: Spatial positioning and hierarchy
- `Renderer`: Visual representation
- `Collider`: Physical interaction boundaries

### Template System

Templates are immutable configuration records that define component properties. See examples in the codebase:

- [`KeyBindingTemplate`](GameEngine/Input/KeyBinding.cs) - Simple value template
- [`InputMapTemplate`](GameEngine/Input/Components/InputMap.cs) - Configuration template
- [`BorderTemplate`](GameEngine/GUI/Components/Border.cs) - UI component template

### Component Implementation Patterns

**Base Class Pattern:**
See [`RuntimeComponent<TTemplate>`](GameEngine/Components/RuntimeComponent.cs) which provides default implementations for:

- Component tree management
- Lifecycle methods (Activate/Deactivate/Dispose)
- Fluent configuration
- Validation framework

**Specialized Component Pattern:**
See actual implementations in the codebase:

- [`KeyBinding<TAction>`](GameEngine/Input/Components/KeyBinding.cs) - Input component with dependency injection
- [`InputMap`](GameEngine/Input/Components/InputMap.cs) - Component collection management
- [`Border`](GameEngine/GUI/Components/Border.cs) - UI component with rendering

### Lifecycle Management

Components follow a strict lifecycle with automatic cascading:

1. **Creation**: `factory.Create<T>()` with dependency injection
2. **Configuration**: `.Configure(template)` applies settings
3. **Validation**: `.Validate()` checks configuration integrity
4. **Activation**: `.Activate()` enables functionality (cascades to children)
5. **Operation**: Component performs its function while `IsEnabled = true`
6. **Deactivation**: `.Deactivate()` disables functionality (cascades to children)
7. **Disposal**: `.Dispose()` releases resources (cascades to children)

### Tree Operations

**Navigation:**

```csharp
// Find specific child types
var textElements = layout.GetChildren<TextElement>();

// Find siblings with filtering
var siblingButtons = button.GetSiblings<Button>(b => b.IsEnabled);

// Find parent containers
var parentLayout = element.FindParent<VerticalLayout>();
```

**Modification:**

```csharp
// Add/remove children (automatically sets Parent property)
parentComponent.AddChild(childComponent);
parentComponent.RemoveChild(childComponent);

// Children collection reflects current tree state
foreach (var child in component.Children) { }
```

### Behavioral Interfaces

Components implement behavior interfaces as needed. See examples in the codebase:

- [`IRenderable`](GameEngine/Graphics/IRenderable.cs) - For visual components
- [`IUpdatable`](GameEngine/Runtime/IUpdatable.cs) - For components requiring frame updates
- [`IInputHandler`](GameEngine/Input/IInputHandler.cs) - For input processing components

### Error Handling and Validation

Built-in validation system with severity levels:

```csharp
protected override IEnumerable<ValidationError> OnValidate()
{
    if (string.IsNullOrEmpty(Name))
    {
        yield return new ValidationError(this,
            "Component should have a name for debugging",
            ValidationSeverity.Warning);
    }

    if (_requiredDependency == null)
    {
        yield return new ValidationError(this,
            "Required dependency not injected",
            ValidationSeverity.Error);
    }
}
```

## Component Factory and Dependency Injection Patterns

The system uses a fluent factory pattern with dependency injection for component creation:

### 1. Dependency Injection Setup

See [`ServiceCollectionExtensions.cs`](GameEngine/Components/ServiceCollectionExtensions.cs) for the complete setup pattern:

```csharp
// In Program.cs or startup configuration
var services = new ServiceCollection();
services.AddLogging();
services.AddComponentFactory(); // Extension method from ServiceCollectionExtensions

// Register your components and their dependencies
services.AddSingleton<QuitGameAction>();
services.AddSingleton<ToggleFullscreenAction>();

var serviceProvider = services.BuildServiceProvider();
var factory = serviceProvider.GetRequiredService<IComponentFactory>();
```

### 2. Template Definition Pattern

Define templates as simple record types with configuration properties. See examples:

- [`KeyBindingTemplate`](GameEngine/Input/KeyBinding.cs) - Input binding configuration
- [`InputMapTemplate`](GameEngine/Input/Components/InputMap.cs) - Input mapping configuration
- [`BorderTemplate`](GameEngine/GUI/Components/Border.cs) - UI component styling

### 3. Component Implementation with Configure Method

See complete implementation examples in the codebase:

- [`KeyBinding<TAction>`](GameEngine/Input/Components/KeyBinding.cs) - Constructor injection, configuration, and lifecycle methods
- [`InputMap`](GameEngine/Input/Components/InputMap.cs) - Collection management and validation
- [`VerticalLayout`](GameEngine/GUI/Components/VerticalLayout.cs) - Child component management

### 4. Fluent Component Creation Pattern

```csharp
// Basic component creation with fluent configuration
var keyBinding = factory.Create<KeyBinding<QuitGameAction>>()
    .Configure(new KeyBindingTemplate
    {
        Key = Key.Escape,
        Name = "Quit Game"
    });

// Complex component with child components
var inputMap = factory.Create<InputMap>()
    .Configure(new InputMapTemplate
    {
        Name = "Main Controls",
        EnabledByDefault = true
    });

// Add children to component tree
inputMap.AddChild(keyBinding);
```

### 5. Polymorphic Component Creation with Pattern Matching

Use pattern matching for creating different component types based on template types:

```csharp
public List<IRuntimeComponent> CreateInputBindingsFromTemplates(
    IComponentFactory factory,
    object[] templates)
{
    var components = new List<IRuntimeComponent>();

    foreach (var template in templates)
    {
        var component = template switch
        {
            KeyBindingTemplate keyTemplate =>
                factory.Create<KeyBinding<QuitGameAction>>().Configure(keyTemplate),

            MouseBindingTemplate mouseTemplate =>
                factory.Create<MouseBinding<QuitGameAction>>().Configure(mouseTemplate),

            GamepadBindingTemplate gamepadTemplate =>
                factory.Create<GamepadBinding<QuitGameAction>>().Configure(gamepadTemplate),

            _ => throw new ArgumentException($"Unsupported template type: {template.GetType()}")
        };

        components.Add(component);
    }

    return components;
}
```

### 6. Component with Child Management Pattern

See [`VerticalLayout`](GameEngine/GUI/Components/VerticalLayout.cs) for a complete example of:

- Child component collection management
- Template-based configuration with subcomponents
- Proper parent-child relationship handling
- Layout-specific lifecycle methods

### 7. Validation Pattern

See validation examples in the codebase:

- [`InputBinding`](GameEngine/Input/Components/InputBinding.cs) - Base validation for input components
- [`InputMap`](GameEngine/Input/Components/InputMap.cs) - Collection validation with warnings and errors
- [`RuntimeComponent<T>`](GameEngine/Components/RuntimeComponent.cs) - Base validation framework

### 8. Component Lifecycle Best Practices

```csharp
// 1. Create component tree
var rootComponent = factory.Create<InputMap>()
    .Configure(new InputMapTemplate { Name = "Root" });

// 2. Add child components
var childComponent = factory.Create<KeyBinding<QuitGameAction>>()
    .Configure(new KeyBindingTemplate { Key = Key.Escape });
rootComponent.AddChild(childComponent);

// 3. Validate before activation
var errors = rootComponent.Validate();
if (errors.Any(e => e.Severity == ValidationSeverity.Error))
{
    throw new InvalidOperationException("Component validation failed");
}

// 4. Activate component tree (will activate all children)
rootComponent.Activate();

// 5. Use component...

// 6. Properly dispose when done
rootComponent.Dispose(); // Will dispose all children
```

## Project Structure

- `.docs/`: Additional documentation
- `GameEngine/`: Core engine components and systems
- `Tests/`: Unit tests for engine functionality

## Dependencies

- .NET 9
- Silk.NET (OpenGL, Input, Windowing)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- xUnit (testing)
