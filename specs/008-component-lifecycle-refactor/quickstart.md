# Quickstart: Component Lifecycle Refactor

## Overview

This refactor changes how components are initialized to ensure a predictable state.

## Key Changes

1.  **`Load(Template)` is sealed/non-virtual**: You can no longer override `Load`. Use `OnLoad` for custom logic.
2.  **`Configure(Template)`**: A new virtual method used by Source Generators to apply properties. Do not implement this manually unless you are bypassing the generator.
3.  **`OnLoad` Timing**: `OnLoad` now runs *after* all properties (including derived ones) are set.
4.  **`ContentManager` Location**: Moved to `Nexus.GameEngine.Runtime`. Update your `using` statements.
5.  **`UpdateLayout`**: `Element`s now have an `UpdateLayout` method called automatically by `ContentManager`.

## Migration Guide

### If you overrode `Load(Template)`:

**Before:**
```csharp
public override void Load(Template t) {
    base.Load(t);
    // custom logic
}
```

**After:**
```csharp
protected override void OnLoad(Template t) {
    base.OnLoad(t); // Optional, base.OnLoad is usually empty
    // custom logic
}
```

### If you manually implemented property application:

**Before:**
```csharp
protected override void OnLoad(Template t) {
    if (t is MyTemplate mt) {
        this.Prop = mt.Prop;
    }
    base.OnLoad(t);
}
```

**After:**
```csharp
protected override void Configure(Template t) {
    base.Configure(t); // CRITICAL: Call base first
    if (t is MyTemplate mt) {
        this.Prop = mt.Prop;
    }
}
```
*(Note: Prefer using `[ComponentProperty]` to let the generator handle this)*
