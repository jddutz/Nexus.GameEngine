# Deferred Property Generation System

## Overview

A source generator-based system that automatically implements animated property updates for all auto-properties with public setters in `IRuntimeComponent` implementations. Properties support:

- **Deferred updates with interpolation** - Changes animate smoothly via keyframe interpolation
- **PropertyChanged notifications** - Automatic change tracking
- **Configurable animation** - Override interpolation mode, duration, and easing
- **Zero runtime overhead** - All code generated at compile time

## Problem Statement

Components need properties that:

1. **Animate smoothly** - Interpolate between values over time (not just defer to next frame)
2. **Support PropertyChanged notifications** - For data binding and reactive patterns
3. **Remain temporally consistent** - Values don't change unexpectedly mid-frame
4. **Are configurable per-property** - Different animation modes, durations, easing functions
5. **Have zero performance overhead** - No reflection, no runtime wrapping

Manual implementation requires significant boilerplate:

- Backing fields (current, target, animation state)
- Property getters/setters with change detection
- Interpolation logic per property type
- Animation timing and easing calculations
- NotifyPropertyChanged() calls

## Solution Architecture

### **Core Principle: Convention over Configuration**

**Generation Rule:**

- Automatically generate animated property implementation for **any auto-property with a public setter** in classes implementing `IRuntimeComponent`
- Only generate for properties declared directly on the component class, not inherited properties
- Infrastructure properties live in a base class that does NOT implement `IRuntimeComponent`
- **NO `partial` keyword required** - generator handles everything automatically

### **Class Hierarchy**

```
ComponentBase (no IRuntimeComponent)
├── ComponentId Id
├── string Name
├── bool IsEnabled
├── ILogger? Logger
├── IComponentFactory? ComponentFactory
├── List<IRuntimeComponent> Children
└── Infrastructure methods

RuntimeComponent : ComponentBase, IRuntimeComponent
├── (inherits all infrastructure properties)
└── (source generation applies here)

TextElement : RuntimeComponent (auto-generated as partial)
├── float FontSize { get; set; }           // ✅ GENERATED
├── Vector4D<float> Color { get; set; }    // ✅ GENERATED
└── string? Text { get; set; }             // ✅ GENERATED
```

### **Why This Works**

1. **Infrastructure properties are inherited** - No need to mark with attributes
2. **Generator only looks at IRuntimeComponent implementers** - Clean separation
3. **Generator ignores base class properties** - Only processes properties declared on the component itself
4. **No special cases or exclusion lists** - Architecture enforces the distinction
5. **Clear semantic separation** - Infrastructure vs. runtime-mutable properties

## Technical Design

### **Source Generator Trigger Conditions**

Generate code when ALL of the following are true:

1. Class implements `IRuntimeComponent` (directly or via inheritance from `RuntimeComponent`)
2. Property is an auto-property (no explicit getter/setter body)
3. Property has a public getter AND public setter
4. Property is declared directly on the class (not inherited from base classes)

**Implementation Strategy**:

Since C# doesn't support implementing a partial property without declaring it as `partial`, we use a hybrid approach:

1. **Class must be declared `partial`** - Required for generator to extend it
2. **Properties declared normally** - No `partial` keyword needed on properties
3. **Source generator creates full implementation** - Backing fields, property implementation, animation logic

**How it works:**

The source file contains the property declaration (which gets ignored by the compiler in favor of the generated version):

```csharp
public partial class TextElement : RuntimeComponent
{
    public float FontSize { get; set; } = 12f;  // Declaration (for developer reference)
}
```

The generator creates `TextElement.g.cs` with the actual implementation:

```csharp
partial class TextElement
{
    private float _fontSize = 12f;
    private float _targetFontSize = 12f;

    public float FontSize
    {
        get => _fontSize;
        set { /* animation logic */ }
    }
}
```

**Important**: The auto-property in the source file is essentially a "declaration" that tells the generator what properties to create. The generated implementation takes precedence during compilation.

**Limitation**: Property initializers in source must match generated defaults, or should be moved to constructors/OnConfigure.

### **Generated Code Pattern**

**Input:**

```csharp
public partial class TextElement : RuntimeComponent
{
    // No animation - instant update (no attribute needed)
    public float FontSize { get; set; } = 12f;

    // Animated with cubic easing
    [Animation(Duration = 0.5f, Interpolation = InterpolationMode.CubicEaseInOut)]
    public Vector4D<float> Color { get; set; }
}
```

**Generated Output:**

```csharp
// In TextElement.g.cs
partial class TextElement
{
    // Backing fields for FontSize (instant update)
    private float _fontSize = 12f;
    private float _targetFontSize = 12f;

    // Backing fields for Color (animated)
    private Vector4D<float> _color;
    private Vector4D<float> _targetColor;
    private PropertyAnimation<Vector4D<float>> _colorAnimation = new()
    {
        Duration = 0.5f,
        Interpolation = InterpolationMode.CubicEaseInOut
    };

    // FontSize property implementation (instant - Duration = 0)
    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (EqualityComparer<float>.Default.Equals(_targetFontSize, value))
                return;

            _targetFontSize = value;
            // Note: Does NOT update _fontSize immediately
            // Update happens in ApplyUpdates() during next frame
        }
    }

    // Color property implementation (animated)
    public Vector4D<float> Color
    {
        get => _color;
        set
        {
            if (_targetColor == value)
                return;

            _targetColor = value;
            _colorAnimation.StartAnimation(_color, _targetColor, TimeProvider.Current);
        }
    }

    // Animation update called during component update
    partial void ApplyUpdates(double deltaTime)
    {
        // FontSize: Instant update (Duration = 0)
        if (_targetFontSize != _fontSize)
        {
            OnPropertyAnimationStarted(nameof(FontSize));
            _fontSize = _targetFontSize;  // Apply immediately
            NotifyPropertyChanged(nameof(FontSize));
            OnPropertyAnimationEnded(nameof(FontSize));
        }

        // Color: Animated update (Duration > 0)
        if (_colorAnimation.IsAnimating)
        {
            _color = _colorAnimation.Update(deltaTime);
            NotifyPropertyChanged(nameof(Color));

            if (!_colorAnimation.IsAnimating)
            {
                OnPropertyAnimationEnded(nameof(Color));
            }
        }
    }
}
```

### **Property Types Supported**

- **Primitives**: `int`, `float`, `double`, `bool`, etc.
- **Structs**: `Vector4D<float>`, `Rectangle<int>`, `Quaternion`, custom structs
- **Reference Types**: `string`, `Camera`, `List<T>`, etc. (instant update, no interpolation)
- **Nullable Types**: `string?`, `int?`, reference types with nullable annotations

### **Animation Configuration**

Properties can be configured with a single `[Animation]` attribute:

#### **`[Animation]` Attribute**

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class AnimationAttribute : Attribute
{
    public float Duration { get; set; } = 0f;           // Seconds (0 = instant)
    public InterpolationMode Interpolation { get; set; } = InterpolationMode.Linear;
}
```

**Usage Examples:**

```csharp
// Instant update (default behavior, no attribute needed)
public float FontSize { get; set; } = 12f;

// Animated with linear interpolation over 0.3 seconds
[Animation(Duration = 0.3f)]
public float FontSize { get; set; } = 12f;

// Animated with cubic interpolation
[Animation(Duration = 0.5f, Interpolation = InterpolationMode.Cubic)]
public Vector4D<float> Color { get; set; }

// Spherical interpolation for quaternions
[Animation(Duration = 0.2f, Interpolation = InterpolationMode.Slerp)]
public Quaternion Rotation { get; set; }
```

#### **InterpolationMode Enum**

```csharp
public enum InterpolationMode
{
    Linear,         // Standard linear interpolation (Lerp)
    LinearEaseIn,   // Linear with ease-in acceleration
    LinearEaseOut,  // Linear with ease-out deceleration
    LinearEaseInOut,// Linear with ease-in-out (S-curve)

    Cubic,          // Cubic spline interpolation
    CubicEaseIn,    // Cubic with ease-in
    CubicEaseOut,   // Cubic with ease-out
    CubicEaseInOut, // Cubic with ease-in-out

    Slerp,          // Spherical linear interpolation (quaternions, unit vectors)

    Step            // No interpolation, snap to target immediately
}
```

**Default Behavior (No Attribute):**

- Duration = 0 (instant update between frames)
- Interpolation = Linear (doesn't matter when duration is 0)

**Type Restrictions:**
The following types support animation:

- ✅ **Numeric primitives**: `float`, `double`, `int`, `long`, `byte`, etc.
- ✅ **Vectors**: `Vector2D`, `Vector3D`, `Vector4D`
- ✅ **Quaternions**: `Quaternion`
- ✅ **Matrices**: `Matrix3x3`, `Matrix4x4`
- ✅ **Structs implementing `IInterpolatable<T>`**

The following types **cannot** be animated (compile-time error):

- ❌ **Reference types**: `string`, `object`, classes
- ❌ **Collections**: `List<T>`, `Dictionary<K,V>`, arrays
- ❌ **Non-numeric types**: `bool`, `char`, enums (without custom interpolation)

These will generate analyzer error **NX1002** if `[Animation]` attribute is applied.

### **Animation Lifecycle**

**Example 1: Instant Update (Duration = 0)**

```
Frame N: Property Set
├── textElement.Width = 200f (no [Animation] attribute, Duration = 0)
├── _targetWidth = 200f
└── Width getter still returns 100f (change deferred)

Frame N+1: Next Update
├── component.OnUpdate(deltaTime) called
├── ApplyUpdates(deltaTime) called
├── Check: _targetWidth != _width → true
├── OnPropertyAnimationStarted("Width") fired
├── _width = _targetWidth (apply instantly)
├── NotifyPropertyChanged("Width")
├── OnPropertyAnimationEnded("Width") fired
└── Width getter now returns 200f (updated between frames)
```

**Example 2: Animated Update (Duration > 0)**

```
Frame 0 (t=0.0s): Property Set
├── textElement.FontSize = 24f (has [Animation(Duration = 0.5f)])
├── _targetFontSize = 24f
├── _fontSizeAnimation.StartAnimation(currentValue: 12f, targetValue: 24f, startTime: 0.0s)
├── OnPropertyAnimationStarted("FontSize") fired
└── FontSize getter still returns 12f (animation starting)

Frame 1 (t=0.016s, deltaTime=0.016s):
├── component.OnUpdate(0.016) called
├── ApplyUpdates(0.016) called
├── _fontSizeAnimation.Update(0.016) → interpolates to ~12.5f
├── _fontSize = 12.5f
├── NotifyPropertyChanged("FontSize")
└── FontSize getter now returns 12.5f

Frame 2 (t=0.032s, deltaTime=0.016s):
├── ApplyUpdates(0.016)
├── _fontSizeAnimation.Update(0.016) → interpolates to ~13.2f
└── FontSize getter returns 13.2f

... (animation continues over configured duration)

Frame 31 (t=0.5s, animation complete):
├── _fontSizeAnimation.Update(deltaTime) → returns 24f
├── _fontSizeAnimation.IsAnimating = false
├── OnPropertyAnimationEnded("FontSize") fired
└── FontSize getter returns 24f (target reached)
```

**Key Behavior:**

- **All property updates are deferred** - Changes only apply during `ApplyUpdates()`
- **Duration = 0** means instant update (not immediate) - happens at next `ApplyUpdates()` call
- **Duration > 0** means interpolated update over time
- **PropertyChanged events** fire when value actually changes, not when setter is called
- **Animation events** fire at start and completion of property changes

## Compiler Integration

### **Roslyn Analyzer Rules**

**NX1001**: Class implementing IRuntimeComponent must be declared partial

- **Severity**: Error
- **Trigger**: Class implements `IRuntimeComponent` but is not declared `partial`
- **Fix**: Add `partial` keyword to class declaration
- **Reason**: Generator needs to extend the class with property implementations

**NX1002**: Animation attribute on non-animatable type

- **Severity**: Error
- **Trigger**: `[Animation]` attribute on property of non-animatable type (string, object, bool, etc.)
- **Fix**: Remove `[Animation]` attribute or change property type to numeric/vector/matrix
- **Example**: `[Animation] public string Text { get; set; }` ← Error

**NX1003**: Invalid interpolation mode for property type

- **Severity**: Warning
- **Trigger**: Incompatible interpolation mode for type (e.g., `Slerp` on `float`)
- **Fix**: Use appropriate interpolation mode for the type
- **Example**: `[Animation(Interpolation = InterpolationMode.Slerp)] public float Value { get; set; }` ← Warning (Slerp is for quaternions)

## Animation System Details

### **PropertyAnimation<T> Class**

The generated code uses a `PropertyAnimation<T>` class to manage interpolation:

```csharp
public class PropertyAnimation<T> where T : struct
{
    public float Duration { get; set; }
    public InterpolationMode Interpolation { get; set; }

    public bool IsAnimating { get; private set; }

    private T _startValue;
    private T _endValue;
    private double _startTime;
    private double _elapsed;

    public void StartAnimation(T startValue, T endValue, double currentTime)
    {
        _startValue = startValue;
        _endValue = endValue;
        _startTime = currentTime;
        _elapsed = 0.0;
        IsAnimating = Duration > 0;
    }

    public T Update(double deltaTime)
    {
        if (!IsAnimating) return _endValue;

        _elapsed += deltaTime;

        if (_elapsed >= Duration)
        {
            IsAnimating = false;
            return _endValue;
        }

        float t = (float)(_elapsed / Duration);

        // Apply interpolation (includes easing if mode has EaseIn/Out/InOut)
        return Interpolate(_startValue, _endValue, t, Interpolation);
    }
}
```

**Notes:**

- Easing is built into the InterpolationMode (e.g., `LinearEaseIn`, `CubicEaseOut`)
- Duration = 0: Property doesn't use `PropertyAnimation<T>`, updated directly in `ApplyUpdates()`
- Duration > 0: Property uses `PropertyAnimation<T>` for interpolation over time
- Interpolation strategy is chosen based on property type and InterpolationMode

**Generated code handles instant (Duration = 0) properties differently:**

```csharp
// Property WITHOUT [Animation] attribute - no PropertyAnimation instance
partial void ApplyUpdates(double deltaTime)
{
    if (_targetWidth != _width)
    {
        OnPropertyAnimationStarted(nameof(Width));
        _width = _targetWidth;  // Direct assignment
        NotifyPropertyChanged(nameof(Width));
        OnPropertyAnimationEnded(nameof(Width));
    }
}

// Property WITH [Animation(Duration > 0)] - uses PropertyAnimation
partial void ApplyUpdates(double deltaTime)
{
    if (_colorAnimation.IsAnimating)
    {
        _color = _colorAnimation.Update(deltaTime);
        NotifyPropertyChanged(nameof(Color));

        if (!_colorAnimation.IsAnimating)
            OnPropertyAnimationEnded(nameof(Color));
    }
}
```

### **Interpolation Strategies**

Different types use different interpolation strategies:

| Type                               | Can Animate? | Default Strategy     | Notes                              |
| ---------------------------------- | ------------ | -------------------- | ---------------------------------- |
| `float`, `double`                  | ✅ Yes       | Linear (Lerp)        | `a + (b - a) * t`                  |
| `int`, `byte`, `long`, `short`     | ✅ Yes       | Linear with rounding | `(int)(a + (b - a) * t)`           |
| `Vector2D`, `Vector3D`, `Vector4D` | ✅ Yes       | Component-wise Lerp  | Per-component interpolation        |
| `Quaternion`                       | ✅ Yes       | Slerp                | Spherical interpolation (rotation) |
| `Matrix3x3`, `Matrix4x4`           | ✅ Yes       | Component-wise Lerp  | Per-element interpolation          |
| `Rectangle<T>`                     | ✅ Yes       | Component-wise       | Interpolate X, Y, Width, Height    |
| `string`                           | ❌ No        | N/A                  | Compile error with `[Animation]`   |
| `bool`                             | ❌ No        | N/A                  | Compile error with `[Animation]`   |
| Reference types (classes)          | ❌ No        | N/A                  | Compile error with `[Animation]`   |
| Collections (`List<T>`, etc.)      | ❌ No        | N/A                  | Compile error with `[Animation]`   |

**Without `[Animation]` attribute:**
All types (including strings, bools, references) update instantly between frames (Duration = 0).

### **Custom Interpolation**

For custom types, implement `IInterpolatable<T>`:

```csharp
public struct CustomTransform : IInterpolatable<CustomTransform>
{
    public Vector3D<float> Position { get; set; }
    public Quaternion Rotation { get; set; }
    public float Scale { get; set; }

    public CustomTransform Interpolate(CustomTransform other, float t, InterpolationMode mode)
    {
        return new CustomTransform
        {
            Position = Vector3D.Lerp(Position, other.Position, t),
            Rotation = Quaternion.Slerp(Rotation, other.Rotation, t),
            Scale = Scale + (other.Scale - Scale) * t
        };
    }
}
```

### **Complex Property Update Scenarios**

For cases where property changes require additional logic (like adding items to collections), override the virtual animation event methods:

```csharp
public partial class ItemList : RuntimeComponent
{
    public List<string> Items { get; set; } = new();

    protected override void OnPropertyAnimationEnded(string propertyName)
    {
        base.OnPropertyAnimationEnded(propertyName);

        if (propertyName == nameof(Items))
        {
            // Custom logic when Items collection is replaced
            RefreshUI();
            ValidateItems();
        }
    }
}
```

Or use the public events from external code:

```csharp
var component = new TextElement();
component.AnimationStarted += (sender, e) =>
{
    Logger.LogDebug("Animation started for property: {PropertyName}", e.PropertyName);
};

component.AnimationEnded += (sender, e) =>
{
    Logger.LogDebug("Animation ended for property: {PropertyName}", e.PropertyName);

    if (e.PropertyName == "FontSize")
    {
        // Do something when font size finishes animating
    }
};
```

## Future Extensions

### **Validation Hooks**

Generator could support optional validation attributes:

```csharp
[Animation(Duration = 0.2f)]
[Range(0, 100)]
[ClampValue]
public float Volume { get; set; } = 50f;

// Generated setter includes validation
set
{
    value = Math.Clamp(value, 0, 100);  // Applied before storing in _targetVolume
    // ... rest of setter
}
```

### **Keyframe Support**

Support multi-keyframe animations:

```csharp
[Keyframes(new[] { 0f, 10f, 20f, 10f, 0f }, new[] { 0f, 0.25f, 0.5f, 0.75f, 1.0f })]
public float BounceHeight { get; set; }
```

## Migration Path

### **Phase 1: Infrastructure Refactoring**

1. Create `ComponentBase` class with all infrastructure properties
2. Move infrastructure from `RuntimeComponent` to `ComponentBase`
3. Make `RuntimeComponent : ComponentBase, IRuntimeComponent`
4. Verify no breaking changes (all infrastructure still available)

### **Phase 2: Generator Implementation**

1. Create `GameEngine.SourceGenerators` project
2. Implement `DeferredPropertyGenerator`
3. Create `GameEngine.Analyzers` project with NX1001 and NX1002
4. Reference generator from `GameEngine.csproj`

### **Phase 3: Gradual Component Migration**

1. Convert one component (e.g., `TextElement`) as proof-of-concept
2. Verify build succeeds and runtime behavior matches
3. Gradually convert other components
4. Remove old manual property implementations

### **Phase 4: Documentation and Training**

1. Document the pattern in developer guidelines
2. Add examples to README
3. Update component development templates

## Project Structure

```
Nexus.GameEngine/
├── GameEngine/
│   ├── GameEngine.csproj (references generator)
│   ├── Components/
│   │   ├── ComponentBase.cs (NEW - infrastructure properties)
│   │   ├── IRuntimeComponent.cs
│   │   └── RuntimeComponent.cs (implements IRuntimeComponent)
│   └── Animation/
│       ├── PropertyAnimation.cs (animation state manager)
│       ├── EasingMode.cs (easing function enum)
│       ├── InterpolationMode.cs (interpolation strategy enum)
│       ├── IInterpolatable.cs (custom interpolation interface)
│       ├── Interpolators/
│       │   ├── FloatInterpolator.cs
│       │   ├── VectorInterpolator.cs
│       │   ├── QuaternionInterpolator.cs
│       │   └── ... (type-specific interpolators)
│       └── Attributes/
│           ├── AnimationDurationAttribute.cs
│           ├── EasingAttribute.cs
│           ├── InterpolationModeAttribute.cs
│           ├── NoAnimationAttribute.cs
│           └── AnimationCurveAttribute.cs
│
├── GameEngine.SourceGenerators/
│   ├── GameEngine.SourceGenerators.csproj
│   ├── AnimatedPropertyGenerator.cs (main generator)
│   ├── PropertyAnalyzer.cs (finds properties to generate)
│   ├── InterpolationStrategy.cs (determines interpolation per type)
│   └── CodeBuilder.cs (builds generated code)
│
└── GameEngine.Analyzers/
    ├── GameEngine.Analyzers.csproj
    ├── ComponentPropertyAnalyzer.cs (NX1001)
    ├── AnimationAttributeAnalyzer.cs (NX1002, NX1003)
    └── CodeFixes/
        └── AnimationAttributeCodeFixProvider.cs
```

## Benefits

### **Developer Experience**

- ✅ Write properties naturally: `public float FontSize { get; set; }` (no `partial` needed)
- ✅ Automatic animation support for all properties
- ✅ Simple attribute-based configuration for custom behavior
- ✅ No manual boilerplate
- ✅ Clear separation: infrastructure vs. runtime properties
- ✅ Compile-time errors if pattern violated

### **Performance**

- ✅ Zero runtime overhead - all code generated at compile time
- ✅ No reflection, no dynamic dispatch
- ✅ Inlined by JIT like hand-written code
- ✅ Memory efficient - same as manual implementation

### **Maintainability**

- ✅ Consistent behavior across all components
- ✅ Single source of truth for deferred update logic
- ✅ Easy to extend (animation, validation, etc.)
- ✅ Compile-time safety via analyzers

### **Architecture**

- ✅ Clean separation of concerns via class hierarchy
- ✅ No attributes needed for exclusion
- ✅ No special-case type filtering
- ✅ Natural C# idioms (partial classes/properties)

## Edge Cases and Considerations

### **Collection Properties**

```csharp
public List<string> Items { get; set; } = new();

// Note: Setter replaces entire collection (instant update, no animation)
items.Items = new List<string> { "a", "b" };  // ✅ Instant replacement
items.Items.Add("c");                          // ⚠️ Direct modification (no property setter called)
```

**Solution**: Reference types don't animate by default. Use immutable collections or provide explicit methods.

### **Property Initialization**

```csharp
// Initializers set the initial value in generated backing fields
public float FontSize { get; set; } = 12f;  // _fontSize and _targetFontSize both = 12f
```

### **Understanding "Instant" Updates**

**Important**: Even properties without `[Animation]` attribute (Duration = 0) are **deferred to the next frame**:

```csharp
// No animation attribute = instant update between frames (NOT immediate)
public int CurrentLevel { get; set; }

// Usage:
component.CurrentLevel = 5;
Console.WriteLine(component.CurrentLevel);  // Still returns old value!

// Value updates during next ApplyUpdates() call
await NextFrame();
Console.WriteLine(component.CurrentLevel);  // Now returns 5
```

**There is no way to update immediately** - all property updates are deferred to maintain temporal consistency. This is intentional design.

### **Template Configuration**

```csharp
protected override void OnConfigure(IComponentTemplate template)
{
    if (template is Template t)
    {
        // Property assignments are deferred
        FontSize = t.FontSize;
        Color = t.Color;

        // Values applied when Configure() calls ApplyUpdates(0) after OnConfigure()
    }
}
```

After `OnConfigure()` returns, `Configure()` calls `ApplyUpdates(0)` to apply all initial property values before the component is activated.

### **Inheritance Hierarchies**

```csharp
public class UIElement : RuntimeComponent
{
    public float Width { get; set; }  // ✅ Generated
    public float Height { get; set; } // ✅ Generated
}

public class Button : UIElement
{
    public string Label { get; set; }  // ✅ Generated
    // Width and Height are inherited, not regenerated
}
```

### **Animation Chaining**

Setting a property multiple times queues new animations:

```csharp
// Frame 1: Start animating from 12 to 24
textElement.FontSize = 24f;

// Frame 5: Interrupt animation, now animate from current value (~15) to 36
textElement.FontSize = 36f;

// Animation restarts from current interpolated value
```

## Runtime Integration

### **Component Update Integration**

The generated `ApplyUpdates()` method is called during the component lifecycle:

```csharp
public abstract class RuntimeComponent : ComponentBase, IRuntimeComponent
{
    // Called during configuration phase (not overridable)
    public sealed void Configure(IComponentTemplate template)
    {
        OnConfigure(template);

        // Apply any pending property changes after configuration
        ApplyUpdates(0);
    }

    // Override this in derived classes for custom configuration
    protected virtual void OnConfigure(IComponentTemplate template)
    {
        // Configure component from template
    }

    // Called every frame during update phase (not overridable)
    protected sealed override void OnUpdate(double deltaTime)
    {
        // Apply property animations/updates before update logic
        ApplyUpdates(deltaTime);

        // Allow derived classes to handle updates
        OnComponentUpdate(deltaTime);

        // Continue with standard update logic
        base.OnUpdate(deltaTime);
    }

    // Override this in derived classes for custom update logic
    protected virtual void OnComponentUpdate(double deltaTime)
    {
        // Custom update logic
    }

    // Generated by source generator in derived classes
    partial void ApplyUpdates(double deltaTime);

    // Animation lifecycle events
    public event EventHandler<PropertyAnimationEventArgs>? AnimationStarted;
    public event EventHandler<PropertyAnimationEventArgs>? AnimationEnded;

    protected virtual void OnPropertyAnimationStarted(string propertyName)
    {
        AnimationStarted?.Invoke(this, new PropertyAnimationEventArgs(propertyName));
    }

    protected virtual void OnPropertyAnimationEnded(string propertyName)
    {
        AnimationEnded?.Invoke(this, new PropertyAnimationEventArgs(propertyName));
    }
}

public class PropertyAnimationEventArgs : EventArgs
{
    public string PropertyName { get; }

    public PropertyAnimationEventArgs(string propertyName)
    {
        PropertyName = propertyName;
    }
}
```

**Key Points:**

- `Configure()` is sealed and calls `ApplyUpdates(0)` after `OnConfigure()` to apply initial property values
- `OnUpdate()` is sealed and calls `ApplyUpdates(deltaTime)` before `OnComponentUpdate()`
- Derived classes override `OnConfigure()` and `OnComponentUpdate()` instead
- Animation events are raised at start and end of property animations
- Virtual method `OnPropertyAnimationStarted/Ended()` can be overridden for complex scenarios

### **Performance Considerations**

- **Animation checking**: Only updates properties with `IsAnimating = true`
- **No allocations**: All animation state stored in struct fields
- **Inlined by JIT**: Simple interpolation math gets optimized
- **Early exit**: Properties that aren't animating have zero cost

### **Default Animation Durations**

By default, properties update instantly (duration = 0). To enable animation globally:

```csharp
// Option 1: Per-property attribute
[AnimationDuration(0.2f)]
public float FontSize { get; set; }

// Option 2: Component-level default (future feature)
[DefaultAnimationDuration(0.2f)]
public class TextElement : RuntimeComponent
{
    public float FontSize { get; set; }  // Inherits 0.2s duration

    [AnimationDuration(0.5f)]
    public Vector4D<float> Color { get; set; }  // Override to 0.5s
}
```

## Design Decisions Summary

### **1. Class Hierarchy & Generation Scope**

**Decision**: Generate for **all classes that implement `IRuntimeComponent`**, including through inheritance.

- ✅ `RuntimeComponent : ComponentBase, IRuntimeComponent` → Generate
- ✅ `TextElement : RuntimeComponent` → Generate (inherits interface)
- ✅ `Button : TextElement` → Generate (inherits interface)
- ❌ `ComponentBase` → Don't generate (no interface)

**Rationale**: Derived component classes need the same property behavior.

### **2. Partial Keyword Requirement**

**Decision**: **Class must be `partial`**, properties are normal auto-properties.

- ✅ Required: `public partial class TextElement : RuntimeComponent`
- ❌ Not required: Properties don't need `partial` keyword
- Analyzer **NX1001** enforces this

**Rationale**: C# requires the class to be partial for generated code to extend it. Properties serve as metadata declarations.

### **3. Single Animation Attribute**

**Decision**: **Single `[Animation]` attribute** with Duration and Interpolation properties.

```csharp
[Animation(Duration = 0.5f, Interpolation = InterpolationMode.CubicEaseInOut)]
```

**Rationale**: Simpler and cleaner than multiple attributes. No `[NoAnimation]` needed - absence = instant.

### **4. InterpolationMode (Combined Easing + Interpolation)**

**Decision**: **Combine into single enum** with variants like `LinearEaseIn`, `CubicEaseOut`, etc.

**Rationale**: Interpolation strategy and easing work together - simpler as one concept.

### **5. Animation Type Restrictions**

**Decision**: **Only numeric/vector/matrix types** can use `[Animation]`. Compile error otherwise.

- ✅ Can animate: `float`, `int`, `Vector4D`, `Quaternion`, `Matrix4x4`
- ❌ Cannot animate: `string`, `bool`, `object`, `List<T>`
- Analyzer **NX1002** enforces this

**Rationale**: Only mathematical types can be interpolated.

### **6. Default Behavior (Opt-In Animation)**

**Decision**: **No animation by default**. Must add `[Animation]` to enable.

- No attribute = Duration 0 = Instant update
- `[Animation(Duration = 0.3f)]` = Animated

**Rationale**: Predictable, explicit behavior. Developers control which properties animate.

## Open Questions

1. **How to handle complex equality comparisons?**

   - Use `EqualityComparer<T>.Default` for most types
   - Could support custom `[EqualityComparer]` attribute later
   - **Recommendation**: Start with default equality, add custom later if needed

2. **Should we support animation callbacks?**

   - Generate events like `FontSizeAnimationStarted`, `FontSizeAnimationCompleted`
   - **Recommendation**: Add in Phase 2 if requested

3. **Thread safety considerations?**
   - Current design assumes single-threaded game loop
   - Could add `[ThreadSafe]` attribute for concurrent scenarios
   - **Recommendation**: Single-threaded for now, components are not thread-safe by design

## Complete Example

### **Before (Manual Implementation)**

```csharp
public class TextElement : RuntimeComponent, IDrawable
{
    private string? _text;
    private Vector4D<float> _color = Colors.White;
    private float _fontSize = 12f;

    public string? Text
    {
        get => _text;
        private set
        {
            if (_text != value)
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }
    }

    public Vector4D<float> Color
    {
        get => _color;
        private set
        {
            if (_color != value)
            {
                _color = value;
                NotifyPropertyChanged();
            }
        }
    }

    public float FontSize
    {
        get => _fontSize;
        private set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                NotifyPropertyChanged();
            }
        }
    }

    public void SetText(string? text) => QueueUpdate(() => Text = text);
    public void SetColor(Vector4D<float> color) => QueueUpdate(() => Color = color);
    public void SetFontSize(float size) => QueueUpdate(() => FontSize = size);
}
```

### **After (Generated Implementation)**

```csharp
public partial class TextElement : RuntimeComponent, IDrawable
{
    // Just declare properties - everything else is generated!
    // No [Animation] attribute = instant update (Duration = 0)
    public string? Text { get; set; }

    // Animated with cubic easing
    [Animation(Duration = 0.3f, Interpolation = InterpolationMode.CubicEaseInOut)]
    public Vector4D<float> Color { get; set; } = Colors.White;

    // Animated with linear interpolation
    [Animation(Duration = 0.2f)]
    public float FontSize { get; set; } = 12f;
}

// Usage:
var textElement = new TextElement();

// Animate color over 0.3 seconds with smooth easing
textElement.Color = Colors.Red;

// Animate font size over 0.2 seconds
textElement.FontSize = 24f;

// Instant text update (no [Animation] attribute)
textElement.Text = "Hello, World!";
```

### **Generated Code (TextElement.g.cs)**

```csharp
// <auto-generated/>
#nullable enable

partial class TextElement
{
    // Text property (instant update)
    private string? _text;
    private string? _targetText;

    public string? Text
    {
        get => _text;
        set
        {
            if (_targetText == value) return;
            _targetText = value;
            _text = value;  // Instant for reference types
            NotifyPropertyChanged(nameof(Text));
        }
    }

    // Color property (animated)
    private Vector4D<float> _color = Colors.White;
    private Vector4D<float> _targetColor = Colors.White;
    private PropertyAnimation<Vector4D<float>> _colorAnimation = new()
    {
        Duration = 0.3f,
        Interpolation = InterpolationMode.CubicEaseInOut
    };

    public Vector4D<float> Color
    {
        get => _color;
        set
        {
            if (_targetColor == value) return;
            _targetColor = value;
            _colorAnimation.StartAnimation(_color, _targetColor, TimeProvider.Current);
            NotifyPropertyChanged(nameof(Color));
        }
    }

    // FontSize property (animated)
    private float _fontSize = 12f;
    private float _targetFontSize = 12f;
    private PropertyAnimation<float> _fontSizeAnimation = new()
    {
        Duration = 0.2f,
        Interpolation = InterpolationMode.Linear
    };

    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (EqualityComparer<float>.Default.Equals(_targetFontSize, value)) return;
            _targetFontSize = value;
            _fontSizeAnimation.StartAnimation(_fontSize, _targetFontSize, TimeProvider.Current);
            NotifyPropertyChanged(nameof(FontSize));
        }
    }

    // Animation update method
    partial void ApplyUpdates(double deltaTime)
    {
        if (_colorAnimation.IsAnimating)
        {
            _color = _colorAnimation.Update(deltaTime);
            NotifyPropertyChanged(nameof(Color));
        }

        if (_fontSizeAnimation.IsAnimating)
        {
            _fontSize = _fontSizeAnimation.Update(deltaTime);
            NotifyPropertyChanged(nameof(FontSize));
        }
    }
}
```

## References

- [C# Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Incremental Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
- [Roslyn Analyzers](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [PropertyChanged Pattern](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged)
- [Easing Functions](https://easings.net/)
- [Interpolation Methods](https://en.wikipedia.org/wiki/Interpolation)
