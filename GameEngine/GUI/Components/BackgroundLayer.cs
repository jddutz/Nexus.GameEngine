using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.Graphics.Resources;
using Nexus.GameEngine.Graphics.Textures;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.GUI.Components;

public class BackgroundLayer(IResourceManager resourceManager)
    : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

    /// <summary>
    /// Template for configuring BackgroundLayer components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// The type of material to render (SolidColor, ImageAsset, or ProceduralTexture).
        /// </summary>
        public MaterialType MaterialType { get; init; } = MaterialType.SolidColor;

        /// <summary>
        /// Base background color for solid backgrounds or tinting for textured backgrounds.
        /// </summary>
        public Vector4D<float> BackgroundColor { get; init; } = new(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Reference to the background image asset (used when MaterialType is ImageAsset).
        /// </summary>
        public AssetReference<Texture2D>? ImageAsset { get; init; }

        /// <summary>
        /// Parameters for procedural texture generation (used when MaterialType is ProceduralTexture).
        /// Format depends on the specific procedural shader being used.
        /// </summary>
        public object? ProceduralParameters { get; init; }

        /// <summary>
        /// Color tinting overlay applied to the background.
        /// </summary>
        public Vector4D<float> Tint { get; init; } = new(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// Saturation adjustment (0.0 = grayscale, 1.0 = normal, >1.0 = oversaturated).
        /// </summary>
        public float Saturation { get; init; } = 1.0f;

        /// <summary>
        /// Opacity/fade level (0.0 = transparent, 1.0 = opaque).
        /// </summary>
        public float Fade { get; init; } = 1.0f;

        /// <summary>
        /// How this layer blends with content behind it.
        /// </summary>
        public BlendMode BlendMode { get; init; } = BlendMode.Replace;

        /// <summary>
        /// How textures wrap at the edges (Repeat, Clamp, Mirror).
        /// </summary>
        public TextureWrapMode TextureWrapMode { get; init; } = TextureWrapMode.Clamp;

        /// <summary>
        /// UV scaling for tiling effects (1.0 = normal size, 2.0 = half size with tiling).
        /// </summary>
        public Vector2D<float> TextureScale { get; init; } = new(1.0f, 1.0f);

        /// <summary>
        /// UV offset for animation/positioning.
        /// </summary>
        public Vector2D<float> TextureOffset { get; init; } = new(0.0f, 0.0f);
    }

    // Configuration state
    private Template _template = new();
    private bool _isVisible = true;

    /// <summary>
    /// Indicates if this component should be rendered in the current frame.
    /// Background layers render when visible and have some opacity.
    /// </summary>
    public bool ShouldRender => _isVisible && _template.Fade > 0.0f;

    /// <summary>
    /// Render priority for sorting. Background layers render very early.
    /// </summary>
    public int RenderPriority => 0; // Background priority

    /// <summary>
    /// Whether this component is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    /// <summary>
    /// Bounding box for frustum culling. Full-screen quads should never be culled.
    /// </summary>
    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    /// <summary>
    /// Bit flags indicating which render passes this component participates in.
    /// Background layers participate in the main color pass.
    /// </summary>
    public uint RenderPassFlags => 0xFFFFFFFF; // All passes

    /// <summary>
    /// Indicates whether child components should be processed for rendering.
    /// Background layers typically don't have children to render.
    /// </summary>
    public bool ShouldRenderChildren => true;

    /// <summary>
    /// Gets the current material type.
    /// </summary>
    public MaterialType GetMaterialType() => _template.MaterialType;

    /// <summary>
    /// Gets the current background color.
    /// </summary>
    public Vector4D<float> GetBackgroundColor() => _template.BackgroundColor;

    /// <summary>
    /// Gets the current saturation value.
    /// </summary>
    public float GetSaturation() => _template.Saturation;

    /// <summary>
    /// Gets the current fade value.
    /// </summary>
    public float GetFade() => _template.Fade;

    /// <summary>
    /// Configures this component from a template.
    /// </summary>
    /// <param name="componentTemplate">The template containing configuration data</param>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate is Template bgTemplate)
        {
            _template = bgTemplate;
            // Clear validation cache since template changed
            ClearValidationResults();
            base.OnConfigure(componentTemplate);
        }
        else
        {
            throw new ArgumentException($"Expected {nameof(Template)}, got {componentTemplate?.GetType().Name ?? "null"}");
        }
    }

    /// <summary>
    /// Renders this background layer using OpenGL calls.
    /// </summary>
    /// <param name="renderer">Provides GL context and helper methods</param>
    /// <param name="deltaTime">Time since last frame for animations</param>
    public void OnRender(IRenderer renderer, double deltaTime)
    {
        Console.WriteLine($"[BackgroundLayer] OnRender called - ShouldRender: {ShouldRender}, MaterialType: {_template.MaterialType}");
        Console.WriteLine($"[BackgroundLayer] BackgroundColor: R={_template.BackgroundColor.X:F2}, G={_template.BackgroundColor.Y:F2}, B={_template.BackgroundColor.Z:F2}, A={_template.BackgroundColor.W:F2}");

        // Early exit if not visible or fully transparent
        if (!ShouldRender)
        {
            Console.WriteLine($"[BackgroundLayer] Skipping render - not visible or fully transparent");
            return;
        }

        // Handle null renderer gracefully
        if (renderer?.GL == null)
        {
            Console.WriteLine($"[BackgroundLayer] Skipping render - renderer or GL is null");
            return;
        }

        var gl = renderer.GL;

        switch (_template.MaterialType)
        {
            case MaterialType.SolidColor:
                Console.WriteLine($"[BackgroundLayer] Rendering solid color");
                RenderSolidColor(gl);
                break;

            case MaterialType.ImageAsset:
                Console.WriteLine($"[BackgroundLayer] Rendering image asset (fallback to solid color)");
                RenderImageAsset(gl);
                break;

            case MaterialType.ProceduralTexture:
                Console.WriteLine($"[BackgroundLayer] Rendering procedural texture (fallback to solid color)");
                RenderProceduralTexture(gl);
                break;
        }
    }

    private void RenderSolidColor(GL gl)
    {
        // For solid colors, we can simply clear the screen with the background color
        // This is much more efficient than rendering a fullscreen quad

        // Apply tint and fade to the background color
        var finalColor = new Vector4D<float>(
            _template.BackgroundColor.X * _template.Tint.X,
            _template.BackgroundColor.Y * _template.Tint.Y,
            _template.BackgroundColor.Z * _template.Tint.Z,
            _template.BackgroundColor.W * _template.Tint.W * _template.Fade
        );

        // Apply saturation adjustment
        if (_template.Saturation != 1.0f)
        {
            var gray = finalColor.X * 0.299f + finalColor.Y * 0.587f + finalColor.Z * 0.114f;
            finalColor = new Vector4D<float>(
                gray + (_template.Saturation * (finalColor.X - gray)),
                gray + (_template.Saturation * (finalColor.Y - gray)),
                gray + (_template.Saturation * (finalColor.Z - gray)),
                finalColor.W
            );
        }

        // Set the clear color and clear the screen
        gl.ClearColor(finalColor.X, finalColor.Y, finalColor.Z, finalColor.W);
        gl.Clear(ClearBufferMask.ColorBufferBit);

        Console.WriteLine($"[BackgroundLayer] Cleared screen with color: R={finalColor.X:F2}, G={finalColor.Y:F2}, B={finalColor.Z:F2}, A={finalColor.W:F2}");
    }

    private void RenderImageAsset(GL gl)
    {
        // TODO: Implement texture-based rendering
        // For now, fall back to solid color
        RenderSolidColor(gl);
    }

    private void RenderProceduralTexture(GL gl)
    {
        // TODO: Implement procedural generation
        // For now, fall back to solid color
        RenderSolidColor(gl);
    }

    private void SetupBlendMode(GL gl, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Replace:
                gl.Disable(EnableCap.Blend);
                break;
            case BlendMode.Alpha:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendMode.Additive:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case BlendMode.Multiply:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
        }
    }

    private void SetShaderUniforms(GL gl, uint shader)
    {
        // Set background color uniform
        var colorLocation = gl.GetUniformLocation(shader, "uBackgroundColor");
        if (colorLocation >= 0)
        {
            gl.Uniform4(colorLocation, _template.BackgroundColor.X, _template.BackgroundColor.Y, _template.BackgroundColor.Z, _template.BackgroundColor.W);
        }

        // Set tint uniform
        var tintLocation = gl.GetUniformLocation(shader, "uTint");
        if (tintLocation >= 0)
        {
            gl.Uniform4(tintLocation, _template.Tint.X, _template.Tint.Y, _template.Tint.Z, _template.Tint.W);
        }

        // Set saturation uniform
        var saturationLocation = gl.GetUniformLocation(shader, "uSaturation");
        if (saturationLocation >= 0)
        {
            gl.Uniform1(saturationLocation, _template.Saturation);
        }

        // Set fade uniform
        var fadeLocation = gl.GetUniformLocation(shader, "uFade");
        if (fadeLocation >= 0)
        {
            gl.Uniform1(fadeLocation, _template.Fade);
        }
    }

    /// <summary>
    /// Validates the current component configuration.
    /// </summary>
    /// <returns>Collection of validation errors, empty if valid</returns>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        var errors = new List<ValidationError>();

        // Validate saturation range
        if (_template.Saturation < 0.0f)
        {
            errors.Add(new ValidationError(this,
                $"Saturation must be non-negative, got {_template.Saturation}", ValidationSeverityEnum.Error));
        }

        // Validate fade range
        if (_template.Fade < 0.0f || _template.Fade > 1.0f)
        {
            errors.Add(new ValidationError(this,
                $"Fade must be between 0.0 and 1.0, got {_template.Fade}", ValidationSeverityEnum.Error));
        }

        // Validate ImageAsset material type requirements
        if (_template.MaterialType == MaterialType.ImageAsset && _template.ImageAsset == null)
        {
            errors.Add(new ValidationError(this,
                "ImageAsset material type requires a valid ImageAsset reference", ValidationSeverityEnum.Error));
        }

        return errors;
    }
}