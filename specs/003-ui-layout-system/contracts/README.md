# API Contracts: UI Layout System

**Feature**: `003-ui-layout-system`  
**Date**: 2025-11-04

## Scope

This feature implements internal game engine UI layout functionality. There are **no external API contracts** (no REST/GraphQL endpoints, no network protocols) since this is a client-side rendering system.

## Internal Contracts (Component APIs)

The "contracts" for this feature are the public interfaces and methods of the layout components. These are defined in the data model and enforced through C# type system.

### Core Interface: IRuntimeComponent

All layout components implement the existing `IRuntimeComponent` interface:

```csharp
public interface IRuntimeComponent
{
    void OnLoad();
    void OnActivate();
    void OnUpdate(double deltaTime);
    void OnDeactivate();
    IComponent? Parent { get; set; }
    List<IComponent> Children { get; }
    // ... (other lifecycle methods)
}
```

### Layout Contract: Size Constraint Propagation

Layout components must implement this pattern:

```csharp
// Parent → Child constraint propagation
public virtual void SetSizeConstraints(Rectangle<int> constraints)
{
    _sizeConstraints = constraints;
    OnSizeConstraintsChanged();
}

// Child responds to constraint changes
protected virtual void OnSizeConstraintsChanged()
{
    // Recalculate size based on constraints and SizeMode
    // Propagate to children if this is a layout container
}
```

### Layout Container Contract

All layout containers (VerticalLayout, HorizontalLayout, GridLayout) must implement:

```csharp
public abstract class Layout : Element
{
    // Trigger layout recalculation
    protected void InvalidateLayout();
    
    // Implement layout algorithm
    protected abstract void RecalculateLayout();
    
    // Lifecycle integration
    public override void OnUpdate(double deltaTime)
    {
        if (_needsLayout)
        {
            RecalculateLayout();
            _needsLayout = false;
        }
        base.OnUpdate(deltaTime);
    }
}
```

## Template Contracts (Configuration API)

Templates are the user-facing "API" for configuring layouts. These are auto-generated from `[TemplateProperty]` attributes.

### Element Template Contract

```csharp
public record ElementTemplate : Template
{
    public Vector2D<float>? Position { get; init; }
    public Vector2D<float>? Size { get; init; }
    public Vector2D<float>? AnchorPoint { get; init; }
    public SizeMode? SizeMode { get; init; }
    public float? WidthPercentage { get; init; }
    public float? HeightPercentage { get; init; }
    public Vector2D<int>? MinSize { get; init; }
    public Vector2D<int>? MaxSize { get; init; }
    // ... (other properties)
}
```

### Layout Template Contracts

```csharp
public record VerticalLayoutTemplate : LayoutTemplate
{
    public HorizontalAlignment? HorizontalAlignment { get; init; }
    // Inherits: Padding, Spacing from LayoutTemplate
}

public record HorizontalLayoutTemplate : LayoutTemplate
{
    public VerticalAlignment? VerticalAlignment { get; init; }
}

public record GridLayoutTemplate : LayoutTemplate
{
    public int? ColumnCount { get; init; }
    public bool? MaintainCellAspectRatio { get; init; }
    public float? CellAspectRatio { get; init; }
}
```

## Testing Contracts

Integration tests verify layout behavior through pixel sampling:

```csharp
public interface IPixelSampler
{
    Vector4D<float> SamplePixel(int x, int y);
}
```

Tests use this contract to verify visual output matches expected layout calculations.

## Version Compatibility

- **Breaking changes**: Changes to template properties or layout algorithms are breaking
- **Non-breaking changes**: New layout types, new properties with defaults
- **Versioning strategy**: SemVer for GameEngine library

## Summary

This feature has no traditional API contracts (no network/RPC interfaces). The "contracts" are:

1. **Component interfaces** (IRuntimeComponent, lifecycle methods)
2. **Template records** (auto-generated configuration API)
3. **Layout propagation pattern** (SetSizeConstraints → OnSizeConstraintsChanged)

All contracts are enforced through C# type system at compile-time.
