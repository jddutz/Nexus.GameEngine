using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data.Binding;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.PushConstants;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Textures;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Components;

public partial class SpriteRenderer : Component, IDrawable
{
    private readonly IPipelineManager _pipelineManager;
    private readonly IDescriptorManager _descriptorManager;
    private readonly IResourceManager _resourceManager;

    [ComponentProperty]
    [TemplateProperty]
    protected TextureResource? _texture;
    partial void OnTextureChanged(TextureResource? oldValue) => UpdateDescriptorSet();

    [ComponentProperty]
    [TemplateProperty]
    protected Vector4D<float> _color = new Vector4D<float>(1, 1, 1, 1);

    [ComponentProperty]
    [TemplateProperty]
    protected bool _visible = true;

    private DescriptorSet? _descriptorSet;
    private PipelineHandle _pipeline;
    private IGeometryResource? _geometry;

    public SpriteRenderer(IPipelineManager pipelineManager, IDescriptorManager descriptorManager, IResourceManager resourceManager)
    {
        _pipelineManager = pipelineManager;
        _descriptorManager = descriptorManager;
        _resourceManager = resourceManager;
    }

    public bool IsVisible() => Visible && IsActive();

    protected override void OnActivate()
    {
        base.OnActivate();
        
        _pipeline = _pipelineManager.GetOrCreate(PipelineDefinitions.UIElement);
        _geometry = _resourceManager.Geometry.GetOrCreate(GeometryDefinitions.TexturedQuad);

        UpdateDescriptorSet();
    }

    protected override void OnDeactivate()
    {
        if (_geometry != null)
        {
            _resourceManager.Geometry.Release(GeometryDefinitions.TexturedQuad);
            _geometry = null;
        }
        
        _descriptorSet = null;

        base.OnDeactivate();
    }

    private void UpdateDescriptorSet()
    {
        if (_texture == null) return;

        var shader = ShaderDefinitions.UIElement;
        if (shader.DescriptorSetLayouts == null || !shader.DescriptorSetLayouts.ContainsKey(1))
        {
             // Fallback or error handling
             return;
        }

        var layout = _descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[1]);
        _descriptorSet = _descriptorManager.AllocateDescriptorSet(layout);

        _descriptorManager.UpdateDescriptorSet(
            _descriptorSet.Value,
            _texture.ImageView,
            _texture.Sampler,
            ImageLayout.ShaderReadOnlyOptimal,
            binding: 0);
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (!IsVisible() || _texture == null || _geometry == null || !_descriptorSet.HasValue)
            yield break;

        IRectTransform? parentRect = null;
        var current = Parent;
        while (current != null)
        {
            if (current is IRectTransform rect)
            {
                parentRect = rect;
                break;
            }
            current = current.Parent;
        }

        var modelMatrix = parentRect?.WorldMatrix ?? Matrix4X4<float>.Identity;
        var size = parentRect?.Size ?? Vector2D<float>.Zero;
        var pivot = parentRect?.Pivot ?? Vector2D<float>.Zero;

        var pushConstants = new UIElementPushConstants
        {
            Model = modelMatrix,
            TintColor = _color,
            UvRect = new Vector4D<float>(0, 0, 1, 1),
            Size = size,
            Pivot = pivot
        };

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.UI,
            Pipeline = _pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            PushConstants = pushConstants,
            DescriptorSet = _descriptorSet.Value
        };
    }
}
