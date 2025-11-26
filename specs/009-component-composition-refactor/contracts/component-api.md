# Component API Contracts

## ITransformable

```csharp
public interface ITransformable
{
    Vector3D<float> Position { get; }
    Quaternion<float> Rotation { get; }
    Vector3D<float> Scale { get; }
    Matrix4X4<float> WorldMatrix { get; }
    // ... mutation methods ...
}
```

## IDrawable

```csharp
public interface IDrawable : IRuntimeComponent
{
    bool IsVisible();
    IEnumerable<DrawCommand> GetDrawCommands(RenderContext context);
}
```

## UserInterfaceElement (Public API)

```csharp
public partial class UserInterfaceElement : RuntimeComponent, ITransformable
{
    // Layout Properties
    public Vector2D<float> Size { get; set; }
    public Vector2D<float> AnchorPoint { get; set; }
    
    // ITransformable Implementation
    public Vector3D<float> Position { get; }
    // ...
}
```
