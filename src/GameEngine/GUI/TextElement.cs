namespace Nexus.GameEngine.GUI;

/// <summary>
/// A UI component that displays text using font atlases with shared geometry.
/// Reuses UIElement shader with per-glyph model matrices for efficient text rendering.
/// Each character references the shared font geometry buffer at different offsets.
/// </summary>
public partial class TextElement : DrawableElement
{
    // Template and runtime properties
    [TemplateProperty]
    [ComponentProperty]
    private string _text = string.Empty;

    /// <summary>
    /// Font definition from template (private, not exposed).
    /// Used to create FontResource in OnActivate.
    /// Cached for re-creation if SetFont(FontDefinition) is called.
    /// Cleared when SetFont(FontResource) is called directly.
    /// </summary>
    [TemplateProperty(Name = "Font")]
    private FontDefinition? _fontDefinition = null;

    /// <summary>
    /// Font resource created from FontDefinition.
    /// Has ComponentProperty for runtime behavior but NOT TemplateProperty (font set via _fontDefinition in template).
    /// </summary>
    [ComponentProperty]
    private FontResource? _fontResource;

    private DescriptorSet? _fontAtlasDescriptorSet;

    /// <summary>
    /// Creates a new TextElement with the specified descriptor and graphics managers.
    /// </summary>
    public TextElement(IDescriptorManager descriptorManager, IResourceManager resourceManager, IPipelineManager pipelineManager)
        : base(descriptorManager, resourceManager, pipelineManager)
    {
        // Use default font if not specified
        _fontDefinition ??= FontDefinitions.RobotoNormal;
    }

    /// <summary>
    /// Property change callback for text updates.
    /// No geometry regeneration needed - just emit different DrawCommands.
    /// </summary>
    partial void OnTextChanged(string? oldValue)
    {
        if (IsActive())
        {
            // Calculate new size from text measurements
            UpdateSizeFromText();
        }
    }

    /// <summary>
    /// Sets font from a definition.
    /// Caches the definition and creates the FontResource via ResourceManager.
    /// </summary>
    public void SetFont(FontDefinition? definition)
    {
        if (ResourceManager == null) return;

        _fontDefinition = definition ?? FontDefinitions.RobotoNormal;
        var resource = ResourceManager.Fonts.GetOrCreate(_fontDefinition);
        SetFont(resource);
    }

    /// <summary>
    /// Sets font resource directly.
    /// Clears the cached definition since it's no longer relevant.
    /// </summary>
    public void SetFont(FontResource? resource)
    {
        _fontDefinition = null;  // Clear definition cache
        _fontResource = resource;
        
        // Update descriptor set if already activated
        if (IsActive() && _fontAtlasDescriptorSet.HasValue && _fontResource != null)
        {
            UpdateFontAtlasDescriptorSet();
        }
        
        // Recalculate size with new font
        if (IsActive())
        {
            UpdateSizeFromText();
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Convert font definition to resource if provided, or use default
        if (_fontDefinition != null && _fontResource == null)
        {
            SetFont(_fontDefinition);
        }
        else if (_fontResource == null)
        {
            // Default to RobotoNormal if no font specified
            SetFont(FontDefinitions.RobotoNormal);
        }

        // Create descriptor set for font atlas texture
        CreateFontAtlasDescriptorSet();

        // Calculate initial size from text
        UpdateSizeFromText();
    }

    /// <summary>
    /// Creates a descriptor set binding the font atlas texture.
    /// </summary>
    private void CreateFontAtlasDescriptorSet()
    {
        if (_fontResource == null) return;

        var shader = ShaderDefinitions.UIElement;
        if (shader.DescriptorSetLayouts == null || !shader.DescriptorSetLayouts.ContainsKey(1))
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout for set 1 (texture sampler)");
        }

        var layout = DescriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[1]);
        _fontAtlasDescriptorSet = DescriptorManager.AllocateDescriptorSet(layout);

        UpdateFontAtlasDescriptorSet();
    }

    /// <summary>
    /// Updates the descriptor set with the current font atlas texture.
    /// </summary>
    private void UpdateFontAtlasDescriptorSet()
    {
        if (!_fontAtlasDescriptorSet.HasValue || _fontResource == null) return;

        DescriptorManager.UpdateDescriptorSet(
            _fontAtlasDescriptorSet.Value,
            _fontResource.AtlasTexture.ImageView,
            _fontResource.AtlasTexture.Sampler,
            ImageLayout.ShaderReadOnlyOptimal,
            binding: 0);
    }

    /// <summary>
    /// Calculates and updates element Size based on text measurements.
    /// Width = sum of glyph advances, Height = font line height.
    /// </summary>
    private void UpdateSizeFromText()
    {
        if (_fontResource == null || string.IsNullOrEmpty(_text))
        {
            SetSize(new Vector2D<int>(0, 0));
            return;
        }

        float totalWidth = 0f;
        foreach (char c in _text)
        {
            if (_fontResource.Glyphs.TryGetValue(c, out var glyph))
            {
                totalWidth += glyph.Advance;
            }
        }

        SetSize(new Vector2D<int>((int)totalWidth, _fontResource.LineHeight));
    }

    /// <summary>
    /// Measures the size of the specified text when rendered with the current font.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The size (width, height) of the rendered text in pixels.</returns>
    public Vector2D<int> MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new Vector2D<int>(0, 0);

        if (_fontResource == null)
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

        // Calculate element's top-left corner in screen space
        float elementOriginX = Position.X - (AnchorPoint.X * Size.X * 0.5f * Scale.X);
        float elementOriginY = Position.Y - (AnchorPoint.Y * Size.Y * 0.5f * Scale.Y);

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
                // Found target glyph - calculate its bounds
                float glyphX = elementOriginX + cursorX + glyph.BearingX;
                float glyphY = elementOriginY + baselineY - glyph.BearingY;

                return new((int)glyphX, (int)glyphY, glyph.Width, glyph.Height);
            }

            cursorX += glyph.Advance;
        }

        return null;
    }

    /// <summary>
    /// Generates DrawCommands for text rendering.
    /// Each visible character gets its own DrawCommand referencing the shared font geometry
    /// at a specific offset (FirstVertex = glyph.CharIndex × 4).
    /// </summary>
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_fontResource == null || !_fontAtlasDescriptorSet.HasValue || _fontResource.SharedGeometry == null)
            yield break;

        if (string.IsNullOrEmpty(_text))
            yield break;

        // Use the pipeline from base Element class (UIElement shader)
        var pipeline = Pipeline;

        // Calculate starting position based on AnchorPoint
        float cursorX = 0f; // Start at element origin
        float baselineY = _fontResource.Ascender; // Baseline position

        int glyphIndex = 0;
        foreach (char c in _text)
        {
            if (!_fontResource.Glyphs.TryGetValue(c, out var glyph))
                continue;

            // Skip whitespace (they advance cursor but don't render)
            if (glyph.Width == 0 || glyph.Height == 0)
            {
                cursorX += glyph.Advance;
                continue;
            }

            // Calculate per-glyph world matrix
            var glyphWorldMatrix = CalculateGlyphWorldMatrix(glyph, cursorX, baselineY);

            yield return new DrawCommand
            {
                RenderMask = RenderPasses.UI,
                Pipeline = pipeline,
                VertexBuffer = _fontResource.SharedGeometry.Buffer,  // SHARED buffer!
                FirstVertex = (uint)(glyph.CharIndex * 4),  // Offset into shared buffer
                VertexCount = 4,
                InstanceCount = 1,
                RenderPriority = 1000, // UI text layer
                PushConstants = UIElementPushConstants.FromModelAndColor(glyphWorldMatrix, TintColor),
                DescriptorSet = _fontAtlasDescriptorSet.Value
            };

            cursorX += glyph.Advance;
            glyphIndex++;
        }
    }

    /// <summary>
    /// Calculates the world matrix for a single glyph.
    /// Combines:
    /// 1. Glyph-local scale (from normalized -1,1 quad to glyph pixel dimensions)
    /// 2. Glyph position within text (cursor + bearing)
    /// 3. Element's screen-space position (accounting for AnchorPoint)
    /// 
    /// Note: We don't use Element's WorldMatrix because that includes Size scaling,
    /// which would incorrectly scale glyphs by the total text dimensions.
    /// </summary>
    private Matrix4X4<float> CalculateGlyphWorldMatrix(GlyphInfo glyph, float cursorX, float baselineY)
    {
        // Glyph dimensions (half-extents for scaling from normalized -1,1 space)
        float scaleX = glyph.Width / 2.0f;
        float scaleY = glyph.Height / 2.0f;

        // Calculate element's top-left corner in screen space
        // Position is where AnchorPoint is located, so offset by anchor to get top-left
        float elementOriginX = Position.X - (AnchorPoint.X * Size.X * 0.5f * Scale.X);
        float elementOriginY = Position.Y - (AnchorPoint.Y * Size.Y * 0.5f * Scale.Y);

        // Glyph position in screen space (element origin + cursor + bearing + centering)
        float glyphCenterX = elementOriginX + cursorX + glyph.BearingX + scaleX;
        float glyphCenterY = elementOriginY + baselineY - glyph.BearingY + scaleY;

        // Create glyph world matrix: scale to glyph size, then translate to screen position
        return Matrix4X4.CreateScale(scaleX, scaleY, 1.0f) *
               Matrix4X4.CreateTranslation(glyphCenterX, glyphCenterY, Position.Z);
    }

    protected override void OnDeactivate()
    {
        // Release font resource
        if (_fontDefinition != null && _fontResource != null)
        {
            ResourceManager.Fonts.Release(_fontDefinition);
            _fontResource = null;
        }

        // Clear descriptor set (DescriptorManager handles cleanup)
        _fontAtlasDescriptorSet = null;

        base.OnDeactivate();
    }
}
