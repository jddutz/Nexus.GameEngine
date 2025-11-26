# Data Model: RectTransform System

## Interfaces

### IRectTransform
Defines the contract for 2D spatial manipulation.

```csharp
public interface IRectTransform
{
    // Core Properties
    Vector2D<float> Position { get; }
    Vector2D<float> Size { get; }
    float Rotation { get; } // Radians
    Vector2D<float> Scale { get; }
    Vector2D<float> Pivot { get; } // 0-1 Normalized

    // Matrices
    Matrix4X4<float> LocalMatrix { get; }
    Matrix4X4<float> WorldMatrix { get; }

    // Methods
    void SetPosition(Vector2D<float> position);
    void SetSize(Vector2D<float> size);
    void SetRotation(float radians);
    void SetScale(Vector2D<float> scale);
    void SetPivot(Vector2D<float> pivot);
    
    // Bounds
    Rectangle<int> GetBounds();
}
```

## Components

### RectTransform
Base component for 2D objects.

```csharp
public partial class RectTransform : RuntimeComponent, IRectTransform
{
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _position;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _size;

    [ComponentProperty]
    [TemplateProperty]
    protected float _rotation;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _scale = Vector2D<float>.One;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _pivot = Vector2D<float>.Zero; // Top-Left default

    // Matrix caching logic...
}
```

### UserInterfaceElement
Updated to inherit from RectTransform.

```csharp
public partial class UserInterfaceElement : RectTransform
{
    // Inherits all transform logic
    // Adds UI-specific logic (Input, Rendering)
}
```
