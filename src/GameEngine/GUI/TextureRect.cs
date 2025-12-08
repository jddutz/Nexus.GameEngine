using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.PushConstants;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Runtime.Extensions;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// UI component that renders a textured rectangle in screen space.
/// Designed for backgrounds, panels, and other solid UI elements with optional textures.
/// </summary>
public partial class TextureRect : UserInterfaceElement, IDrawable
{
    protected TextureResource? _texture;
    private DescriptorSet? _descriptorSet;
    private PipelineHandle _pipeline;
    private IGeometryResource? _geometry;

    [TemplateProperty(Name = "Texture")]
    private void SetTexture(TextureDefinition textureDefinition)
    {
        _textureDefinition = textureDefinition;
    }
    private TextureDefinition? _textureDefinition;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector4D<float> _color = new Vector4D<float>(1, 1, 1, 1);

    public TextureRect()
    {
    }

    public bool IsVisible() => Visible && IsActive();

    protected override void OnActivate()
    {
        base.OnActivate();

        // Convert texture definition to resource
        if (_textureDefinition != null)
        {
            _texture = Resources.GetTexture(_textureDefinition);
        }

        _pipeline = Graphics.GetPipeline(PipelineDefinitions.UIElement);
        _geometry = Resources.GetGeometry(GeometryDefinitions.TexturedQuad);

        UpdateDescriptorSet();
    }

    protected override void OnDeactivate()
    {
        if (_geometry != null)
        {
            Resources.ReleaseGeometry(GeometryDefinitions.TexturedQuad);
            _geometry = null;
        }

        if (_texture != null && _textureDefinition != null)
        {
            Resources.ReleaseTexture(_textureDefinition);
            _texture = null;
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

        var layout = Graphics.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[1]);
        _descriptorSet = Graphics.AllocateDescriptorSet(layout);

        Graphics.UpdateDescriptorSet(
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

        var pushConstants = new UIElementPushConstants
        {
            Model = WorldMatrix,
            TintColor = _color,
            UvRect = new Vector4D<float>(0, 0, 1, 1),
            Size = Size,
            Pivot = Pivot
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
