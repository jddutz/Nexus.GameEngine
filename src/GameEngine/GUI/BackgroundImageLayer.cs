using Nexus.GameEngine.Graphics.PushConstants;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Runtime.Extensions;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Full-screen background with an image texture.
/// Supports various placement modes (fill, fit, stretch, etc.).
/// Uses texture resources and descriptor sets.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class BackgroundImageLayer()
    : Component, IDrawable
{
    private IGeometryResource? _geometry;
    
    // Texture resources
    private TextureResource? _texture;
    private DescriptorSet? _textureDescriptorSet;
    
    [ComponentProperty]
    [TemplateProperty]
    protected TextureDefinition _textureDefinition = new()
    {
        Name = "DefaultTexture",
        Source = new ArgbArrayTextureSource(1, 1, [new Vector4D<float>(1, 1, 1, 1)])
    };
    
    [ComponentProperty]
    [TemplateProperty]
    protected int _placement = BackgroundImagePlacement.FillCenter;

    [ComponentProperty]
    [TemplateProperty]
    protected bool _visible = true;

    public bool IsVisible() => IsValid() && IsActive() && Visible;

    public PipelineHandle Pipeline =>
        Graphics.GetPipeline(PipelineDefinitions.ImageTexture);

    protected override void OnActivate()
    {
        base.OnActivate();

        // Load texture with specified definition
        _texture = Resources.GetTexture(_textureDefinition);
        
        // Get or create descriptor set layout
        var shader = ShaderDefinitions.ImageTexture;
        if (shader.DescriptorSetLayouts == null || !shader.DescriptorSetLayouts.ContainsKey(0))
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout for set 0");
        }

        var layout = Graphics.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[0]);
        _textureDescriptorSet = Graphics.AllocateDescriptorSet(layout);

        // Update descriptor set with texture
        Graphics.UpdateDescriptorSet(
            _textureDescriptorSet.Value,
            _texture.ImageView,
            _texture.Sampler,
            ImageLayout.ShaderReadOnlyOptimal,
            binding: 0);
        
        // Create textured quad geometry
        _geometry = Resources.GetGeometry(GeometryDefinitions.TexturedQuad);
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_textureDescriptorSet.HasValue || _texture == null)
            yield break;

        // Calculate UV bounds based on placement mode and viewport size
        var pushConstants = CalculateImageTexturePushConstants(context);

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Main,
            Pipeline = Pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            PushConstants = pushConstants,
            DescriptorSet = _textureDescriptorSet.Value
        };
    }

    /// <summary>
    /// Calculates push constants for UV bounds based on placement mode.
    /// </summary>
    private ImageTexturePushConstants CalculateImageTexturePushConstants(RenderContext context)
    {
        if (_texture == null)
            return default;
        
        // Get viewport dimensions from extent
        var extent = context.Viewport.Extent;
        var viewportWidth = (float)extent.Width;
        var viewportHeight = (float)extent.Height;
        
        // Calculate UV bounds based on placement mode
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            _placement,
            _texture.Width,
            _texture.Height,
            viewportWidth,
            viewportHeight);
        
        var pushConstants = ImageTexturePushConstants.FromUVBounds(uvMin, uvMax);
        
        return pushConstants;
    }

    protected override void OnDeactivate()
    {
        // Clean up texture resources
        if (_texture != null && _textureDefinition != null)
        {
            Resources.ReleaseTexture(_textureDefinition);
            _texture = null;
            
        }
        
        if (_textureDescriptorSet.HasValue)
        {
            _textureDescriptorSet = null;
        }
        
        if (_geometry != null)
        {
            Resources.ReleaseGeometry(GeometryDefinitions.TexturedQuad);
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
