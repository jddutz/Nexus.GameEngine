using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Shaders.Definitions;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Full-screen background with an image texture.
/// Supports various placement modes (fill, fit, stretch, etc.).
/// Uses texture resources and descriptor sets.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class ImageTextureBackground(
    IPipelineManager pipelineManager,
    IResourceManager resources,
    IDescriptorManager descriptorManager)
    : DrawableComponent, IDrawable
{
    /// <summary>
    /// Template for configuring ImageTextureBackground components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Texture definition to load.
        /// Required. Use a static texture definition from your resource definitions class (e.g., TestResources.UvGridTexture).
        /// </summary>
        public required Resources.Textures.ITextureDefinition TextureDefinition { get; set; }
        
        /// <summary>
        /// Image placement mode.
        /// Use BackgroundImagePlacement constants (e.g., FillCenter, FitCenter, Stretch).
        /// Default: FillCenter
        /// </summary>
        public int Placement { get; set; } = BackgroundImagePlacement.FillCenter;
    }

    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;
    private int _drawCallCount = 0;
    
    // Texture resources
    private Resources.Textures.TextureResource? _texture;
    private DescriptorSet? _textureDescriptorSet;
    
    private Resources.Textures.ITextureDefinition? _textureDefinition;
    private int _placement = BackgroundImagePlacement.FillCenter;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            _textureDefinition = template.TextureDefinition;
            _placement = template.Placement;
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        if (_textureDefinition == null)
        {
            throw new InvalidOperationException("ImageTextureBackground requires a TextureDefinition to be set");
        }

        try
        {
            // Build pipeline for image texture rendering
            _pipeline = pipelineManager.GetBuilder()
                .WithShader(new ImageTextureShader())
                .WithRenderPasses(RenderPasses.Main)
                .WithTopology(PrimitiveTopology.TriangleStrip)
                .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                .WithDepthTest()
                .WithDepthWrite()
                .Build("ImageTextureBackground_Pipeline");


            // Load texture with specified definition
            _texture = resources.Textures.GetOrCreate(_textureDefinition);
            
            // Create descriptor set for texture
            var shader = new ImageTextureShader();
            if (shader.DescriptorSetLayoutBindings == null || shader.DescriptorSetLayoutBindings.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Shader {shader.Name} does not define descriptor set layout bindings");
            }
            
            var layout = descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayoutBindings);
            _textureDescriptorSet = descriptorManager.AllocateDescriptorSet(layout);
            
            // Update descriptor set with texture
            descriptorManager.UpdateDescriptorSet(
                _textureDescriptorSet.Value,
                _texture.ImageView,
                _texture.Sampler,
                ImageLayout.ShaderReadOnlyOptimal,
                binding: 0);
            
            // Create textured quad geometry
            _geometry = resources.Geometry.GetOrCreate(new TexturedQuad());
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_textureDescriptorSet.HasValue || _texture == null)
            yield break;

        // Calculate UV bounds based on placement mode and viewport size
        var pushConstants = CalculateImageTexturePushConstants(context);

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Main,
            Pipeline = _pipeline,
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
        
        // Get viewport dimensions
        var vulkanViewport = context.Viewport.VulkanViewport;
        var viewportWidth = vulkanViewport.Width;
        var viewportHeight = vulkanViewport.Height;
        
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
            resources.Textures.Release(_textureDefinition);
            _texture = null;
            
        }
        
        if (_textureDescriptorSet.HasValue)
        {
            _textureDescriptorSet = null;
        }
        
        if (_geometry != null)
        {
            resources.Geometry.Release(new TexturedQuad());
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
