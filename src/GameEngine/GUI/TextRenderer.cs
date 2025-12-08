using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data.Binding;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.PushConstants;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Runtime.Extensions;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI;

public partial class TextRenderer : UserInterfaceElement, IDrawable
{
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

    private FontResource? _fontResource;
    private DescriptorSet? _fontAtlasDescriptorSet;
    private PipelineHandle _pipeline;

    public TextRenderer()
    {
    }

    public bool IsVisible() => Visible;

    protected override void OnActivate()
    {
        base.OnActivate();

        _pipeline = Graphics.GetPipeline(PipelineDefinitions.UIElement);

        // Load font
        var def = _fontDefinition ?? FontDefinitions.RobotoNormal;
        _fontResource = Resources.GetFont(def);

        CreateFontAtlasDescriptorSet();
        UpdateSize();
    }

    protected override void OnDeactivate()
    {
        if (_fontResource != null && _fontDefinition != null)
        {
            Resources.ReleaseFont(_fontDefinition);
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

        var layout = Graphics.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[1]);
        _fontAtlasDescriptorSet = Graphics.AllocateDescriptorSet(layout);

        Graphics.UpdateDescriptorSet(
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

        // Measure text to determine total size
        var textSize = MeasureText(_text);
        
        // Calculate text start position based on Pivot
        // Pivot (0,0) = top-left, Pivot (0.5,0.5) = center, Pivot (1,1) = bottom-right
        float textStartX = Pivot.X - Alignment.X * textSize.X;
        float textStartY = Pivot.Y - Alignment.Y * textSize.Y;
        
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
            
            // Calculate glyph position in local space (relative to TextRenderer's position)
            // Glyph metrics are relative to baseline/cursor position
            // Position at the top-left corner of the glyph (since quad is 0-1 with pivot at 0)
            float glyphLeft = textStartX + cursorX + glyph.BearingX;
            float glyphTop = textStartY + baselineY - glyph.BearingY;
            
            // Build glyph world matrix: rotation + translation only (NO scaling)
            // Glyphs should render at their actual pixel size from the font atlas
            var rotationMatrix = Matrix4X4.CreateRotationZ(Rotation);
            var translationMatrix = Matrix4X4.CreateTranslation(new Vector3D<float>(
                MathF.Floor(Position.X + glyphLeft),
                MathF.Floor(Position.Y + glyphTop),
                0.0f));
            
            var glyphWorldMatrix = rotationMatrix * translationMatrix;
            
            // The normalized quad is 1x1 (from 0 to 1), so we pass the full glyph size
            // because the shader does (inPos - pivot) * size, and inPos ranges over 1 unit
            // Pivot at 0 means top-left alignment for the quad
            var glyphSize = new Vector2D<float>(glyph.Width, glyph.Height);
            var glyphPivot = new Vector2D<float>(0f, 0f);
            
            yield return new DrawCommand
            {
                RenderMask = RenderPasses.UI,
                Pipeline = _pipeline,
                VertexBuffer = _fontResource.SharedGeometry.Buffer,
                FirstVertex = (uint)(glyph.CharIndex * 4),
                VertexCount = 4,
                InstanceCount = 1,
                RenderPriority = 1000,
                PushConstants = new UIElementPushConstants(glyphWorldMatrix, _color, glyphSize, glyphPivot),
                DescriptorSet = _fontAtlasDescriptorSet.Value
            };
            
            cursorX += glyph.Advance;
        }
    }

    public Vector2D<float> MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text) || _fontResource == null)
            return new Vector2D<float>(0, 0);

        float totalWidth = 0f;
        foreach (char c in text)
        {
            if (_fontResource.Glyphs.TryGetValue(c, out var glyph))
            {
                totalWidth += glyph.Advance;
            }
        }

        return new Vector2D<float>(totalWidth, _fontResource.LineHeight);
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

        // Measure total text size to calculate pivot offset
        var textSize = MeasureText(_text);

        // Calculate text block offset based on Pivot (relative to element's origin)
        float textStartX = -Pivot.X * textSize.X;
        float textStartY = -Pivot.Y * textSize.Y;

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
                float glyphLocalX = textStartX + cursorX + glyph.BearingX;
                float glyphLocalY = textStartY + baselineY - glyph.BearingY;

                // Transform to world space using this element's transform
                var glyphPos = new Vector2D<float>(glyphLocalX, glyphLocalY);
                
                // For simplicity, assume no rotation and just apply position + scale
                float worldX = Position.X + glyphPos.X * Scale.X;
                float worldY = Position.Y + glyphPos.Y * Scale.Y;

                return new((int)worldX, (int)worldY, (int)(glyph.Width * Scale.X), (int)(glyph.Height * Scale.Y));
            }

            cursorX += glyph.Advance;
        }

        return null;
    }
}
