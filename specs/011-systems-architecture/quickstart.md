# Quickstart: Systems Architecture

**Date**: December 6, 2025  
**Audience**: Framework developers working on Nexus.GameEngine infrastructure  
**Goal**: Get started using systems pattern in 5 minutes

## Overview

The systems pattern eliminates constructor injection bloat in framework classes by providing access to framework services through strongly-typed system properties and extension methods.

**Before** (Constructor Injection):
```csharp
public class Renderer : IRenderer
{
    private readonly IGraphicsContext _context;
    private readonly IPipelineManager _pipelines;
    private readonly ISwapChain _swapChain;
    private readonly ICommandPoolManager _commandPools;
    private readonly ISyncManager _sync;
    private readonly IDescriptorManager _descriptors;
    private readonly IResourceManager _resources;
    private readonly IBufferManager _buffers;
    // ... 9 constructor parameters
    
    public Renderer(
        IGraphicsContext context,
        IPipelineManager pipelines,
        ISwapChain swapChain,
        ICommandPoolManager commandPools,
        ISyncManager sync,
        IDescriptorManager descriptors,
        IResourceManager resources,
        IBufferManager buffers)
    {
        _context = context;
        _pipelines = pipelines;
        // ... assign all 9 parameters
    }
}
```

**After** (Systems Pattern):
```csharp
public class Renderer : IRenderer, IRequiresSystems
{
    public IGraphicsSystem Graphics { get; internal set; } = null!;
    public IResourceSystem Resources { get; internal set; } = null!;
    
    public Renderer() { } // Parameterless constructor
    
    public void InitializeSystems(IServiceProvider serviceProvider)
    {
        Graphics = serviceProvider.GetRequiredService<IGraphicsSystem>();
        Resources = serviceProvider.GetRequiredService<IResourceSystem>();
    }
    
    public void RenderFrame()
    {
        Graphics.BeginFrame();
        Graphics.DrawQuad(position, size, color);
        Graphics.EndFrame();
    }
}
```

## Quick Start: Using Systems in 3 Steps

### Step 1: Add System Properties

Add system properties to your framework class with public getter and internal setter:

```csharp
public class MyFrameworkClass : IMyFrameworkClass, IRequiresSystems
{
    // System properties - public get, internal set
    public IGraphicsSystem Graphics { get; internal set; } = null!;
    public IResourceSystem Resources { get; internal set; } = null!;
    public IContentSystem Content { get; internal set; } = null!;
    
    // Parameterless constructor (or domain dependencies only)
    public MyFrameworkClass() { }
}
```

### Step 2: Implement InitializeSystems

Implement `IRequiresSystems.InitializeSystems()` to assign system properties:

```csharp
public void InitializeSystems(IServiceProvider serviceProvider)
{
    Graphics = serviceProvider.GetRequiredService<IGraphicsSystem>();
    Resources = serviceProvider.GetRequiredService<IResourceSystem>();
    Content = serviceProvider.GetRequiredService<IContentSystem>();
}
```

### Step 3: Use Systems via Extension Methods

Access framework capabilities through system extension methods:

```csharp
public void DoWork()
{
    // Graphics operations
    Graphics.BeginFrame();
    Graphics.DrawQuad(position, size, color);
    Graphics.EndFrame();
    
    // Resource operations
    var shader = Resources.LoadShader("shaders/basic.spv");
    var geometry = Resources.CreateGeometry(vertices, indices);
    
    // Content operations
    var component = Content.CreateInstance(template);
    Content.Activate(component);
}
```

## Available Systems

| System | Access | Extension Methods Namespace |
|--------|--------|------------------------------|
| **Graphics** | `this.Graphics` | `Nexus.GameEngine.Systems.Extensions` |
| **Resources** | `this.Resources` | `Nexus.GameEngine.Systems.Extensions` |
| **Content** | `this.Content` | `Nexus.GameEngine.Systems.Extensions` |
| **Window** | `this.Window` | `Nexus.GameEngine.Systems.Extensions` |
| **Input** | `this.Input` | `Nexus.GameEngine.Systems.Extensions` |

## Common Operations

### Graphics System

```csharp
// Frame management
Graphics.BeginFrame();
Graphics.EndFrame();

// Drawing (high-level)
Graphics.DrawQuad(position, size, color);
Graphics.DrawSprite(sprite, transform);
Graphics.DrawMesh(mesh, material, transform);

// Pipeline management
var pipeline = Graphics.GetOrCreatePipeline(config);
Graphics.BindPipeline(pipeline);

// Command buffer operations
Graphics.BeginCommandBuffer();
Graphics.SubmitCommands();

// Synchronization
Graphics.WaitForFrame(frameIndex);
Graphics.SignalFrameComplete(frameIndex);
```

### Resource System

```csharp
// Shader loading
var shader = Resources.LoadShader("shaders/vertex.spv");
var shaderModule = Resources.CreateShaderModule(spirvBytes);

// Geometry creation
var geometry = Resources.CreateGeometry(vertices, indices);
var buffer = Resources.CreateBuffer(size, usage);

// Resource queries
var exists = Resources.TryGetShader("shader-name", out var shader);
var geometry = Resources.GetGeometry("geometry-id");
```

### Content System

```csharp
// Component lifecycle
var component = Content.CreateInstance(template);
Content.Activate(component);
Content.Deactivate(component);
Content.Dispose(component);

// Component queries
var active = Content.GetActiveComponents<IRenderable>();
var cached = Content.TryGetCached("component-id", out var component);
```

### Window System

```csharp
// Window properties
var size = Window.GetSize();
var title = Window.GetTitle();
Window.SetTitle("New Title");

// Input context
var input = Window.GetInputContext();
var keyboard = Window.GetKeyboard();
var mouse = Window.GetMouse();

// Window events
Window.OnResize += (size) => { /* handle resize */ };
Window.OnClose += () => { /* handle close */ };
```

### Input System

```csharp
// Keyboard input
var isPressed = Input.IsKeyPressed(Key.W);
var wasJustPressed = Input.WasKeyJustPressed(Key.Space);

// Mouse input
var position = Input.GetMousePosition();
var delta = Input.GetMouseDelta();
var isButtonPressed = Input.IsMouseButtonPressed(MouseButton.Left);

// Input axes (configured in input map)
var horizontal = Input.GetAxis("Horizontal");
var vertical = Input.GetAxis("Vertical");
```

## DI Registration

When registering framework classes with systems in the DI container, use factory pattern:

```csharp
// Register systems (done once at startup)
services.AddSingleton<IGraphicsSystem>(sp => new GraphicsSystem(
    sp.GetRequiredService<IGraphicsContext>(),
    sp.GetRequiredService<IPipelineManager>(),
    sp.GetRequiredService<ISwapChain>(),
    sp.GetRequiredService<ICommandPoolManager>(),
    sp.GetRequiredService<ISyncManager>(),
    sp.GetRequiredService<IDescriptorManager>()));

services.AddSingleton<IResourceSystem>(sp => new ResourceSystem(
    sp.GetRequiredService<IResourceManager>(),
    sp.GetRequiredService<IGeometryResourceManager>(),
    sp.GetRequiredService<IShaderResourceManager>(),
    sp.GetRequiredService<IBufferManager>()));

// Register framework class using factory pattern
services.AddSingleton<IRenderer>(sp =>
{
    var renderer = new Renderer();
    renderer.InitializeSystems(sp);
    return renderer;
});
```

## Testing with Mock Systems

Create mock system implementations for unit testing:

```csharp
public class MockGraphicsSystem : IGraphicsSystem
{
    public List<string> OperationLog { get; } = new();
    
    public void LogOperation(string operation) => OperationLog.Add(operation);
}

public static class MockGraphicsSystemExtensions
{
    public static void DrawQuad(this IGraphicsSystem graphics, Vector2 pos, Vector2 size, Vector4 color)
    {
        if (graphics is MockGraphicsSystem mock)
            mock.LogOperation($"DrawQuad({pos}, {size}, {color})");
    }
}

// In your test
[Fact]
public void RenderFrame_DrawsQuad_WhenCalled()
{
    // Arrange
    var mockGraphics = new MockGraphicsSystem();
    var renderer = new Renderer
    {
        Graphics = mockGraphics,
        Resources = new MockResourceSystem()
    };
    
    // Act
    renderer.RenderFrame();
    
    // Assert
    Assert.Contains(mockGraphics.OperationLog, op => op.StartsWith("DrawQuad"));
}
```

## Migration Checklist

When migrating an existing framework class to use systems:

- [ ] Remove framework service constructor parameters
- [ ] Add system properties (`public {ISystem} {Name} { get; internal set; } = null!;`)
- [ ] Implement `IRequiresSystems` interface
- [ ] Add `InitializeSystems()` method to assign systems
- [ ] Update DI registration to use factory pattern with `InitializeSystems()` call
- [ ] Replace direct service field access with system extension methods
- [ ] Update unit tests to use mock systems instead of mocked services
- [ ] Verify integration tests pass unchanged
- [ ] Verify no performance degradation

## Best Practices

### DO ✅

- Use system properties for framework services (graphics, resources, content, etc.)
- Use parameterless constructors for framework classes (or domain dependencies only)
- Use extension methods to access system functionality
- Initialize systems via `InitializeSystems()` called by DI factory
- Create mock systems for unit testing

### DON'T ❌

- Use constructor injection for framework services (defeats purpose)
- Use service locator pattern (`GetRequiredService<T>()`) outside DI root
- Create framework classes manually without calling `InitializeSystems()`
- Expose system implementation types in extension method signatures
- Mix constructor injection and systems pattern in same class

## Performance Tips

- Extension methods have **zero overhead** - they compile to identical IL as instance methods
- System properties are direct references - no dictionary lookups or reflection
- Systems are singletons - one instance per application, no allocation overhead
- JIT inlines extension methods identically to instance methods

## IntelliSense Discoverability

Type `this.Graphics.` and IntelliSense shows all available graphics operations:

```
this.Graphics.
  BeginFrame()
  EndFrame()
  DrawQuad(...)
  DrawSprite(...)
  DrawMesh(...)
  GetOrCreatePipeline(...)
  BindPipeline(...)
  ...
```

Extension methods appear as instance methods with full documentation and parameter info.

## Troubleshooting

### "Object reference not set to an instance of an object" when accessing system

**Problem**: System property is null
**Solution**: Ensure `InitializeSystems()` is called after construction. Framework classes must be created via DI factory, not manual `new`.

### Extension methods don't appear in IntelliSense

**Problem**: Extension method namespace not imported
**Solution**: Add `using Nexus.GameEngine.Systems.Extensions;` or ensure it's in `GlobalUsings.cs`

### Can't access system implementation details

**Problem**: Trying to access internal implementation from extension method
**Solution**: Extension methods should cast to implementation internally using private `AsImpl()` helper

### Performance degradation after migration

**Problem**: Unexpected performance drop
**Solution**: Verify extension methods are inlining (use BenchmarkDotNet). Ensure no boxing/unboxing. Check for unnecessary allocations.

## Next Steps

1. Review [data-model.md](data-model.md) for detailed entity definitions
2. Review [contracts/](contracts/) for system interface contracts
3. Review [research.md](research.md) for implementation decisions and alternatives
4. Review [plan.md](plan.md) for migration strategy
5. Start migrating high-impact classes (Renderer, PipelineManager, etc.)

## Summary

Systems pattern provides clean, discoverable access to framework services without constructor injection bloat. Use system properties, implement `IRequiresSystems`, and access functionality through extension methods. Tests use mock systems. Performance is identical to direct method calls.
