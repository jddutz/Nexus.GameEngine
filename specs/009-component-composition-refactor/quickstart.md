# Quickstart: Component Composition

**Feature**: Component Composition Refactor
**Branch**: `009-component-composition-refactor`

## Creating a Button (Composition)

Old way (Inheritance):
```csharp
var button = new ButtonElement(); // Inherits DrawableElement
```

New way (Composition):
```csharp
// 1. Create the layout container
var button = new UserInterfaceElementTemplate 
{
    Size = new Vector2D<float>(200, 50),
    Position = new Vector3D<float>(100, 100, 0)
};

// 2. Add visual background
button.Children.Add(new SpriteRendererTemplate 
{
    Texture = "Textures/ButtonBg",
    Color = Colors.White
});

// 3. Add text label
button.Children.Add(new TextRendererTemplate 
{
    Text = "Click Me",
    Color = Colors.Black,
    Position = new Vector3D<float>(0, 0, 1) // Offset Z for layering
});
```

## Creating Custom Visuals

To create a custom visual component:

1. Create a class inheriting `RuntimeComponent` and implementing `IDrawable`.
2. Inject dependencies via constructor.
3. Implement `GetDrawCommands`.

```csharp
public class HealthBarRenderer : RuntimeComponent, IDrawable
{
    public HealthBarRenderer(IGraphicsContext graphics) { ... }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        // Use Parent.WorldMatrix for positioning
        var transform = (Parent as ITransformable)?.WorldMatrix ?? Matrix4X4.Identity;
        // ... emit draw commands ...
    }
}
```
