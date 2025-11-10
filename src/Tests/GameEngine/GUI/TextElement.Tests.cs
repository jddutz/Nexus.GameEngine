using Moq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Tests.GameEngine.GUI;

public class TextElementTests
{
    [Fact]
    public void MeasureText_ReturnsZero_WhenNoFontOrEmpty()
    {
        var desc = new Mock<IDescriptorManager>();
        var res = new Mock<IResourceManager>();
        var pm = new Mock<IPipelineManager>();

        var text = new TextElement(desc.Object, res.Object, pm.Object);
        Assert.Equal(new Vector2D<int>(0,0), text.MeasureText("") );
        Assert.Equal(new Vector2D<int>(0,0), text.MeasureText("Hello") );
    }

    [Fact]
    public void MeasureText_And_MeasureGlyph_WithFontMetrics()
    {
        var desc = new Mock<IDescriptorManager>();
        var res = new Mock<IResourceManager>();
        var pm = new Mock<IPipelineManager>();

        // Create minimal texture resource required by FontResource
        var tex = new TextureResource(new Image(), new DeviceMemory(), new ImageView(), new Sampler(), 8, 8, Format.R8G8B8A8Unorm, "test");

        // Create simple glyph map
        var glyphs = new Dictionary<char, GlyphInfo>
        {
            ['A'] = new GlyphInfo { Character = 'A', CharIndex = 0, TexCoordMin = new Vector2D<float>(0,0), TexCoordMax = new Vector2D<float>(1,1), Width = 10, Height = 12, BearingX = 2, BearingY = 3, Advance = 8 }
        };

        var fontResource = new FontResource(tex, null, glyphs, lineHeight: 12, ascender: 8, descender: -4, fontSize: 16);

        var text = new TextElement(desc.Object, res.Object, pm.Object);
        text.SetFont(fontResource);
        text.SetText("A");
        text.ApplyUpdates(0.016);

        var measure = text.MeasureText("A");
        Assert.Equal(8, measure.X);
        Assert.Equal(12, measure.Y);

        // Position the element so elementOrigin == Position by using Size=0 and AnchorPoint top-left
        text.SetPosition(new Vector3D<float>(100f, 200f, 0f));
        text.SetSize(new Vector2D<int>(0,0));
        text.SetAnchorPoint(new Vector2D<float>(-1f, -1f));
        text.ApplyUpdates(0.016);

        var glyphBounds = text.MeasureGlyph(0);
        Assert.NotNull(glyphBounds);
        Assert.Equal(102, glyphBounds.Value.Origin.X); // 100 + BearingX(2)
        Assert.Equal(205, glyphBounds.Value.Origin.Y); // 200 + Ascender(8) - BearingY(3)
        Assert.Equal(10, glyphBounds.Value.Size.X);
        Assert.Equal(12, glyphBounds.Value.Size.Y);
    }
}
