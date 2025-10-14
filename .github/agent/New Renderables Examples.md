# Example: Creating New Renderables After Refactoring

## Template for New Renderables

After the refactoring, creating a new renderable component follows a simple declarative pattern:

```csharp
using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Example renderable component - YOUR_DESCRIPTION_HERE
/// </summary>
public partial class YourRenderable : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    // Template for configuration
    public new record Template : RuntimeComponent.Template
    {
        // Add your configuration properties here
        public bool IsVisible { get; set; } = true;
        public Vector4D<float> Color { get; set; } = new(1f, 1f, 1f, 1f);
    }

    // Component properties with deferred updates
    [ComponentProperty]
    private bool _isVisible = true;

    [ComponentProperty(Duration = AnimationDuration.Normal, Interpolation = InterpolationMode.Linear)]
    private Vector4D<float> _color = new(1f, 1f, 1f, 1f);

    // Constructor - inject IResourceManager
    public YourRenderable(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    }

    // IRenderable implementation
    public uint RenderPriority => 100;
    public Box3D<float> BoundingBox => Box3D<float>.Empty; // Or calculate actual bounds
    public uint RenderPassFlags => 1; // Participate in default render pass

    // Declare what to render (declarative, no GL code!)
    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        if (!IsVisible)
            yield break;

        // Get or create resources (cached automatically by ResourceManager)
        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.YourGeometry);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.YourShader);

        // Return render request with all needed information
        yield return new DrawCommand
        {
            Vao = geometry.VaoId,
            Shader = shader.ProgramId,
            IndexCount = geometry.IndexCount,
            PrimitiveType = geometry.PrimitiveType,
            IndexType = geometry.IndexType,
            Priority = RenderPriority,
            SourceViewport = vp,
            Uniforms = new Dictionary<string, object>
            {
                ["color"] = Color,
                // Add more uniforms as needed
            }
        };
    }

    // Configuration
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            _isVisible = template.IsVisible;
            _color = template.Color;
        }
    }

    // Public API for visibility
    public void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    // Public API for color
    public void SetColor(Vector4D<float> color)
    {
        Color = color;
    }
}
```

**That's it!** ~60 lines total, most of it boilerplate. No OpenGL knowledge required.

---

## Example 1: Simple Colored Quad (HelloQuad Refactored)

```csharp
public partial class HelloQuad : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    public new record Template : RuntimeComponent.Template
    {
        public bool IsVisible { get; set; } = true;
        public Vector4D<float> BackgroundColor { get; set; } = new(0.39f, 0.58f, 0.93f, 1.0f);
    }

    [ComponentProperty]
    private bool _isVisible = true;

    [ComponentProperty(Duration = AnimationDuration.Normal, Interpolation = InterpolationMode.CubicEaseInOut)]
    private Vector4D<float> _backgroundColor = new(0.39f, 0.58f, 0.93f, 1.0f);

    public HelloQuad(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public uint RenderPriority => 0;
    public Box3D<float> BoundingBox => Box3D<float>.Empty;
    public uint RenderPassFlags => 1;

    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        if (!IsVisible)
            yield break;

        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.BasicQuad);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.BasicQuad);

        yield return new DrawCommand
        {
            Vao = geometry.VaoId,
            Shader = shader.ProgramId,
            IndexCount = geometry.IndexCount,
            PrimitiveType = geometry.PrimitiveType,
            Priority = RenderPriority,
            Uniforms = new() { ["backgroundColor"] = BackgroundColor }
        };
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        if (componentTemplate is Template template)
        {
            _isVisible = template.IsVisible;
            _backgroundColor = template.BackgroundColor;
        }
    }

    public void SetVisible(bool visible) => IsVisible = visible;
    public void SetBackgroundColor(Vector4D<float> color) => BackgroundColor = color;
}
```

**Total: ~50 lines** (down from ~150)
**GL code: 0 lines** (down from ~100)

---

## Example 2: Textured Sprite

```csharp
public partial class Sprite : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    public new record Template : RuntimeComponent.Template
    {
        public required string TexturePath { get; init; }
        public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
        public Vector2D<float> Size { get; set; } = new(100f, 100f);
        public float Rotation { get; set; } = 0f;
        public Vector4D<float> Tint { get; set; } = new(1f, 1f, 1f, 1f);
    }

    [ComponentProperty]
    private string _texturePath = "";

    [ComponentProperty(Duration = AnimationDuration.Fast)]
    private Vector2D<float> _position = Vector2D<float>.Zero;

    [ComponentProperty(Duration = AnimationDuration.Fast)]
    private Vector2D<float> _size = new(100f, 100f);

    [ComponentProperty(Duration = AnimationDuration.Normal, Interpolation = InterpolationMode.Linear)]
    private float _rotation = 0f;

    [ComponentProperty(Duration = AnimationDuration.Normal)]
    private Vector4D<float> _tint = new(1f, 1f, 1f, 1f);

    public Sprite(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public uint RenderPriority => 100; // Render after background
    public Box3D<float> BoundingBox => CalculateBoundingBox();
    public uint RenderPassFlags => 1;

    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        // Get resources
        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.TexturedQuad);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.SpriteShader);
        var texture = _resourceManager.GetOrCreateTexture(TextureDefinitions.FromFile(_texturePath));

        // Calculate model matrix
        var modelMatrix = CalculateModelMatrix();

        yield return new DrawCommand
        {
            Vao = geometry.VaoId,
            Shader = shader.ProgramId,
            IndexCount = geometry.IndexCount,
            PrimitiveType = geometry.PrimitiveType,
            Priority = RenderPriority,
            Uniforms = new()
            {
                ["modelMatrix"] = modelMatrix,
                ["diffuseTexture"] = (int)texture.TextureUnit,
                ["tint"] = Tint
            }
        };
    }

    private Matrix4X4<float> CalculateModelMatrix()
    {
        // Transform math
        var translation = Matrix4X4.CreateTranslation(new Vector3D<float>(_position.X, _position.Y, 0));
        var rotation = Matrix4X4.CreateRotationZ(_rotation);
        var scale = Matrix4X4.CreateScale(new Vector3D<float>(_size.X, _size.Y, 1f));
        return scale * rotation * translation;
    }

    private Box3D<float> CalculateBoundingBox()
    {
        var halfSize = _size / 2f;
        return new Box3D<float>(
            new Vector3D<float>(_position.X - halfSize.X, _position.Y - halfSize.Y, 0),
            new Vector3D<float>(_position.X + halfSize.X, _position.Y + halfSize.Y, 0)
        );
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        if (componentTemplate is Template template)
        {
            _texturePath = template.TexturePath;
            _position = template.Position;
            _size = template.Size;
            _rotation = template.Rotation;
            _tint = template.Tint;
        }
    }
}
```

**Complexity:** Medium (has transforms)
**GL code:** 0 lines
**Focus:** Game logic, not rendering internals

---

## Example 3: Particle System

```csharp
public partial class ParticleSystem : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;
    private readonly List<Particle> _particles = new();

    public new record Template : RuntimeComponent.Template
    {
        public Vector3D<float> EmitterPosition { get; set; } = Vector3D<float>.Zero;
        public int MaxParticles { get; set; } = 100;
        public float EmissionRate { get; set; } = 10f; // particles per second
    }

    // ... particle fields ...

    public ParticleSystem(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public uint RenderPriority => 300; // Render after opaque objects
    public Box3D<float> BoundingBox => CalculateParticleBounds();
    public uint RenderPassFlags => 1;

    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.ParticleQuad);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.ParticleShader);
        var texture = _resourceManager.GetOrCreateTexture(TextureDefinitions.Particle);

        // Emit one render call per particle (could be instanced for performance)
        foreach (var particle in _particles.Where(p => p.IsAlive))
        {
            yield return new DrawCommand
            {
                Vao = geometry.VaoId,
                Shader = shader.ProgramId,
                IndexCount = geometry.IndexCount,
                PrimitiveType = geometry.PrimitiveType,
                Priority = RenderPriority,
                Uniforms = new()
                {
                    ["modelMatrix"] = particle.Transform,
                    ["color"] = particle.Color,
                    ["particleTexture"] = (int)texture.TextureUnit
                }
            };
        }
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Update particle physics
        foreach (var particle in _particles)
        {
            particle.Update(deltaTime);
        }

        // Emit new particles
        EmitParticles(deltaTime);
    }

    private void EmitParticles(double deltaTime) { /* ... */ }
    private Box3D<float> CalculateParticleBounds() { /* ... */ }
}
```

**Complexity:** High (game logic)
**GL code:** 0 lines
**Rendering:** Still simple and declarative

---

## Example 4: 3D Mesh (Multiple Materials)

```csharp
public partial class MeshRenderer : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    public new record Template : RuntimeComponent.Template
    {
        public required string MeshPath { get; init; }
        public List<string> MaterialPaths { get; init; } = new();
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
        public Vector3D<float> Rotation { get; set; } = Vector3D<float>.Zero;
        public Vector3D<float> Scale { get; set; } = Vector3D<float>.One;
    }

    // ... component properties ...

    public MeshRenderer(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public uint RenderPriority => 200;
    public Box3D<float> BoundingBox => _meshBounds;
    public uint RenderPassFlags => 0b0011; // Render in passes 0 and 1

    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        // Load mesh (could have multiple sub-meshes)
        var meshData = AssetLoader.LoadMesh(_meshPath);

        // Render each sub-mesh with its material
        for (int i = 0; i < meshData.SubMeshes.Count; i++)
        {
            var subMesh = meshData.SubMeshes[i];
            var materialPath = i < _materialPaths.Count ? _materialPaths[i] : "default.mat";

            var geometry = _resourceManager.GetOrCreateGeometry(subMesh.GeometryDefinition);
            var material = LoadMaterial(materialPath);

            yield return new DrawCommand
            {
                Vao = geometry.VaoId,
                Shader = material.Shader.ProgramId,
                IndexCount = geometry.IndexCount,
                PrimitiveType = geometry.PrimitiveType,
                Priority = RenderPriority,
                Uniforms = new()
                {
                    ["modelMatrix"] = CalculateModelMatrix(),
                    ["viewMatrix"] = vp.Camera.ViewMatrix,
                    ["projectionMatrix"] = vp.Camera.ProjectionMatrix,
                    ["diffuseTexture"] = (int)material.DiffuseTexture.TextureUnit,
                    ["normalTexture"] = (int)material.NormalTexture.TextureUnit,
                    ["material.ambient"] = material.Ambient,
                    ["material.diffuse"] = material.Diffuse,
                    ["material.specular"] = material.Specular,
                    ["material.shininess"] = material.Shininess
                }
            };
        }
    }

    private MaterialData LoadMaterial(string path)
    {
        // Load material definition and get resources
        var matDef = AssetLoader.LoadMaterialDefinition(path);
        return new MaterialData
        {
            Shader = _resourceManager.GetOrCreateShader(matDef.ShaderDefinition),
            DiffuseTexture = _resourceManager.GetOrCreateTexture(matDef.DiffuseTexture),
            NormalTexture = _resourceManager.GetOrCreateTexture(matDef.NormalTexture),
            // ... other material properties
        };
    }
}
```

**Complexity:** High (3D rendering, materials)
**GL code:** 0 lines
**Focus:** Scene graph and material system, not GL details

---

## Example 5: UI Button (With Hover State)

```csharp
public partial class Button : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;
    private bool _isHovered = false;

    public new record Template : RuntimeComponent.Template
    {
        public required string Text { get; init; }
        public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
        public Vector2D<float> Size { get; set; } = new(200f, 50f);
        public Vector4D<float> NormalColor { get; set; } = new(0.2f, 0.2f, 0.8f, 1f);
        public Vector4D<float> HoverColor { get; set; } = new(0.3f, 0.3f, 1f, 1f);
    }

    // ... component properties ...

    public Button(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public uint RenderPriority => 400; // UI renders last
    public Box3D<float> BoundingBox => Box3D<float>.Empty; // Never cull UI
    public uint RenderPassFlags => 1;

    public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
    {
        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.UIQuad);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.UIShader);

        // Render button background
        yield return new DrawCommand
        {
            Vao = geometry.VaoId,
            Shader = shader.ProgramId,
            IndexCount = geometry.IndexCount,
            Priority = RenderPriority,
            Uniforms = new()
            {
                ["position"] = _position,
                ["size"] = _size,
                ["color"] = _isHovered ? _hoverColor : _normalColor,
                ["screenSize"] = new Vector2D<float>(vp.Width, vp.Height)
            }
        };

        // Render button text (separate draw call)
        var textGeometry = GenerateTextGeometry(_text);
        var textShader = _resourceManager.GetOrCreateShader(ShaderDefinitions.TextShader);
        var fontTexture = _resourceManager.GetOrCreateTexture(TextureDefinitions.Font);

        yield return new DrawCommand
        {
            Vao = textGeometry.VaoId,
            Shader = textShader.ProgramId,
            IndexCount = textGeometry.IndexCount,
            Priority = RenderPriority + 1, // Text on top of button
            Uniforms = new()
            {
                ["position"] = _position + new Vector2D<float>(10, 10), // Text offset
                ["fontTexture"] = (int)fontTexture.TextureUnit,
                ["textColor"] = new Vector4D<float>(1f, 1f, 1f, 1f)
            }
        };
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Check hover state (would use input system)
        _isHovered = IsMouseOver();
    }

    private bool IsMouseOver() { /* ... */ return false; }
    private GeometryResource GenerateTextGeometry(string text) { /* ... */ throw new NotImplementedException(); }
}
```

**Complexity:** Medium (UI logic, text rendering)
**GL code:** 0 lines
**Multiple draw calls:** Easy to express

---

## Key Takeaways

### Pattern for All Renderables

1. **Inject IResourceManager** in constructor
2. **Get resources** using `GetOrCreateGeometry/Shader/Texture`
3. **Return DrawCommand** with resource IDs and uniforms
4. **No GL code** - focus on game logic

### Benefits

- ✅ **Simple:** ~50 lines for basic renderables
- ✅ **Consistent:** Same pattern for all types
- ✅ **Testable:** Mock IResourceManager
- ✅ **Maintainable:** No GL knowledge needed
- ✅ **Extensible:** Easy to add features
- ✅ **Performant:** Automatic resource caching

### What You Don't Need to Know

- ❌ OpenGL buffer creation
- ❌ Shader compilation
- ❌ Vertex attribute setup
- ❌ Resource lifecycle management
- ❌ GL state management

### What You Focus On

- ✅ Game logic (positions, transforms, physics)
- ✅ Which resources to use
- ✅ What uniforms to set
- ✅ Render priority
- ✅ Bounding boxes for culling

---

## Comparison Matrix

| Aspect                | Before Refactoring                    | After Refactoring          |
| --------------------- | ------------------------------------- | -------------------------- |
| **Lines of code**     | ~150                                  | ~50                        |
| **GL API calls**      | 20+                                   | 0                          |
| **Resource creation** | Manual, inline                        | Automatic, cached          |
| **Testability**       | Requires GL context                   | Mock ResourceManager       |
| **Time to implement** | 2 hours                               | 15 minutes                 |
| **Error handling**    | Manual                                | Handled by ResourceManager |
| **Code duplication**  | High (each component repeats GL code) | None (declarative)         |
| **Maintenance**       | Hard (GL expertise needed)            | Easy (just data)           |

---

## Next Renderable You'll Create

With the refactored architecture, creating your next renderable is as simple as:

1. Copy the template
2. Define your resources (geometry, shader, texture)
3. Fill in the uniforms
4. Done!

**No OpenGL knowledge required. Just declare what you want to render.**
