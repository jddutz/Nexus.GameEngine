# Property Binding: Manual Code to Template Syntax

## The Core Mechanism

You've identified it perfectly:

```csharp
sourceComponent.PropertyChanged += (value) => targetComponent.SetHealth(typeConverter(value));
```

Let me show what this looks like in real code, then distill to template syntax.

## Step 1: Manual Implementation in OnActivate()

### Example 1: Simple Parent-to-Child Binding

```csharp
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    protected float _health;
    
    [ComponentProperty]
    protected float _maxHealth;
    
    private HealthSystem? _healthSystem;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Find the source component
        _healthSystem = this.FindParent<HealthSystem>();
        
        if (_healthSystem != null)
        {
            // Subscribe to source property changes
            _healthSystem.PropertyChanged += OnHealthSystemPropertyChanged;
            
            // Initial sync
            SetHealth(_healthSystem.Health);
            SetMaxHealth(_healthSystem.MaxHealth);
        }
    }
    
    private void OnHealthSystemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_healthSystem == null) return;
        
        // When Health changes on parent, update our Health
        if (e.PropertyName == nameof(HealthSystem.Health))
        {
            SetHealth(_healthSystem.Health);
            //       ^^^^^^^^^^^^^^^^^^^^^^^^ Gets value from source
            //       ^^^^^^^^ Calls generated Set method (uses ComponentPropertyUpdater)
        }
        
        // When MaxHealth changes on parent, update our MaxHealth
        if (e.PropertyName == nameof(HealthSystem.MaxHealth))
        {
            SetMaxHealth(_healthSystem.MaxHealth);
        }
    }
    
    protected override void OnDeactivate()
    {
        // Cleanup - unsubscribe from events
        if (_healthSystem != null)
        {
            _healthSystem.PropertyChanged -= OnHealthSystemPropertyChanged;
            _healthSystem = null;
        }
        
        base.OnDeactivate();
    }
}
```

### Example 2: With Type Conversion

```csharp
public partial class HealthDisplay : RuntimeComponent
{
    [ComponentProperty]
    protected string _text = string.Empty;
    
    private HealthSystem? _healthSystem;
    private FloatToStringConverter _converter = new() { Format = "0.0" };
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        _healthSystem = this.FindParent<HealthSystem>();
        
        if (_healthSystem != null)
        {
            _healthSystem.PropertyChanged += OnHealthSystemPropertyChanged;
            
            // Initial sync with conversion
            var convertedValue = _converter.Convert(_healthSystem.Health);
            SetText(convertedValue);
        }
    }
    
    private void OnHealthSystemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_healthSystem == null) return;
        
        if (e.PropertyName == nameof(HealthSystem.Health))
        {
            // Convert float → string
            var convertedValue = _converter.Convert(_healthSystem.Health);
            SetText(convertedValue);
        }
    }
    
    protected override void OnDeactivate()
    {
        if (_healthSystem != null)
        {
            _healthSystem.PropertyChanged -= OnHealthSystemPropertyChanged;
            _healthSystem = null;
        }
        
        base.OnDeactivate();
    }
}
```

### Example 3: Two-Way Binding

```csharp
public partial class VolumeSlider : RuntimeComponent
{
    [ComponentProperty]
    protected float _value;
    
    private AudioSettings? _audioSettings;
    private bool _isUpdatingFromBinding; // Prevent circular updates
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        _audioSettings = this.FindParent<AudioSettings>();
        
        if (_audioSettings != null)
        {
            // Forward: AudioSettings.Volume → VolumeSlider.Value
            _audioSettings.PropertyChanged += OnAudioSettingsPropertyChanged;
            
            // Initial sync
            _isUpdatingFromBinding = true;
            SetValue(_audioSettings.Volume);
            _isUpdatingFromBinding = false;
        }
    }
    
    private void OnAudioSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_audioSettings == null || _isUpdatingFromBinding) return;
        
        if (e.PropertyName == nameof(AudioSettings.Volume))
        {
            _isUpdatingFromBinding = true;
            SetValue(_audioSettings.Volume);
            _isUpdatingFromBinding = false;
        }
    }
    
    // This would be called when user drags slider
    private void OnValueChanged(float oldValue)
    {
        // Backward: VolumeSlider.Value → AudioSettings.Volume
        if (_audioSettings != null && !_isUpdatingFromBinding)
        {
            _isUpdatingFromBinding = true;
            _audioSettings.SetVolume(Value);
            _isUpdatingFromBinding = false;
        }
    }
    
    protected override void OnDeactivate()
    {
        if (_audioSettings != null)
        {
            _audioSettings.PropertyChanged -= OnAudioSettingsPropertyChanged;
            _audioSettings = null;
        }
        
        base.OnDeactivate();
    }
}
```

### Example 4: Context Binding

```csharp
public partial class ThemedButton : RuntimeComponent
{
    [ComponentProperty]
    protected Color _backgroundColor;
    
    private ThemeContext? _themeContext;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Search up tree for context provider
        _themeContext = FindContextProvider<ThemeContext>();
        
        if (_themeContext != null)
        {
            _themeContext.PropertyChanged += OnThemeContextPropertyChanged;
            SetBackgroundColor(_themeContext.PrimaryColor);
        }
    }
    
    private void OnThemeContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_themeContext == null) return;
        
        if (e.PropertyName == nameof(ThemeContext.PrimaryColor))
        {
            SetBackgroundColor(_themeContext.PrimaryColor);
        }
    }
    
    private T? FindContextProvider<T>() where T : class, IContextProvider
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is T context)
                return context;
            parent = parent.Parent;
        }
        return null;
    }
    
    protected override void OnDeactivate()
    {
        if (_themeContext != null)
        {
            _themeContext.PropertyChanged -= OnThemeContextPropertyChanged;
            _themeContext = null;
        }
        
        base.OnDeactivate();
    }
}
```

## Step 2: Pattern Analysis

Looking at the manual implementations, the pattern is:

### Common Elements
1. **Source component reference** (stored as field)
2. **Event subscription** (in OnActivate)
3. **Event handler** (checks property name, calls Set method)
4. **Initial sync** (set initial value on activation)
5. **Cleanup** (unsubscribe in OnDeactivate)

### Variable Elements
- **Lookup strategy** (FindParent, FindContext, FindSibling)
- **Source property name** (what to watch)
- **Target property name** (what to update)
- **Converter** (optional type conversion)
- **Mode** (OneWay, TwoWay)
- **Re-entry prevention** (for TwoWay)

## Step 3: Distill to Template Syntax

### What We Need to Capture in Template

```
[Binding Configuration]
├── Target Property (on this component)
├── Source Component Lookup
│   ├── Strategy (Parent, Context, Sibling, Path)
│   └── Type or Name
├── Source Property (on source component)
├── Optional: Converter
├── Optional: Validator
├── Optional: Mode (OneWay, TwoWay)
└── Optional: Behavior (Fallback, Throttle, etc.)
```

### Minimal Required Information

For the simple case:
1. **Target property** - Which property on this component to update
2. **Source lookup** - How to find source component
3. **Source property** - Which property on source component

### Proposed Template Syntax (Refined)

#### Option A: Tuple Syntax (Absolute Minimum)
```csharp
new HealthBarTemplate()
{
    Bindings = 
    [
        // (Target, SourceType, SourceProperty)
        (nameof(Health), typeof(HealthSystem), nameof(HealthSystem.Health)),
        (nameof(MaxHealth), typeof(HealthSystem), nameof(HealthSystem.MaxHealth))
    ]
}
```

**Generated code does**:
```csharp
protected override void OnActivate()
{
    base.OnActivate();
    
    // For each binding...
    var source_0 = this.FindParent<HealthSystem>();
    if (source_0 != null)
    {
        source_0.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HealthSystem.Health))
                SetHealth(source_0.Health);
        };
        SetHealth(source_0.Health); // Initial sync
    }
    
    // Repeat for each binding...
}
```

#### Option B: Builder Syntax (Full Control)
```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Health))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .Build(),
            
        PropertyBinding.For(nameof(BackgroundColor))
            .FromContext<ThemeContext>()
            .Property(nameof(ThemeContext.PrimaryColor))
            .WithConverter(new ColorTintConverter { Brightness = 0.8f })
            .WithFallback(Colors.Gray)
            .Build()
    ]
}
```

**Generated code does**:
```csharp
protected override void OnActivate()
{
    base.OnActivate();
    
    // Binding 0: Simple parent binding
    var source_0 = this.FindParent<HealthSystem>();
    if (source_0 != null)
    {
        source_0.PropertyChanged += OnBinding0SourceChanged;
        SetHealth(source_0.Health);
    }
    
    // Binding 1: Context binding with converter and fallback
    var source_1 = FindContextProvider<ThemeContext>();
    if (source_1 != null)
    {
        source_1.PropertyChanged += OnBinding1SourceChanged;
        var value = _binding1Converter.Convert(source_1.PrimaryColor);
        SetBackgroundColor(value ?? _binding1Fallback);
    }
    else
    {
        SetBackgroundColor(_binding1Fallback); // Use fallback if no context
    }
}

private void OnBinding0SourceChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == "Health")
        SetHealth(((HealthSystem)sender).Health);
}

private void OnBinding1SourceChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == "PrimaryColor")
    {
        var value = _binding1Converter.Convert(((ThemeContext)sender).PrimaryColor);
        SetBackgroundColor(value ?? _binding1Fallback);
    }
}
```

## Step 4: Generated Code Structure

### Base Template Record
```csharp
public record Template
{
    public string? Name { get; set; }
    public Template[] Subcomponents { get; init; } = [];
    
    // Simple tuple-based bindings
    public (string target, Type sourceType, string source)[] Bindings { get; init; } = [];
    
    // Complex builder-based bindings
    public PropertyBinding[] PropertyBindings { get; init; } = [];
}
```

### Source Generator Addition

The **ComponentPropertyGenerator** would be extended to:

1. **Detect bindings in template** (during OnLoad)
2. **Generate binding fields** (to store source references)
3. **Generate OnActivate binding setup** (subscribe + initial sync)
4. **Generate event handlers** (property change handlers)
5. **Generate OnDeactivate cleanup** (unsubscribe)

### Generated Partial Class Structure

```csharp
// User-written code
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    protected float _health;
}

// Generated code (HealthBar.g.cs)
partial class HealthBar
{
    // Generated property implementation (existing)
    private ComponentPropertyUpdater<float> _healthState;
    public float Health => _health;
    public void SetHealth(float value, InterpolationFunction<float>? interpolator = null) { ... }
    
    // NEW: Generated binding fields (if bindings configured)
    private HealthSystem? _binding0Source;
    
    // NEW: Extend OnActivate to setup bindings
    partial void OnActivateBindings()
    {
        _binding0Source = this.FindParent<HealthSystem>();
        if (_binding0Source != null)
        {
            _binding0Source.PropertyChanged += OnBinding0PropertyChanged;
            SetHealth(_binding0Source.Health); // Initial sync
        }
    }
    
    // NEW: Generated event handler
    private void OnBinding0PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Health" && _binding0Source != null)
        {
            SetHealth(_binding0Source.Health);
        }
    }
    
    // NEW: Extend OnDeactivate to cleanup bindings
    partial void OnDeactivateBindings()
    {
        if (_binding0Source != null)
        {
            _binding0Source.PropertyChanged -= OnBinding0PropertyChanged;
            _binding0Source = null;
        }
    }
}
```

### RuntimeComponent Base Class Hooks

```csharp
public partial class RuntimeComponent
{
    protected virtual void OnActivate()
    {
        OnActivateBindings(); // Call generated method
    }
    
    protected virtual void OnDeactivate()
    {
        OnDeactivateBindings(); // Call generated method
    }
    
    // Partial methods implemented by generator if bindings exist
    partial void OnActivateBindings();
    partial void OnDeactivateBindings();
}
```

## Step 5: Comparison - Manual vs Generated

### Manual Code (What You Write Now)
```csharp
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    protected float _health;
    
    private HealthSystem? _healthSystem;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        _healthSystem = this.FindParent<HealthSystem>();
        if (_healthSystem != null)
        {
            _healthSystem.PropertyChanged += OnHealthSystemPropertyChanged;
            SetHealth(_healthSystem.Health);
        }
    }
    
    private void OnHealthSystemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_healthSystem != null && e.PropertyName == "Health")
            SetHealth(_healthSystem.Health);
    }
    
    protected override void OnDeactivate()
    {
        if (_healthSystem != null)
        {
            _healthSystem.PropertyChanged -= OnHealthSystemPropertyChanged;
            _healthSystem = null;
        }
        base.OnDeactivate();
    }
}
```
**Lines**: ~30 lines per binding

### Template-Driven (What You Write)
```csharp
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    protected float _health;
}

// In template:
new HealthBarTemplate()
{
    Bindings = 
    [
        (nameof(Health), typeof(HealthSystem), nameof(HealthSystem.Health))
    ]
}
```
**Lines**: ~1 line per binding (30x reduction!)

## Recommendation

**Support both syntaxes**:

1. **Tuple syntax** for simple parent bindings (90% of cases)
   ```csharp
   Bindings = [(nameof(Health), typeof(HealthSystem), nameof(HealthSystem.Health))]
   ```

2. **Builder syntax** for complex scenarios (converters, validation, etc.)
   ```csharp
   PropertyBindings = [
       PropertyBinding.For(nameof(Text))
           .FromParent<HealthSystem>()
           .Property(nameof(HealthSystem.Health))
           .WithConverter(new FloatToStringConverter())
           .Build()
   ]
   ```

This gives you maximum conciseness for common cases while maintaining full power for complex bindings!
