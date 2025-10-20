# Silk.NET Setup and Graphics Integration

## Overview

The Nexus Game Engine uses Silk.NET as its primary graphics and windowing abstraction layer. This document covers the integration patterns, resource management, and rendering architecture built on top of Silk.NET.

## Silk.NET Integration

### Core Dependencies

The engine integrates the following Silk.NET packages:

- **Silk.NET.OpenGL**: Direct OpenGL API access for rendering
- **Silk.NET.Windowing**: Window creation and management
- **Silk.NET.Input**: Input device handling
- **Silk.NET.Maths**: Mathematical types and operations

### Architecture Integration

```csharp
// Core rendering interface provides direct GL access
public interface IRenderer
{
    GL GL { get; }  // Direct Silk.NET OpenGL interface
    void RenderFrame();
    IRuntimeComponent? RootComponent { get; set; }
}

// Components render directly using GL calls
public class BackgroundLayer : RuntimeComponent, IDrawable
{
    public void OnRender(IRenderer renderer, double deltaTime)
    {
        var gl = renderer.GL;  // Direct access to Silk.NET GL
        // Direct OpenGL calls for maximum performance
    }
}
```

## Resource Management with Silk.NET

### Attribute-Based Resource System

The engine implements a declarative resource management system that wraps Silk.NET OpenGL objects:

```csharp
// Declare shared resources using attributes
public static class Geometry
{
    [SharedResource("FullScreenQuad", ResourceType.Geometry)]
    public static readonly GeometryDefinition FullScreenQuad = new()
    {
        Vertices = [ /* vertex data */ ],
        Indices = [ /* index data */ ],
        Attributes = [ /* vertex attributes */ ]
    };
}

// Resource definitions create actual OpenGL objects
public record GeometryDefinition : IResourceDefinition
{
    public uint CreateResource(GL gl, IAssetService? assetService = null)
    {
        // Creates VAO using Silk.NET GL interface
        return CreateVertexArrayObject(gl);
    }
}
```

### Resource Lifecycle Management

The resource management system handles OpenGL object lifecycle:

```csharp
public interface IResourceManager
{
    // Type-safe resource access with automatic creation
    T GetOrCreateResource<T>(IResourceDefinition definition) where T : struct;

    // Component-scoped resources for automatic cleanup
    void SetResourceScope(string resourceName, IRuntimeComponent? component);

    // Memory pressure handling
    (int freed, long memory) PurgeComponentResources(IRuntimeComponent component);
}
```

### OpenGL Object Types

The system manages these Silk.NET OpenGL objects:

- **Vertex Array Objects (VAOs)**: Geometry definitions with vertex attributes
- **Shader Programs**: Compiled vertex/fragment shader combinations
- **Textures**: 2D texture objects loaded from assets
- **Framebuffers**: Render targets for multi-pass rendering
- **Uniform Buffer Objects**: Shared uniform data across shaders

## Rendering Pipeline

### Multi-Pass Architecture

The rendering system supports configurable render passes:

```csharp
public class RenderPassConfiguration
{
    public string Name { get; init; }
    public uint Id { get; init; }
    public RenderPassType Type { get; init; }
    public bool ClearColor { get; init; }
    public bool ClearDepth { get; init; }
    public Vector4D<float> ClearColorValue { get; init; }
}
```

### Component Rendering Integration

Components implement `IDrawable` to participate in rendering:

```csharp
public interface IDrawable : IRuntimeComponent
{
    void OnRender(IRenderer renderer, double deltaTime);
    bool ShouldRender { get; }
    int RenderPriority { get; }  // 0=Background, 100-299=3D, 400+=UI
    Box3D<float> BoundingBox { get; }  // For frustum culling
    uint RenderPassFlags { get; }  // Which passes to participate in
    bool ShouldRenderChildren { get; }  // Hierarchical culling
}
```

## Shader Management

### Declarative Shader Definitions

Shaders are declared as resource definitions:

```csharp
public static class Shaders
{
    [SharedResource("BackgroundSolid", ResourceType.Shader)]
    public static readonly ShaderDefinition BackgroundSolid = new()
    {
        VertexSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            layout (location = 1) in vec2 aTexCoord;
            out vec2 TexCoord;
            void main() {
                gl_Position = vec4(aPosition, 1.0);
                TexCoord = aTexCoord;
            }",
        FragmentSource = @"
            #version 330 core
            out vec4 FragColor;
            uniform vec4 uBackgroundColor;
            uniform float uFade;
            void main() {
                FragColor = vec4(uBackgroundColor.rgb, uBackgroundColor.a * uFade);
            }"
    };
}
```

### Shader Compilation and Caching

The resource manager handles shader compilation:

```csharp
public record ShaderDefinition : IResourceDefinition
{
    public uint CreateResource(GL gl, IAssetService? assetService = null)
    {
        var vertexShader = CompileShader(gl, ShaderType.VertexShader, VertexSource);
        var fragmentShader = CompileShader(gl, ShaderType.FragmentShader, FragmentSource);

        var program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);

        // Cleanup and validation...
        return program;
    }
}
```

## Asset Loading Integration

### Texture Loading Pipeline

The asset system integrates with Silk.NET for texture loading:

```csharp
public class TextureAsset
{
    public uint Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void LoadFromFile(GL gl, string path)
    {
        using var image = Image.Load<Rgba32>(path);
        var textureData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(textureData);

        Handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Handle);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8,
            (uint)image.Width, (uint)image.Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, textureData);

        // Set texture parameters...
    }
}
```

### Asset Reference System

Components reference assets using strongly-typed references:

```csharp
public class BackgroundLayer : RuntimeComponent, IDrawable
{
    public record Template : RuntimeComponent.Template
    {
        public AssetReference<Texture2D>? ImageAsset { get; init; }
    }

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        if (Template.ImageAsset != null)
        {
            var texture = _resourceManager.GetOrCreateResource<uint>(Template.ImageAsset.Id);
            renderer.GL.BindTexture(TextureTarget.Texture2D, texture);
        }
    }
}
```

## Performance Considerations

### Resource Sharing

The attribute-based system promotes resource sharing:

- **Shared Geometry**: Common quads, sprites used by multiple components
- **Shader Reuse**: Same shaders used across similar rendering operations
- **Texture Atlasing**: Multiple small textures combined into larger atlases
- **Batch Rendering**: Components group draws by render state

### Memory Management

Component-scoped resources enable automatic cleanup:

```csharp
// Component-scoped resource automatically cleaned up on component disposal
var backgroundTexture = new AssetDefinition
{
    Name = $"Background_{componentId}",
    Scope = this,  // Tied to this component's lifecycle
    AssetPath = texturePath,
    AssetType = typeof(Texture2D)
};
```

### OpenGL State Management

The rendering system minimizes state changes:

- **Render Priority Sorting**: Components rendered in priority order
- **Batch Grouping**: Similar render operations batched together
- **State Caching**: OpenGL state tracked to avoid redundant calls
- **Resource Binding**: Textures and buffers bound once per batch

## Debugging and Validation

### OpenGL Error Checking

Debug builds include automatic error checking:

```csharp
public static class GLExtensions
{
    [Conditional("DEBUG")]
    public static void CheckError(this GL gl, string operation)
    {
        var error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            throw new InvalidOperationException($"OpenGL error after {operation}: {error}");
        }
    }
}
```

### Resource Debugging

The resource manager provides debugging information:

```csharp
public interface IResourceManager
{
    ResourceMemoryStats GetMemoryStats();
    IEnumerable<ResourceInfo> GetResourceInfo();
}

// Usage in debugging/profiling
var stats = resourceManager.GetMemoryStats();
logger.LogDebug("GPU Memory Usage: {stats.EstimatedMemoryUsage / 1024 / 1024}MB");
```

## Best Practices

### Resource Management

1. **Use Shared Resources**: Declare common geometry and shaders as shared resources
2. **Component Scoping**: Scope assets to components for automatic cleanup
3. **Lazy Loading**: Resources created only when first accessed
4. **Memory Monitoring**: Monitor memory usage and purge when needed

### Rendering Performance

1. **Minimize State Changes**: Group similar rendering operations
2. **Use Render Priorities**: Sort components by render priority
3. **Frustum Culling**: Implement proper bounding boxes for culling
4. **Batch Rendering**: Use the batch system for similar draws

### Shader Development

1. **Shared Shaders**: Create reusable shaders for common operations
2. **Uniform Buffers**: Use UBOs for frequently updated uniforms
3. **Shader Variants**: Create specialized versions for different use cases
4. **Error Handling**: Include proper shader compilation error handling

This integration provides a robust foundation for graphics development while maintaining the flexibility and performance of direct OpenGL access through Silk.NET.
