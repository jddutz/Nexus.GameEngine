# Property Binding Research

## Overview

This document examines how different platforms solve the problem of wiring up properties between components, with analysis relevant to Nexus.GameEngine's composition-first architecture.

## Current State Analysis

### Existing Systems in Nexus.GameEngine

1. **ComponentProperty System** - Deferred property updates with animation
   - Properties marked with `[ComponentProperty]` get generated backing fields
   - Supports interpolation between values (animated transitions)
   - Changes are deferred until next frame for temporal consistency
   - No built-in mechanism for linking properties between components

2. **IDataBinding Interface** - Comprehensive data binding infrastructure (stub implementation)
   - Traditional WPF-style binding model
   - String-based property paths
   - Supports OneWay, TwoWay, OneTime, OneWayToSource modes
   - Includes validation, conversion, error handling
   - Event-based notification system
   - Currently appears to be scaffolding without concrete implementation

3. **Event System** - Component lifecycle and state events
   - Components expose events (Activating, Activated, Updating, Updated, etc.)
   - No formal property change notification mechanism
   - Parents can subscribe to child events

### Current Gaps

- **No PropertyChanged notification** - ComponentProperty system doesn't notify when values change
- **No parent-child property linking** - No way to wire a child's property to react to parent changes
- **IDataBinding is incomplete** - Full WPF-style binding infrastructure defined but not implemented
- **Manual wiring required** - Developers must manually connect related properties

## Industry Solutions

### 1. WPF/XAML (Microsoft)

**Approach**: Declarative binding with INPC (INotifyPropertyChanged)

```csharp
// Model/ViewModel
public class ViewModel : INotifyPropertyChanged
{
    private string _name;
    public string Name 
    { 
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// XAML
<TextBlock Text="{Binding Name, Mode=TwoWay}" />
```

**Strengths**:
- Mature, well-understood pattern
- Strong tooling support
- Separation of data from presentation
- Expression-based binding for compile-time checking

**Weaknesses**:
- Requires boilerplate (INPC implementation)
- String-based paths are fragile
- Reflection overhead
- Doesn't align with composition-first approach
- Heavy dependency on XAML markup

### 2. Unity Engine

**Approach A**: Direct component references with manual polling

```csharp
public class HealthBar : MonoBehaviour
{
    public Player player;  // Set in editor
    private Slider slider;
    
    void Update()
    {
        slider.value = player.health / player.maxHealth;
    }
}
```

**Approach B**: Events and delegates

```csharp
public class Player : MonoBehaviour
{
    public event Action<float> OnHealthChanged;
    
    private float _health;
    public float Health
    {
        get => _health;
        set
        {
            _health = value;
            OnHealthChanged?.Invoke(_health);
        }
    }
}

public class HealthBar : MonoBehaviour
{
    void Start()
    {
        player.OnHealthChanged += UpdateBar;
    }
    
    void UpdateBar(float health) { /* ... */ }
}
```

**Approach C**: UnityEvents (inspector-configurable)

```csharp
public class Player : MonoBehaviour
{
    public UnityEvent<float> onHealthChanged;
    
    public float Health
    {
        set { onHealthChanged.Invoke(value); }
    }
}
```

**Strengths**:
- Simple and direct
- No magic or hidden behavior
- Performance-friendly (direct calls)
- Inspector integration for UnityEvents

**Weaknesses**:
- Manual wiring in every component
- Tight coupling with direct references
- No automatic cleanup (memory leaks with events)
- Polling in Update() is inefficient
- No built-in validation or conversion

### 3. Unreal Engine

**Approach A**: Property binding in Blueprints (visual scripting)

Visual node-based binding between components

**Approach B**: Delegates in C++

```cpp
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnHealthChanged, float, NewHealth);

UCLASS()
class APlayer : public AActor
{
    UPROPERTY(BlueprintAssignable)
    FOnHealthChanged OnHealthChanged;
    
    void SetHealth(float NewHealth)
    {
        Health = NewHealth;
        OnHealthChanged.Broadcast(NewHealth);
    }
};
```

**Strengths**:
- Blueprint bindings are visual and designer-friendly
- Type-safe delegates
- Editor integration

**Weaknesses**:
- Blueprint bindings can have performance overhead
- Delegates require manual management
- Complex for nested hierarchies

### 4. React (Web)

**Approach**: Props down, events up + state management

```jsx
function Parent() {
    const [value, setValue] = useState(0);
    
    return (
        <Child 
            value={value} 
            onChange={(newValue) => setValue(newValue)}
        />
    );
}

function Child({ value, onChange }) {
    return <input value={value} onChange={e => onChange(e.target.value)} />;
}
```

**With Context API for deep trees**:

```jsx
const ThemeContext = React.createContext();

function Parent() {
    return (
        <ThemeContext.Provider value={{ color: 'red' }}>
            <DeepChild />
        </ThemeContext.Provider>
    );
}

function DeepChild() {
    const theme = useContext(ThemeContext);
    return <div style={{ color: theme.color }}>...</div>;
}
```

**Strengths**:
- Unidirectional data flow is predictable
- Props are explicit and type-safe
- Context avoids prop drilling
- Composition-first philosophy

**Weaknesses**:
- Requires re-rendering subtrees
- Can lead to performance issues without optimization
- Context updates trigger all consumers

### 5. Vue.js

**Approach**: Reactive properties with automatic dependency tracking

```javascript
// Parent
const state = reactive({
    count: 0
});

// Child automatically reacts
watchEffect(() => {
    console.log(state.count);  // Re-runs when count changes
});

// Two-way binding
const model = defineModel();
```

**Strengths**:
- Automatic dependency tracking (no manual subscriptions)
- Two-way binding with v-model
- Minimal boilerplate

**Weaknesses**:
- Magic behavior can be hard to debug
- Reactivity system has learning curve
- Not type-safe without TypeScript

### 6. Godot Engine (GDScript)

**Approach**: Signals (similar to Qt)

```gdscript
# Player.gd
signal health_changed(new_health)

var health = 100:
    set(value):
        health = value
        health_changed.emit(health)

# HealthBar.gd
func _ready():
    player.health_changed.connect(_on_health_changed)

func _on_health_changed(new_health):
    update_bar(new_health)
```

**Strengths**:
- Clean signal/slot pattern
- Editor integration for connecting signals
- Type-safe with static typing

**Weaknesses**:
- Manual connection boilerplate
- No automatic cleanup (must disconnect)

### 7. Angular (Web)

**Approach**: Observables (RxJS) + Templates

```typescript
export class Parent {
    value$ = new BehaviorSubject(0);
}

// Template
<child [value]="value$ | async"></child>

// Child
export class Child {
    @Input() value: number;
}
```

**Strengths**:
- Powerful reactive streams
- Explicit data flow
- Strong type safety

**Weaknesses**:
- RxJS has steep learning curve
- Easy to create subscription leaks
- Heavy runtime

### 8. SwiftUI (Apple)

**Approach**: Property wrappers with automatic dependency tracking

```swift
class Model: ObservableObject {
    @Published var count = 0
}

struct ParentView: View {
    @StateObject var model = Model()
    
    var body: some View {
        ChildView(count: $model.count)  // $ creates binding
    }
}

struct ChildView: View {
    @Binding var count: Int
    
    var body: some View {
        Text("\\(count)")
    }
}
```

**Strengths**:
- Automatic dependency tracking and re-rendering
- Type-safe bindings with @Binding
- Minimal boilerplate
- Composition-friendly

**Weaknesses**:
- Requires understanding of property wrappers
- SwiftUI-specific, not portable

## Comparison Matrix

| Framework | Notification | Type Safety | Boilerplate | Composition | Performance | Auto Cleanup |
|-----------|-------------|-------------|-------------|-------------|-------------|--------------|
| WPF       | INPC        | Weak        | High        | Medium      | Medium      | No           |
| Unity     | Events      | Strong      | Medium      | Low         | High        | No           |
| Unreal    | Delegates   | Strong      | Medium      | Low         | High        | No           |
| React     | Props       | Strong      | Low         | High        | Medium      | Yes          |
| Vue       | Reactive    | Medium      | Low         | High        | Medium      | Yes          |
| Godot     | Signals     | Strong      | Medium      | Medium      | High        | No           |
| SwiftUI   | @Published  | Strong      | Low         | High        | High        | Yes          |

## Patterns Summary

### Pattern 1: Observer/Event Pattern
- **Used by**: Unity, Unreal, Godot, WPF (INPC)
- **Pros**: Simple, direct, performant
- **Cons**: Manual wiring, no automatic cleanup, boilerplate

### Pattern 2: Reactive Properties
- **Used by**: Vue, SwiftUI, Angular
- **Pros**: Automatic dependency tracking, minimal boilerplate
- **Cons**: Magic behavior, runtime overhead for tracking

### Pattern 3: Explicit Props/Binding
- **Used by**: React, SwiftUI (with @Binding)
- **Pros**: Explicit data flow, composition-friendly, predictable
- **Cons**: Requires parent to pass data down

### Pattern 4: Context/Provider
- **Used by**: React (Context), WPF (DataContext)
- **Pros**: Avoids prop drilling, good for global state
- **Cons**: Can hide dependencies, updates trigger all consumers

## Recommendations for Nexus.GameEngine

Based on the research and Nexus.GameEngine's composition-first architecture, consider a **hybrid approach**:

### Core Principles

1. **Composition over Configuration**
   - Favor explicit parent-to-child property flow
   - Components should declare dependencies clearly
   - Minimize magic/hidden behavior

2. **Source Generation over Reflection**
   - Leverage existing ComponentProperty generation system
   - Generate property change notifications at compile time
   - Type-safe bindings without runtime overhead

3. **Explicit over Implicit**
   - Make data flow visible in component tree
   - Avoid hidden global state
   - Clear ownership of data

### Proposed Approaches

#### Approach A: Property Synchronization via Template (REVISED)
**Problem with original approach**: Attributes on component classes bake in relationships at design time, violating composition-first principles.

**Solution**: Define bindings declaratively in templates at composition time:

```csharp
// Components are generic, no hardcoded relationships
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    public float Health { get; set; } = 100f;
}

public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    public float Health { get; set; }
}

// Relationships defined at composition time via template
new HealthSystem()
{
    Health = 100f,
    Subcomponents = 
    [
        new HealthBar()
        {
            // Binding defined in template, not component class
            PropertyBindings = 
            [
                new PropertyBinding("../Health", "Health")  // Parent's Health → My Health
            ]
        }
    ]
}
```

**How it works**:
- PropertyBindings array added to base Template record
- Paths are relative: "../PropertyName" = parent, "PropertyName" = this component
- Relationships established during component creation from template
- No code generation needed - runtime subscription during OnActivate()

#### Approach B: PropertyBinding Component (REVISED - Preferred)
**First-class binding component** that establishes relationships declaratively:

```csharp
// Components remain generic
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    public float Health { get; set; } = 100f;
}

public partial class HealthBar : RuntimeComponent  
{
    [ComponentProperty]
    public float Health { get; set; }
}

// Binding is a separate component in the tree
new HealthSystem()
{
    Subcomponents = 
    [
        new PropertyBinding()  // ✅ Binding IS A COMPONENT
        {
            SourcePath = "../Health",       // HealthSystem.Health
            TargetPath = "HealthBar.Health", // Sibling's property
            Mode = BindingMode.OneWay
        },
        new HealthBar()
    ]
}
```

**Why this is better**:
- ✅ Bindings visible in component tree (explicit composition)
- ✅ Can be added/removed dynamically
- ✅ No changes to component classes
- ✅ Supports complex scenarios (validation, conversion)
- ✅ Runtime configuration (no source generation needed)
- ✅ Follows existing component patterns

#### Approach C: Context Components (Global State - UNCHANGED)
Context components provide values to descendant tree:

```csharp
// Context is special-purpose, providing global/theme values
new GameContext()  // Provides game-wide state
{
    Theme = darkTheme,
    Subcomponents = 
    [
        new UI()
        {
            Subcomponents =
            [
                new Button()
                {
                    PropertyBindings = 
                    [
                        // Search up tree for ThemeContext
                        new ContextBinding<ThemeContext>("Theme", "Theme")
                    ]
                }
            ]
        }
    ]
}
```

**Note**: Context bindings still make sense because:
- Themes/settings are genuinely global
- Multiple unrelated components need same value
- Alternative would be excessive prop drilling

### REVISED Recommendation: Composition-First Binding

After reconsidering the **composition-first** constraint, the recommendation changes significantly:

**Primary Mechanism**: `PropertyBinding` component (declarative, template-based)

```csharp
// Add to base Template record
public record Template
{
    public PropertyBindingTemplate[] PropertyBindings { get; init; } = [];
    // ... existing properties
}

// Usage in templates
new HealthBar()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate 
        {
            SourcePath = "../Health",  // Relative path to parent
            TargetProperty = "Health",
            Mode = BindingMode.OneWay
        }
    ]
}
```

**Why this is superior for composition-first**:
- ✅ **Zero component class changes** - components remain generic and reusable
- ✅ **Relationships defined at composition time** - not hardcoded in component design
- ✅ **Fully declarative** - everything visible in template
- ✅ **Runtime flexibility** - bindings created during component instantiation
- ✅ **Matches existing patterns** - similar to how Subcomponents work
- ✅ **No code generation needed** - simple runtime reflection (acceptable cost)

**Secondary Mechanism**: Context bindings for truly global state (themes, settings)

**Tertiary**: Manual event subscriptions for custom logic that doesn't fit binding patterns

This aligns with how Unity/Unreal/Godot work - bindings established in scenes/prefabs, not in scripts.

## Next Steps

1. Design detailed specification for chosen approach(es)
2. Implement source generator extensions
3. Update ComponentProperty system to support notifications
4. Create examples and documentation
5. Consider performance implications (benchmark against manual approaches)
