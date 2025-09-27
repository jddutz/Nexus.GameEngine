# BackgroundLayer Component Design

## Overview

The BackgroundLayer component will render a full-screen quad with configurable materials. This is intended to be a foundational UI component that can display solid colors, procedural textures, or image assets as a background layer.

## Template Properties Analysis

Based on the existing patterns in the codebase (Border, TextElement) and the requirements, the Template should include:

### Core Visual Properties

1. **MaterialType** (enum): SolidColor | ProceduralTexture | ImageAsset
2. **BackgroundColor** (Vector4D<float>): Base color for solid backgrounds or tinting
3. **ImageAsset** (AssetReference<Texture2D>): Reference to background image
4. **ProceduralParameters** (object/record): Parameters for procedural generation

### Material Effects Properties

1. **Tint** (Vector4D<float>): Color tinting overlay (default: white/transparent)
2. **Saturation** (float): Saturation adjustment (0.0 = grayscale, 1.0 = normal, >1.0 = oversaturated)
3. **Fade** (float): Opacity/fade level (0.0 = transparent, 1.0 = opaque)
4. **BlendMode** (BlendingMode): How this layer blends with content behind it

### UV/Texture Properties

1. **TextureWrapMode** (enum): Repeat | Clamp | Mirror
2. **TextureScale** (Vector2D<float>): UV scaling for tiling effects
3. **TextureOffset** (Vector2D<float>): UV offset for animation/positioning

## IRenderable Implementation Strategy

### Render Priority

- Should be very low priority (background layer) - suggest priority 0-10
- Background layers should render before all other content

### Render Pass Participation

- Should participate in main color pass
- May want to exclude from shadow passes
- UI pass participation depends on whether this is world-space or screen-space

### Bounding Box

- For full-screen quads, should return Box3D.Empty to prevent culling
- The component should always be rendered regardless of camera position

### ShouldRender Logic

- Check if IsVisible is true
- Check if Fade > 0.0 (no point rendering fully transparent)
- Always return true for background layers unless explicitly disabled

### OnRender Implementation Strategy

#### Approach 1: Direct GL Calls

```csharp
void OnRender(IRenderer renderer, double deltaTime)
{
    // 1. Set up full-screen quad geometry
    // 2. Choose shader based on MaterialType
    // 3. Bind appropriate textures/uniforms
    // 4. Render quad
}
```

#### Approach 2: Shared Resource Caching

```csharp
void OnRender(IRenderer renderer, double deltaTime)
{
    // 1. Get or create shared fullscreen quad from renderer
    // 2. Get or create appropriate shader program
    // 3. Set material uniforms
    // 4. Draw with cached resources
}
```

**Recommendation**: Use Approach 2 for better performance and resource sharing.

## Resource Management Considerations

### Shared Resources to Cache

1. **"fullscreen_quad_vao"**: Vertex Array Object for full-screen quad
2. **"background_solid_shader"**: Shader program for solid colors
3. **"background_texture_shader"**: Shader program for textured backgrounds
4. **"background_procedural_shader"**: Shader program for procedural generation

### Asset Loading

- Use AssetReference<Texture2D> pattern for image assets
- Implement lazy loading (load when first needed in OnRender)
- Handle asset loading failures gracefully (fallback to solid color)

## Shader Requirements

### Solid Color Shader

- Vertex shader: Simple fullscreen quad positioning
- Fragment shader: Output uniform color with tint/saturation/fade applied

### Texture Shader

- Vertex shader: Fullscreen quad with UV generation
- Fragment shader: Sample texture with UV transform, apply tint/saturation/fade

### Procedural Shader

- Vertex shader: Fullscreen quad with UV and time uniform
- Fragment shader: Generate patterns (noise, gradients, etc.) with parameters

## Performance Considerations

1. **Early Exit**: Skip rendering if fade <= 0.0
2. **Shader Switching**: Minimize shader program changes by batching
3. **Texture Binding**: Cache texture bindings across frames
4. **Uniform Updates**: Only update uniforms when properties change

## Configuration Validation

The component should validate:

1. MaterialType has appropriate supporting data (e.g., ImageAsset when MaterialType.ImageAsset)
2. Numeric ranges (Saturation >= 0, Fade 0.0-1.0, etc.)
3. Asset references are valid paths

## Integration with Component System

### Template Pattern

- Follow the existing RuntimeComponent.Template pattern
- Implement OnConfigure() to apply template properties to runtime instance
- Support declarative syntax in ComponentTemplate-derived classes

### Event-Driven Updates

- Fire property changed events when template properties change
- Consider implementing IAnimatable for smooth transitions
- Support real-time editing in development tools

## Shared Resource Management - Attribute-Based Declaration Pattern

### Problem with Current Approach

The existing GetOrCreate pattern scattered throughout components leads to:

- Duplicate resource creation code across components
- Inconsistent resource definitions (same quad defined differently)
- No central registry of what resources exist
- Difficult asset path management for textures

### Proposed Solution: Attribute-Based Resource Declaration

#### 1. Resource Declaration Attributes

Use attributes on static object instances to declare shared resources:

```csharp
// GameEngine/Graphics/Resources/SharedResourceAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class SharedResourceAttribute : Attribute
{
    public string ResourceName { get; }
    public ResourceType ResourceType { get; }
    public int Priority { get; set; } = 0; // For initialization order
    public string[] Dependencies { get; set; } = Array.Empty<string>(); // Resource dependencies

    public SharedResourceAttribute(string resourceName, ResourceType resourceType)
    {
        ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
        ResourceType = resourceType;
    }
}

public enum ResourceType
{
    Geometry,
    Shader,
    Texture,
    AssetReference,
    Material
}
```

#### 2. Resource Definition Types

Create strongly-typed resource definitions that can create OpenGL resources:

```csharp
// GameEngine/Graphics/Resources/ResourceDefinitions.cs
public interface IResourceDefinition
{
    string Name { get; }
    ResourceType Type { get; }
    bool IsPersistent { get; }  // true = never purged, false = purged when no components use it
    uint CreateResource(GL gl, IAssetService? assetService = null);
}

public record GeometryDefinition : IResourceDefinition
{
    public string Name { get; init; } = string.Empty;
    public ResourceType Type => ResourceType.Geometry;
    public bool IsPersistent { get; init; } = true;  // Core geometry is persistent by default
    public float[] Vertices { get; init; } = Array.Empty<float>();
    public uint[] Indices { get; init; } = Array.Empty<uint>();
    public VertexAttribute[] Attributes { get; init; } = Array.Empty<VertexAttribute>();

    public uint CreateResource(GL gl, IAssetService? assetService = null)
    {
        return CreateVAO(gl, Vertices, Indices, Attributes);
    }
}

public record ShaderDefinition : IResourceDefinition
{
    public string Name { get; init; } = string.Empty;
    public ResourceType Type => ResourceType.Shader;
    public bool IsPersistent { get; init; } = true;  // Core shaders are persistent by default
    public string VertexSource { get; init; } = string.Empty;
    public string FragmentSource { get; init; } = string.Empty;
    public string? GeometrySource { get; init; }

    public uint CreateResource(GL gl, IAssetService? assetService = null)
    {
        return CompileShaderProgram(gl, VertexSource, FragmentSource, GeometrySource);
    }
}

public record AssetDefinition : IResourceDefinition
{
    public string Name { get; init; } = string.Empty;
    public ResourceType Type => ResourceType.AssetReference;
    public bool IsPersistent { get; init; } = false;  // Assets are non-persistent by default
    public string AssetPath { get; init; } = string.Empty;
    public Type AssetType { get; init; } = typeof(object);

    public uint CreateResource(GL gl, IAssetService? assetService = null)
    {
        if (assetService == null)
            throw new InvalidOperationException("AssetService required for asset resources");

        var asset = assetService.Load<Texture2D>(AssetPath);
        return asset?.Handle ?? 0;
    }
}
```

#### 3. Declarative Resource Definitions

Split resources into intuitive, domain-specific classes:

```csharp
// GameEngine/Graphics/Resources/Geometry.cs
public static class Geometry
{
    [SharedResource("FullScreenQuad", ResourceType.Geometry, Priority = 10)]
    public static readonly GeometryDefinition FullScreenQuad = new()
    {
        Name = "FullScreenQuad",
        IsPersistent = true,  // Core geometry used by many components - never purge
        Vertices = [
            // Position        // TexCoords (NDC coordinates -1 to 1)
            -1.0f, -1.0f, 0.0f,  0.0f, 0.0f, // Bottom-left
             1.0f, -1.0f, 0.0f,  1.0f, 0.0f, // Bottom-right
             1.0f,  1.0f, 0.0f,  1.0f, 1.0f, // Top-right
            -1.0f,  1.0f, 0.0f,  0.0f, 1.0f  // Top-left
        ],
        Indices = [0, 1, 2, 2, 3, 0],
        Attributes = [
            new() { Location = 0, Size = 3, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 0 },
            new() { Location = 1, Size = 2, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 3 * sizeof(float) }
        ]
    };

    [SharedResource("SpriteQuad", ResourceType.Geometry, Priority = 10)]
    public static readonly GeometryDefinition SpriteQuad = new()
    {
        Name = "SpriteQuad",
        IsPersistent = true,  // Core geometry used by many components - never purge
        Vertices = [
            // Position        // TexCoords (Object space -0.5 to 0.5)
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f, // Bottom-left
             0.5f, -0.5f, 0.0f,  1.0f, 0.0f, // Bottom-right
             0.5f,  0.5f, 0.0f,  1.0f, 1.0f, // Top-right
            -0.5f,  0.5f, 0.0f,  0.0f, 1.0f  // Top-left
        ],
        Indices = [0, 1, 2, 2, 3, 0],
        Attributes = [
            new() { Location = 0, Size = 3, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 0 },
            new() { Location = 1, Size = 2, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 3 * sizeof(float) }
        ]
    };
}

// GameEngine/Graphics/Resources/Shaders.cs
public static class Shaders
{
    [SharedResource("BackgroundSolid", ResourceType.Shader, Priority = 20)]
    public static readonly ShaderDefinition BackgroundSolid = new()
    {
        Name = "BackgroundSolid",
        IsPersistent = true,  // Core shader used by many background components - never purge
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
uniform vec4 uTint;
uniform float uSaturation;
uniform float uFade;
void main() {
    vec4 color = uBackgroundColor * uTint;
    vec3 gray = vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114)));
    color.rgb = mix(gray, color.rgb, uSaturation);
    FragColor = vec4(color.rgb, color.a * uFade);
}"
    };

    [SharedResource("BackgroundTexture", ResourceType.Shader, Priority = 20)]
    public static readonly ShaderDefinition BackgroundTexture = new()
    {
        Name = "BackgroundTexture",
        IsPersistent = true,  // Core shader used by many background components - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform vec2 uTextureScale;
uniform vec2 uTextureOffset;
void main() {
    gl_Position = vec4(aPosition, 1.0);
    TexCoord = aTexCoord * uTextureScale + uTextureOffset;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D uTexture;
uniform vec4 uTint;
uniform float uSaturation;
uniform float uFade;
void main() {
    vec4 texColor = texture(uTexture, TexCoord);
    vec4 color = texColor * uTint;
    vec3 gray = vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114)));
    color.rgb = mix(gray, color.rgb, uSaturation);
    FragColor = vec4(color.rgb, color.a * uFade);
}"
    };

    [SharedResource("BasicSprite", ResourceType.Shader, Priority = 20)]
    public static readonly ShaderDefinition BasicSprite = new()
    {
        Name = "BasicSprite",
        IsPersistent = true,  // Core sprite shader used by many components - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform mat4 uTransform;
void main() {
    gl_Position = uTransform * vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D uTexture;
uniform vec4 uTint;
void main() {
    FragColor = texture(uTexture, TexCoord) * uTint;
}"
    };
}

// GameEngine/Graphics/Resources/CommonAssets.cs - For globally shared assets
public static class CommonAssets
{
    [SharedResource("DefaultTexture", ResourceType.AssetReference, Priority = 30)]
    public static readonly AssetDefinition DefaultTexture = new()
    {
        Name = "DefaultTexture",
        IsPersistent = true,  // Global fallback texture - never purge
        AssetPath = "textures/default_white.png",
        AssetType = typeof(Texture2D)
    };

    [SharedResource("ErrorTexture", ResourceType.AssetReference, Priority = 30)]
    public static readonly AssetDefinition ErrorTexture = new()
    {
        Name = "ErrorTexture",
        IsPersistent = true,  // Global fallback texture - never purge
        AssetPath = "textures/error_magenta.png",
        AssetType = typeof(Texture2D)
    };
}
```

#### 3a. Component-Scoped Asset Management

For component-specific assets, support both static declaration and dynamic registration:

```csharp
// Example: MainMenu component with its own background
// UI/Components/MainMenu.cs
public class MainMenu : ComponentTemplate
{
    // Component-scoped asset - not shared globally
    private static readonly AssetDefinition MenuBackground = new()
    {
        Name = "MainMenuBackground",
        IsPersistent = false,  // Component-specific asset - can be purged when component is disposed
        AssetPath = "textures/ui/main_menu_bg.png",
        AssetType = typeof(Texture2D)
    };

    private readonly IResourceManager _resourceManager;

    public MainMenu(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        // Register component-scoped asset dynamically
        _resourceManager.RegisterDefinition(MenuBackground);
    }

    public override IRuntimeComponent[] Components =>
    [
        new BackgroundLayer
        {
            Template = new BackgroundLayer.Template
            {
                MaterialType = MaterialType.ImageAsset,
                ImageAsset = "MainMenuBackground", // Reference to our component-scoped asset
                Tint = Vector4D.One,
                Fade = 1.0f
            }
        },
        // ... other menu components
    ];
}

// Alternative: Inline asset definition without static declaration
public class LoadingScreen : ComponentTemplate
{
    private readonly IResourceManager _resourceManager;

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        // Create and register asset definition on the fly
        var loadingBg = new AssetDefinition
        {
            Name = "LoadingScreenBackground",
            IsPersistent = false,  // Loading screen asset - can be purged when loading completes
            AssetPath = "textures/ui/loading_screen_bg.jpg",
            AssetType = typeof(Texture2D)
        };

        _resourceManager.RegisterDefinition(loadingBg);
    }
}
```

#### 3b. Enhanced IResourceManager Interface

Support both definition-based and string-based resource access:

```csharp
// Updated GameEngine/Graphics/Resources/IResourceManager.cs
public interface IResourceManager
{
    /// <summary>
    /// Gets or creates a resource using a resource definition directly.
    /// Automatically registers the definition if not already registered.
    /// Associates the resource with the specified component for dependency tracking.
    /// This is the preferred method for type-safe resource access.
    /// </summary>
    T GetOrCreateResource<T>(IResourceDefinition definition, IRuntimeComponent? usingComponent = null) where T : struct;

    /// <summary>
    /// Gets or creates a resource using the attribute-based registry system by name.
    /// Associates the resource with the specified component for dependency tracking.
    /// Used for resources discovered via reflection or previously registered.
    /// </summary>
    T GetOrCreateResource<T>(string resourceName, IRuntimeComponent? usingComponent = null) where T : struct;

    /// <summary>
    /// Gets a cached resource by name without creating it.
    /// Returns default(T) if not found.
    /// </summary>
    T GetSharedResourceID<T>(string name) where T : struct;

    /// <summary>
    /// Dynamically registers a resource definition at runtime.
    /// Useful for component-scoped assets that don't need global sharing.
    /// </summary>
    void RegisterDefinition(IResourceDefinition definition);

    /// <summary>
    /// Checks if a resource definition exists (either from attributes or dynamic registration).
    /// </summary>
    bool HasResource(string resourceName);

    /// <summary>
    /// Checks if a resource definition exists by comparing the definition itself.
    /// </summary>
    bool HasResource(IResourceDefinition definition);

    /// <summary>
    /// Gets a cached resource without creating it (legacy compatibility).
    /// </summary>
    T GetSharedResource<T>(string name);

    /// <summary>
    /// Sets a shared resource in the cache (legacy compatibility).
    /// </summary>
    void SetSharedResource<T>(string name, T resource);

    /// <summary>
    /// Preloads resources by type for better performance.
    /// </summary>
    void PreloadResourcesByType(ResourceType resourceType);

    /// <summary>
    /// Preloads specific resources by name.
    /// </summary>
    void PreloadResources(params string[] resourceNames);

    /// <summary>
    /// Gets all resource names from the registry.
    /// </summary>
    IEnumerable<string> GetResourceNames();

    /// <summary>
    /// Unregisters a dynamically registered resource (useful for cleanup).
    /// </summary>
    void UnregisterDefinition(string resourceName);

    /// <summary>
    /// Clears the resource cache (useful for testing).
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Removes a component's dependency on a resource.
    /// If no other components reference the resource and it's not persistent, it becomes eligible for purging.
    /// </summary>
    void RemoveComponentDependency(string resourceName, IRuntimeComponent component);

    /// <summary>
    /// Purges all resources that have no component dependencies and are not marked as persistent.
    /// Returns number of resources purged and memory freed (estimated).
    /// </summary>
    (int resourcesFreed, long memoryFreed) PurgeUnreferencedResources();

    /// <summary>
    /// Purges all resources associated with a specific component when it's disposed.
    /// Also removes the component from dependency lists of all resources.
    /// </summary>
    (int resourcesFreed, long memoryFreed) PurgeComponentResources(IRuntimeComponent component);
}
```

#### 3c. Enhanced ResourceManager Implementation

Support definition-based resource creation with automatic registration:

```csharp
// Updated GameEngine/Graphics/Resources/AttributeBasedResourceManager.cs
public class AttributeBasedResourceManager : IResourceManager
{
    private readonly GL _gl;
    private readonly IAssetService _assetService;
    private readonly ResourceRegistry _registry;
    private readonly ConcurrentDictionary<string, object> _sharedResources = new();
    private readonly ILogger<AttributeBasedResourceManager> _logger;

    // Primary method - accepts definition directly
    public T GetOrCreateResource<T>(IResourceDefinition definition) where T : struct
    {
        // Check cache first using definition name
        var cached = GetSharedResourceID<T>(definition.Name);
        if (!EqualityComparer<T>.Default.Equals(cached, default(T)))
            return cached;

        // Register definition if not already registered
        if (!HasResource(definition))
        {
            RegisterDefinition(definition);
        }

        // Create and cache resource
        var resourceHandle = definition.CreateResource(_gl, _assetService);
        var result = (T)(object)resourceHandle;
        SetSharedResource(definition.Name, result);
        _logger.LogDebug("Created resource from definition: {ResourceName} = {Handle}", definition.Name, resourceHandle);
        return result;
    }

    // Secondary method - string-based lookup for registered resources
    public T GetOrCreateResource<T>(string resourceName) where T : struct
    {
        // Check cache first
        var cached = GetSharedResourceID<T>(resourceName);
        if (!EqualityComparer<T>.Default.Equals(cached, default(T)))
            return cached;

        // Get definition from registry
        var definition = _registry.GetResourceDefinition(resourceName);
        if (definition == null)
            throw new InvalidOperationException($"Unknown resource: {resourceName}");

        // Create and cache resource
        var resourceHandle = definition.CreateResource(_gl, _assetService);
        var result = (T)(object)resourceHandle;
        SetSharedResource(resourceName, result);
        _logger.LogDebug("Created resource from name: {ResourceName} = {Handle}", resourceName, resourceHandle);
        return result;
    }

    // New method for getting cached resources by name
    public T GetSharedResourceID<T>(string name) where T : struct
    {
        if (_sharedResources.TryGetValue(name, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return default(T)!;
    }

    public bool HasResource(IResourceDefinition definition)
    {
        return _registry.HasResource(definition.Name);
    }

    public bool HasResource(string resourceName)
    {
        return _registry.HasResource(resourceName);
    }

    public void RegisterDefinition(IResourceDefinition definition)
    {
        _registry.RegisterDefinition(definition);
    }

    // Legacy compatibility methods
    public T GetSharedResource<T>(string name) => GetSharedResourceID<T>(name);

    public void SetSharedResource<T>(string name, T resource)
    {
        if (resource != null)
        {
            _sharedResources[name] = resource;
            _logger.LogTrace("Set shared resource '{Name}' of type {Type}", name, typeof(T).Name);
        }
    }

    // ... other existing methods
}

    /// <summary>
    /// Sets a shared resource in the cache.
    /// </summary>
    void SetSharedResource<T>(string name, T resource);

    /// <summary>
    /// Preloads resources by type for better performance.
    /// </summary>
    void PreloadResourcesByType(ResourceType resourceType);

    /// <summary>
    /// Preloads specific resources by name.
    /// </summary>
    void PreloadResources(params string[] resourceNames);

    /// <summary>
    /// Gets all resource names from the registry.
    /// </summary>
    IEnumerable<string> GetResourceNames();

    /// <summary>
    /// Unregisters a dynamically registered resource (useful for cleanup).
    /// </summary>
    void UnregisterDefinition(string resourceName);

    /// <summary>
    /// Clears the resource cache (useful for testing).
    /// </summary>
    void ClearCache();
}
```

#### 3c. Enhanced ResourceRegistry Implementation

Support both static discovery and dynamic registration:

```csharp
// Updated GameEngine/Graphics/Resources/ResourceRegistry.cs
public class ResourceRegistry
{
    private readonly Dictionary<string, (IResourceDefinition definition, SharedResourceAttribute? attribute)> _resources = new();
    private readonly object _lock = new object();

    public void RegisterDefinition(IResourceDefinition definition)
    {
        lock (_lock)
        {
            if (_resources.ContainsKey(definition.Name))
            {
                _logger.LogWarning("Overriding existing resource: {ResourceName}", definition.Name);
            }

            _resources[definition.Name] = (definition, null); // null attribute indicates dynamic registration
            _logger.LogDebug("Dynamically registered resource: {ResourceName} ({ResourceType})", definition.Name, definition.Type);
        }
    }

    public void UnregisterDefinition(string resourceName)
    {
        lock (_lock)
        {
            if (_resources.TryGetValue(resourceName, out var entry) && entry.attribute == null)
            {
                _resources.Remove(resourceName);
                _logger.LogDebug("Unregistered dynamic resource: {ResourceName}", resourceName);
            }
            else
            {
                _logger.LogWarning("Cannot unregister static resource: {ResourceName}", resourceName);
            }
        }
    }

    public bool HasResource(string resourceName)
    {
        lock (_lock)
        {
            return _resources.ContainsKey(resourceName);
        }
    }

    // ... existing discovery methods
}
```

#### 4. Reflection-Based Discovery System

Automatically discover resources using reflection:

```csharp
// GameEngine/Graphics/Resources/ResourceRegistry.cs
public class ResourceRegistry
{
    private readonly Dictionary<string, (IResourceDefinition definition, SharedResourceAttribute attribute)> _resources = new();

    public ResourceRegistry(ILogger<ResourceRegistry> logger)
    {
        DiscoverResources();
    }

    public IResourceDefinition? GetResourceDefinition(string name)
    {
        return _resources.TryGetValue(name, out var entry) ? entry.definition : null;
    }

    private void DiscoverResources()
    {
        var assemblies = new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() }
            .Where(a => a != null).Cast<Assembly>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed);

            foreach (var type in types)
            {
                var members = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Cast<MemberInfo>()
                    .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                    .Where(m => m.GetCustomAttribute<SharedResourceAttribute>() != null);

                foreach (var member in members)
                {
                    ProcessResourceMember(member);
                }
            }
        }
    }
}
```

#### 5. Integrated Resource Manager

Replace IRenderer's shared resource methods with dedicated resource management:

```csharp
// GameEngine/Graphics/Resources/IResourceManager.cs
public interface IResourceManager
{
    /// <summary>
    /// Gets or creates a resource using the attribute-based registry system.
    /// </summary>
    T GetOrCreateResource<T>(string resourceName) where T : struct;

    /// <summary>
    /// Gets a cached resource without creating it.
    /// </summary>
    T GetSharedResource<T>(string name);

    /// <summary>
    /// Sets a shared resource in the cache.
    /// </summary>
    void SetSharedResource<T>(string name, T resource);

    /// <summary>
    /// Preloads resources by type for better performance.
    /// </summary>
    void PreloadResourcesByType(ResourceType resourceType);

    /// <summary>
    /// Preloads specific resources by name.
    /// </summary>
    void PreloadResources(params string[] resourceNames);

    /// <summary>
    /// Gets all resource names from the registry.
    /// </summary>
    IEnumerable<string> GetResourceNames();

    /// <summary>
    /// Clears the resource cache (useful for testing).
    /// </summary>
    void ClearCache();
}

// GameEngine/Graphics/Resources/AttributeBasedResourceManager.cs
public class AttributeBasedResourceManager : IResourceManager
{
    private readonly GL _gl;
    private readonly IAssetService _assetService;
    private readonly ResourceRegistry _registry;
    private readonly ConcurrentDictionary<string, object> _sharedResources = new();
    private readonly ILogger<AttributeBasedResourceManager> _logger;

    public AttributeBasedResourceManager(
        GL gl,
        IAssetService assetService,
        ResourceRegistry registry,
        ILogger<AttributeBasedResourceManager> logger)
    {
        _gl = gl;
        _assetService = assetService;
        _registry = registry;
        _logger = logger;
    }

    public T GetOrCreateResource<T>(string resourceName) where T : struct
    {
        // Check cache first
        var cached = GetSharedResource<T>(resourceName);
        if (!EqualityComparer<T>.Default.Equals(cached, default(T)))
            return cached;

        // Get definition from registry
        var definition = _registry.GetResourceDefinition(resourceName);
        if (definition == null)
            throw new InvalidOperationException($"Unknown resource: {resourceName}");

        // Create and cache resource
        var resourceHandle = definition.CreateResource(_gl, _assetService);
        var result = (T)(object)resourceHandle;
        SetSharedResource(resourceName, result);
        _logger.LogDebug("Created resource: {ResourceName} = {Handle}", resourceName, resourceHandle);
        return result;
    }

    public T GetSharedResource<T>(string name)
    {
        if (_sharedResources.TryGetValue(name, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return default(T)!;
    }

    public void SetSharedResource<T>(string name, T resource)
    {
        if (resource != null)
        {
            _sharedResources[name] = resource;
            _logger.LogTrace("Set shared resource '{Name}' of type {Type}", name, typeof(T).Name);
        }
    }

    public void PreloadResourcesByType(ResourceType resourceType)
    {
        var resources = _registry.GetResourceNames()
            .Where(name => _registry.GetResourceType(name) == resourceType);

        foreach (var name in resources)
        {
            try
            {
                GetOrCreateResource<uint>(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload resource: {ResourceName}", name);
            }
        }
    }

    public void PreloadResources(params string[] resourceNames)
    {
        foreach (var name in resourceNames)
        {
            try
            {
                GetOrCreateResource<uint>(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload resource: {ResourceName}", name);
            }
        }
    }

    public IEnumerable<string> GetResourceNames() => _registry.GetResourceNames();

    public void ClearCache() => _sharedResources.Clear();
}
```

#### 6. Updated IRenderer Interface

Remove resource management from IRenderer, focusing it on rendering concerns:

```csharp
// Updated GameEngine/Graphics/Rendering/IRenderer.cs
public interface IRenderer
{
    /// <summary>
    /// Direct access to Silk.NET OpenGL interface for component rendering.
    /// Use GLRenderingExtensions for common helper methods.
    /// </summary>
    GL GL { get; }

    /// <summary>
    /// Collection of draw batches for lambda-based batching support.
    /// Provides read-only access to all batches for debugging and statistics.
    /// </summary>
    List<DrawBatch> Batches { get; }

    /// <summary>
    /// Gets or creates a draw batch for the specified batch key.
    /// Uses object-based keys for maximum flexibility in batching strategies.
    /// </summary>
    /// <param name="batchKey">Unique key identifying the batch (typically tuple of render state)</param>
    /// <returns>Existing or newly created draw batch for the key</returns>
    DrawBatch GetOrCreateBatch(object batchKey);

    /// <summary>
    /// Root component for the component tree to render.
    /// The renderer will walk this tree during RenderFrame() to collect draw commands.
    /// </summary>
    IRuntimeComponent? RootComponent { get; set; }

    /// <summary>
    /// List of discovered cameras in the component tree.
    /// Automatically populated when RenderFrame() is called if empty.
    /// </summary>
    IReadOnlyList<ICamera> Cameras { get; }

    /// <summary>
    /// Orchestrates the actual rendering process through all configured passes.
    /// Called after the application has walked the component tree to update GL state.
    /// This method executes the render passes in dependency order.
    /// </summary>
    void RenderFrame();
}

// Note: GetSharedResource<T> and SetSharedResource<T> methods removed from IRenderer
// These are now handled by IResourceManager
```

#### 7. Component Usage Pattern

Components now use IResourceManager with intuitive resource class names:

````csharp
public class BackgroundLayer : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    public BackgroundLayer(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        // Direct definition access - clean and type-safe, no strings!
        var quadVAO = _resourceManager.GetOrCreateResource<uint>(Geometry.FullScreenQuad);
        var shader = _resourceManager.GetOrCreateResource<uint>(Shaders.BackgroundSolid);

        // Use resources for rendering with renderer's GL context
        var gl = renderer.GL;
        gl.UseProgram(shader);
        gl.BindVertexArray(quadVAO);
        // ... rendering logic
    }
}

// Example with component-scoped asset
public class SplashScreen : ComponentTemplate, IRenderable
{
    private readonly IResourceManager _resourceManager;

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        // Register component-specific asset
        var splashBg = new AssetDefinition
        {
            Name = "SplashBackground",
            AssetPath = "textures/ui/company_logo.png",
            AssetType = typeof(Texture2D)
        };
        _resourceManager.RegisterDefinition(splashBg);
    }

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        // Mix of definition-based and string-based access
        var quadVAO = _resourceManager.GetOrCreateResource<uint>(Geometry.FullScreenQuad);
        var shader = _resourceManager.GetOrCreateResource<uint>(Shaders.BackgroundTexture);
        var texture = _resourceManager.GetOrCreateResource<uint>("SplashBackground"); // Component-scoped asset

        // Render splash screen...
    }
}
```### Benefits of This Enhanced Approach

1. **No Magic Strings**: Direct resource definition access eliminates string-based lookups
2. **Type Safety**: Compile-time checking ensures resource definitions exist
3. **IntelliSense Support**: Auto-completion shows available resources by category
4. **Refactoring Safety**: Resource renames are caught by the compiler
5. **Automatic Registration**: Definitions are registered automatically when first used
6. **Flexible Access Patterns**: Support both definition-based and string-based access
7. **Component Scoping**: Easy to create component-specific assets inline
8. **Performance**: No reflection needed at runtime for definition-based access
9. **Clean API**: `_resourceManager.GetOrCreateResource<uint>(Geometry.FullScreenQuad)` is much cleaner than `nameof()` calls

### Comparison of Access Patterns

```csharp
// OLD: String-based with nameof (clunky)
var quadVAO = _resourceManager.GetOrCreateResource<uint>(nameof(Geometry.FullScreenQuad));

// NEW: Direct definition access (clean and type-safe)
var quadVAO = _resourceManager.GetOrCreateResource<uint>(Geometry.FullScreenQuad);

// ALTERNATIVE: String-based for cached resources
var quadVAO = _resourceManager.GetSharedResourceID<uint>("SomeExistingResource");

// DYNAMIC: Create definitions on the fly
var customAsset = new AssetDefinition { Name = "Custom", AssetPath = "path.png", AssetType = typeof(Texture2D) };
var texture = _resourceManager.GetOrCreateResource<uint>(customAsset);
```

## Memory Management and Resource Lifecycle

### Overview

The resource management system needs comprehensive memory management capabilities to handle resource-constrained scenarios and provide fine-grained control over resource lifetime. This includes purging capabilities, sticky resource marking, and priority-based cleanup.

### Core Memory Management Features

#### 1. Component-Based Resource Scoping

Resources are either scoped to a specific component or unscoped (persistent):

```csharp
public interface IResourceDefinition
{
    string Name { get; }
    ResourceType Type { get; }
    IRuntimeComponent? Scope { get; }  // null = persistent, non-null = tied to component lifecycle
    uint CreateResource(GL gl, IAssetService assetService);
}
```

#### 2. Reference-Counted Dependency Tracking

Resources track which components are using them for intelligent cleanup:

```csharp
internal class ResourceEntry
{
    public object Resource { get; set; }
    public IResourceDefinition Definition { get; set; }
    public HashSet<IRuntimeComponent> UsingComponents { get; set; } = new();  // Components that reference this resource
    public DateTime LastAccessed { get; set; }
    public DateTime Created { get; set; }
    public long MemorySize { get; set; }  // Estimated memory usage

    /// <summary>
    /// Whether this resource can be purged when no components reference it.
    /// </summary>
    public bool CanBePurged => !Definition.IsPersistent && UsingComponents.Count == 0;
}
```

#### 3. Component Dependency Management

Track resource dependencies automatically through component references:

```csharp
internal class ResourceEntry
{
    public object Resource { get; set; }
    public IResourceDefinition Definition { get; set; }
    public HashSet<IRuntimeComponent> UsingComponents { get; set; } = new();  // Components that reference this resource
    public DateTime LastAccessed { get; set; }
    public DateTime Created { get; set; }
    public long MemorySize { get; set; }  // Estimated memory usage

    /// <summary>
    /// Whether this resource can be purged when no components reference it.
    /// </summary>
    public bool CanBePurged => !Definition.IsPersistent && UsingComponents.Count == 0;
}
```

### Extended IResourceManager Interface

Add memory management methods:

```csharp
public interface IResourceManager
{
    // Existing methods...

    /// <summary>
    /// Purges all resources scoped to a specific component.
    /// Called automatically when components are disposed.
    /// Returns number of resources purged and memory freed (estimated).
    /// </summary>
    (int resourcesFreed, long memoryFreed) PurgeComponentResources(IRuntimeComponent component);

    /// <summary>
    /// Purges all component-scoped resources where the component is disposed or no longer valid.
    /// Useful for cleaning up orphaned component resources.
    /// </summary>
    (int resourcesFreed, long memoryFreed) PurgeOrphanedResources();

    /// <summary>
    /// Emergency purge of all component-scoped resources under memory pressure.
    /// Persistent (unscoped) resources are never purged.
    /// </summary>
    (int resourcesFreed, long memoryFreed) PurgeAllComponentResources();

    /// <summary>
    /// Associates a resource with a component for lifecycle management.
    /// The resource will be automatically cleaned up when the component is disposed.
    /// </summary>
    void SetResourceScope(string resourceName, IRuntimeComponent? scopeComponent);

    /// <summary>
    /// Increments reference count for a resource (for manual lifetime management).
    /// </summary>
    void AddReference(string resourceName);

    /// <summary>
    /// Decrements reference count for a resource.
    /// </summary>
    void RemoveReference(string resourceName);

    /// <summary>
    /// Gets current memory usage statistics.
    /// </summary>
    ResourceMemoryStats GetMemoryStats();

    /// <summary>
    /// Sets the maximum cache size. When exceeded, automatic purging begins.
    /// </summary>
    void SetMaxCacheSize(long maxBytes);

    /// <summary>
    /// Gets detailed information about all cached resources.
    /// </summary>
    IEnumerable<ResourceInfo> GetResourceInfo();
}

public record ResourceMemoryStats(
    int TotalResources,
    int PersistentResources,
    int ComponentScopedResources,
    long EstimatedMemoryUsage,
    long MaxCacheSize,
    DateTime LastPurge
);

public record ResourceInfo(
    string Name,
    ResourceType Type,
    IRuntimeComponent? ScopeComponent,
    int ReferenceCount,
    DateTime LastAccessed,
    long EstimatedSize
);
```

### Resource Definition Updates

Update resource definitions to support scope-based memory management:

```csharp
[SharedResource("FullScreenQuad", ResourceType.Geometry, Priority = 10, Scope = ResourceScope.Global)]
public static GeometryDefinition FullScreenQuad => new()
{
    Name = "FullScreenQuad",
    Vertices = new float[] { /* ... */ },
    Indices = new uint[] { /* ... */ },
    Scope = ResourceScope.Global
};

// Application-specific resources are purged when switching applications/games
[SharedResource("GameSpecificTexture", ResourceType.Texture, Scope = ResourceScope.Application)]
public static AssetDefinition GameSpecificTexture => new()
{
    Name = "GameSpecificTexture",
    AssetPath = "textures/game/specific_texture.png",
    AssetType = typeof(Texture2D),
    Scope = ResourceScope.Application
};

// Component-specific resources are purged when components are disposed
public static AssetDefinition CreateComponentTexture(string componentId) => new()
{
    Name = $"ComponentTexture_{componentId}",
    AssetPath = "textures/components/dynamic.png",
    AssetType = typeof(Texture2D),
    Scope = ResourceScope.Component
};
```

### Automatic Memory Management

Implement automatic purging based on memory pressure and component lifecycle:

```csharp
public class AttributeBasedResourceManager : IResourceManager
{
    private long _maxCacheSize = 256 * 1024 * 1024; // 256MB default
    private readonly Timer _purgeTimer;

    public AttributeBasedResourceManager(/* ... */)
    {
        // Set up automatic purging every 30 seconds
        _purgeTimer = new Timer(AutoPurgeCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void AutoPurgeCallback(object state)
    {
        var stats = GetMemoryStats();

        // If we're over 80% of max cache size, start purging component-scoped resources
        if (stats.EstimatedMemoryUsage > _maxCacheSize * 0.8)
        {
            // First, purge orphaned component resources (components that no longer exist)
            var (freed1, memory1) = PurgeOrphanedResources();

            // If still over threshold, purge all component-scoped resources
            if (GetMemoryStats().EstimatedMemoryUsage > _maxCacheSize * 0.8)
            {
                var (freed2, memory2) = PurgeAllComponentResources();
                _logger.LogWarning("Memory pressure: purged {Total} component resources, freed {Memory} bytes",
                    freed1 + freed2, memory1 + memory2);
            }
            else if (freed1 > 0)
            {
                _logger.LogInformation("Auto-purged {Total} orphaned resources, freed {Memory} bytes",
                    freed1, memory1);
            }
        }
    }

    // Automatically called when components are disposed
    public void OnComponentDisposed(IRuntimeComponent component)
    {
        PurgeComponentResources(component);
    }
}
```

### Usage Patterns

#### Component-Scoped vs Persistent Resources

```csharp
// Persistent resources (Scope = null) - shared, never purged
public static class CoreAssets
{
    [SharedResource("FullScreenQuad", ResourceType.Geometry)]
    public static GeometryDefinition FullScreenQuad => new()
    {
        Name = "FullScreenQuad",
        Scope = null,  // Persistent - used by many components
        /* ... geometry data ... */
    };
}

// Component-scoped resources - automatically cleaned up when component is disposed
public class BackgroundLayer : RuntimeComponent
{
    private AssetDefinition CreateBackgroundAsset(string imagePath)
    {
        return new AssetDefinition
        {
            Name = $"Background_{Id}",
            Scope = this,  // Scoped to this component
            AssetPath = imagePath,
            AssetType = typeof(Texture2D)
        };
    }
}
```

#### Component Lifecycle Management

```csharp
public class BackgroundLayer : RuntimeComponent, IRenderable
{
    private string _backgroundTextureName;

    public override void OnConfigure(BackgroundLayerTemplate template)
    {
        // Create component-scoped resource
        if (!string.IsNullOrEmpty(template.ImageAsset?.Id))
        {
            var textureDefinition = new AssetDefinition
            {
                Name = $"Background_{Id}",
                Scope = this,  // Automatically cleaned up when this component is disposed
                AssetPath = template.ImageAsset.Id,
                AssetType = typeof(Texture2D)
            };

            var texture = _resourceManager.GetOrCreateResource<uint>(textureDefinition, this);
            _backgroundTextureName = textureDefinition.Name;
        }
    }

    protected override void OnDispose()
    {
        // Component disposal automatically triggers cleanup of component-scoped resources
        // No manual cleanup needed - the resource manager handles it via component lifecycle events
    }
}
```

#### Responding to Memory Pressure

```csharp
// In game main loop or memory management system
public void HandleMemoryPressure()
{
    var stats = _resourceManager.GetMemoryStats();

    if (stats.EstimatedMemoryUsage > _targetMemoryLimit)
    {
        // Progressive purging based on severity
        if (stats.EstimatedMemoryUsage > _criticalMemoryLimit)
        {
            // Emergency: purge all component-scoped resources
            _resourceManager.PurgeAllComponentResources();
        }
        else
        {
            // Standard: purge only orphaned component resources first
            _resourceManager.PurgeOrphanedResources();
        }
    }
}

// Clear resources when switching game states/levels
public void OnLevelChange()
{
    // Clear all component resources from the previous level
    _resourceManager.PurgeAllComponentResources();

    // Persistent (unscoped) resources remain loaded for reuse
}
```

### Integration with Dependency Injection

Register services in Services.cs:

```csharp
services.AddSingleton<ResourceRegistry>();
services.AddSingleton<IResourceManager, AttributeBasedResourceManager>();
````

### Migration Strategy

1. Create attribute classes and resource definition types
2. Implement ResourceRegistry with reflection-based discovery
3. Create AttributeBasedResourceManager with IResourceManager interface
4. **Update IRenderer interface**: Remove GetSharedResource<T> and SetSharedResource<T> methods
5. **Update Renderer implementation**: Remove shared resource dictionary and methods
6. Move existing resource definitions to static classes with attributes
7. Update components to inject IResourceManager instead of using IRenderer for resources
8. Remove duplicate creation code from existing extensions
9. Add resource preloading to application startup

### Interface Cleanup Benefits

By moving resource management out of IRenderer:

1. **Single Responsibility**: IRenderer focuses purely on rendering orchestration
2. **Cleaner Dependencies**: Components can inject IResourceManager without needing full IRenderer
3. **Better Testing**: Resource management can be mocked independently of renderer
4. **Reduced Coupling**: Resource creation logic separated from rendering logic
5. **Performance**: Dedicated resource manager can optimize caching strategies
6. **Maintainability**: Resource-related code centralized in one place

### Validation and Error Handling

- Duplicate resource names detected at startup
- Missing dependencies resolved through priority ordering
- Invalid resource definitions logged as warnings
- Runtime creation failures gracefully handled with fallbacks

## Memory Management Summary

The resource management system now includes a simple and elegant component-based scoping approach:

### Core Concept

- **Component-Scoped Resources**: `Scope = component` - automatically cleaned up when the component is disposed
- **Persistent Resources**: `Scope = null` - never purged, shared across components

### Key Benefits

- **Simplicity**: Only two categories - component-scoped or persistent
- **Automatic Management**: Component disposal automatically triggers resource cleanup
- **No Arbitrary Levels**: No confusing priority systems or sticky flags
- **Natural Lifecycle**: Resource lifetime matches component lifetime exactly
- **Memory Efficiency**: Unused component resources are automatically freed

### How It Works

1. **Persistent Resources** (Scope = null): Core engine assets, shared geometry, common shaders
2. **Component Resources** (Scope = component): Background textures, component-specific materials
3. **Automatic Cleanup**: When a component is disposed, all its scoped resources are purged
4. **Memory Pressure**: Under memory pressure, purge orphaned or all component resources
5. **Shared Resources**: Multiple components can reference the same persistent resource

### Usage Patterns

- Static resource declarations default to persistent (Scope = null)
- Components create scoped resources by setting Scope = this
- Resource manager automatically tracks component lifecycle
- No manual cleanup needed - disposal handles everything

## Next Steps for Implementation

1. Create resource declaration static classes (StandardResources, GameAssets)
2. **Implement attribute classes (SharedResourceAttribute with memory properties, ResourceType enum, ResourcePriority enum)**
3. **Create resource definition types (IResourceDefinition with Priority/IsSticky, GeometryDefinition, ShaderDefinition, AssetDefinition)**
4. **Implement ResourceRegistry with reflection-based discovery and memory management tracking**
5. **Create IResourceManager interface and AttributeBasedResourceManager implementation with full memory management**
6. Update IRenderer interface to remove GetSharedResource/SetSharedResource methods
7. Update Renderer class to remove shared resource management code
8. Register ResourceRegistry and IResourceManager in DI container
9. Define the BackgroundLayer Template record with all properties
10. Implement BackgroundLayer OnConfigure method to apply template to runtime
11. Implement basic IRenderable methods using IResourceManager
12. Create BackgroundLayer OnRender implementation for solid colors first
13. Add texture-based rendering with asset loading
14. Add procedural generation support
15. Implement material effects (tint, saturation, fade)
16. Add comprehensive validation
17. Create unit tests covering all material types and effects
18. **Add memory management unit tests for purging, reference counting, and automatic cleanup**
