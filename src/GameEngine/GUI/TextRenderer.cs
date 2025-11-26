using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data.Binding;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.PushConstants;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI;

public partial class TextRenderer : RuntimeComponent, IDrawable
{
    private readonly IPipelineManager _pipelineManager;
    private readonly IDescriptorManager _descriptorManager;
    private readonly IResourceManager _resourceManager;

    [ComponentProperty]
    [TemplateProperty]
    protected string _text = string.Empty;
    partial void OnTextChanged(string? oldValue) => UpdateSize();

    [ComponentProperty]
    [TemplateProperty]
    protected FontDefinition? _fontDefinition;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector4D<float> _color = new Vector4D<float>(0, 0, 0, 1);

    [ComponentProperty]
    [TemplateProperty]
    protected bool _visible = true;

    private FontResource? _fontResource;
    private DescriptorSet? _fontAtlasDescriptorSet;
    private PipelineHandle _pipeline;

    public TextRenderer(IPipelineManager pipelineManager, IDescriptorManager descriptorManager, IResourceManager resourceManager)
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

        // Load font
        var def = _fontDefinition ?? FontDefinitions.RobotoNormal;
        _fontResource = _resourceManager.Fonts.GetOrCreate(def);

        CreateFontAtlasDescriptorSet();
        UpdateSize();
    }

    protected override void OnDeactivate()
    {
        if (_fontResource != null && _fontDefinition != null)
        {
            _resourceManager.Fonts.Release(_fontDefinition);
            _fontResource = null;
        }
        
        _fontAtlasDescriptorSet = null;

        base.OnDeactivate();
    }

    private void CreateFontAtlasDescriptorSet()
    {
        if (_fontResource == null) return;

        var shader = ShaderDefinitions.UIElement;
        if (shader.DescriptorSetLayouts == null || !shader.DescriptorSetLayouts.ContainsKey(1))
            return;

        var layout = _descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[1]);
        _fontAtlasDescriptorSet = _descriptorManager.AllocateDescriptorSet(layout);

        _descriptorManager.UpdateDescriptorSet(
            _fontAtlasDescriptorSet.Value,
            _fontResource.AtlasTexture.ImageView,
            _fontResource.AtlasTexture.Sampler,
            ImageLayout.ShaderReadOnlyOptimal,
            binding: 0);
    }

    private void UpdateSize()
    {
        // In the future, we might want to update the parent's size if it's set to AutoSize
        // For now, we just render.
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (!IsVisible() || _fontResource == null || _fontResource.SharedGeometry == null || !_fontAtlasDescriptorSet.HasValue || string.IsNullOrEmpty(_text))
            yield break;

        var parentUI = Parent as UserInterfaceElement;
        
        var parentWorldMatrix = parentUI?.WorldMatrix ?? Matrix4X4<float>.Identity;
        var parentPivot = parentUI?.Pivot ?? Vector2D<float>.Zero;
        
        // Measure text
        var textSize = MeasureText(_text);
        
        // Calculate text block offset based on Pivot (relative to parent's origin)
        float textOffsetX = -parentPivot.X * textSize.X;
        float textOffsetY = -parentPivot.Y * textSize.Y;
        
        float cursorX = 0f;
        float baselineY = _fontResource.Ascender;
        
        foreach (char c in _text)
        {
            if (!_fontResource.Glyphs.TryGetValue(c, out var glyph))
                continue;

            if (glyph.Width == 0 || glyph.Height == 0)
            {
                cursorX += glyph.Advance;
                continue;
            }
            
            // Glyph center in LOCAL space
            float halfWidth = glyph.Width / 2.0f;
            float halfHeight = glyph.Height / 2.0f;
            
            float glyphLocalX = textOffsetX + cursorX + glyph.BearingX + halfWidth;
            float glyphLocalY = textOffsetY + baselineY - glyph.BearingY + halfHeight;
            
            // Create local translation matrix for the glyph
            var glyphLocalMatrix = Matrix4X4.CreateTranslation(glyphLocalX, glyphLocalY, 0);
            
            // Combine with parent world matrix
            // This allows the text to rotate/scale with the parent
            var glyphWorldMatrix = glyphLocalMatrix * parentWorldMatrix;
            
            var glyphSize = new Vector2D<int>(glyph.Width, glyph.Height);
            
            yield return new DrawCommand
            {
                RenderMask = RenderPasses.UI,
                Pipeline = _pipeline,
                VertexBuffer = _fontResource.SharedGeometry.Buffer,
                FirstVertex = (uint)(glyph.CharIndex * 4),
                VertexCount = 4,
                InstanceCount = 1,
                RenderPriority = 1000,
                PushConstants = new UIElementPushConstants(glyphWorldMatrix, _color, glyphSize, new Vector2D<float>(0.5f, 0.5f)),
                DescriptorSet = _fontAtlasDescriptorSet.Value
            };
            
            cursorX += glyph.Advance;
        }
    }

    public Vector2D<int> MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text) || _fontResource == null)
            return new Vector2D<int>(0, 0);

        float totalWidth = 0f;
        foreach (char c in text)
        {
            if (_fontResource.Glyphs.TryGetValue(c, out var glyph))
            {
                totalWidth += glyph.Advance;
            }
        }

        return new Vector2D<int>((int)totalWidth, _fontResource.LineHeight);
    }

    /// <summary>
    /// Gets the bounding box of a specific character/glyph in screen coordinates.
    /// </summary>
    /// <param name="index">The character index in the text string (0-based).</param>
    /// <returns>Bounding box (X, Y, Width, Height) in screen space, or null if index is out of range.</returns>
    public Rectangle<int>? MeasureGlyph(int index)
    {
        if (string.IsNullOrEmpty(_text) || index < 0 || index >= _text.Length)
            return null;

        if (_fontResource == null)
            return null;

        var parentUI = Parent as UserInterfaceElement;
        var parentWorldMatrix = parentUI?.WorldMatrix ?? Matrix4X4<float>.Identity;
        var parentPivot = parentUI?.Pivot ?? Vector2D<float>.Zero;
        var parentPosition = parentUI?.Position ?? Vector2D<float>.Zero;
        var parentSize = parentUI?.Size ?? Vector2D<float>.Zero;
        var parentScale = parentUI?.Scale ?? Vector2D<float>.One;

        // Measure total text size to calculate anchor offset
        var textSize = MeasureText(_text);

        // Calculate text block offset based on Pivot (relative to parent's origin)
        float textOffsetX = -parentPivot.X * textSize.X;
        float textOffsetY = -parentPivot.Y * textSize.Y;

        // Calculate cursor position up to the target index
        float cursorX = 0f;
        float baselineY = _fontResource.Ascender;

        for (int i = 0; i <= index; i++)
        {
            char c = _text[i];
            if (!_fontResource.Glyphs.TryGetValue(c, out var glyph))
                continue;

            if (i == index)
            {
                // Found target glyph - calculate its bounds in local space
                float glyphLocalX = textOffsetX + cursorX + glyph.BearingX;
                float glyphLocalY = textOffsetY + baselineY - glyph.BearingY;

                // Transform to screen space
                // Note: This is a simplified transform assuming no rotation for the bounds rect
                // For full correctness with rotation, we'd need to transform all 4 corners
                
                // Calculate element's top-left corner in screen space (approximate)
                float elementOriginX = parentPosition.X - (parentPivot.X * parentSize.X * 0.5f * parentScale.X);
                float elementOriginY = parentPosition.Y - (parentPivot.Y * parentSize.Y * 0.5f * parentScale.Y);
                
                float screenX = elementOriginX + glyphLocalX * parentScale.X;
                float screenY = elementOriginY + glyphLocalY * parentScale.Y;

                return new((int)screenX, (int)screenY, (int)(glyph.Width * parentScale.X), (int)(glyph.Height * parentScale.Y));
            }

            cursorX += glyph.Advance;
        }

        return null;
    }
}
