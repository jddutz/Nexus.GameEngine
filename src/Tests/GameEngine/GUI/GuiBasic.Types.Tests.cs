using System;
using System.Linq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources.Fonts;
using Silk.NET.Maths;
using Xunit;

namespace Tests.GameEngine.GUI;

public class GuiBasicTypesTests
{
    [Fact]
    public void Padding_StaticFactories_Work()
    {
        var zero = Padding.Zero;
        Assert.Equal(new Padding(0,0,0,0), zero);

        var all = Padding.All(5);
        Assert.Equal(new Padding(5,5,5,5), all);

        var horiz = Padding.Horizontal(3);
        Assert.Equal(new Padding(3,0,3,0), horiz);

        var vert = Padding.Vertical(7);
        Assert.Equal(new Padding(0,7,0,7), vert);

        // Constructors
        Assert.Equal(new Padding(), Padding.Zero);
        Assert.Equal(new Padding(4), new Padding(4,4,4,4));
        Assert.Equal(new Padding(2,6), new Padding(2,6,2,6));
    }

    [Fact]
    public void Margin_StaticFactories_Work()
    {
        var zero = Margin.Zero;
        Assert.Equal(new Margin(0,0,0,0), zero);

        var all = Margin.All(1.5f);
        Assert.Equal(new Margin(1.5f,1.5f,1.5f,1.5f), all);

        var horiz = Margin.Horizontal(2f);
        Assert.Equal(new Margin(2f,0f,2f,0f), horiz);

        var vert = Margin.Vertical(4f);
        Assert.Equal(new Margin(0f,4f,0f,4f), vert);

        Assert.Equal(new Margin(3f), new Margin(3f,3f,3f,3f));
        Assert.Equal(new Margin(2f,5f), new Margin(2f,5f,2f,5f));
    }

    [Fact]
    public void Thickness_StaticFactories_Work()
    {
        var zero = Thickness.Zero;
        Assert.Equal(new Thickness(0,0,0,0), zero);

        var all = Thickness.All(2f);
        Assert.Equal(new Thickness(2f,2f,2f,2f), all);

        var horiz = Thickness.Horizontal(1f);
        Assert.Equal(new Thickness(1f,0f,1f,0f), horiz);

        var vert = Thickness.Vertical(6f);
        Assert.Equal(new Thickness(0f,6f,0f,6f), vert);

        Assert.Equal(new Thickness(), Thickness.Zero);
        Assert.Equal(new Thickness(3f), new Thickness(3f,3f,3f,3f));
        Assert.Equal(new Thickness(2f,4f), new Thickness(2f,4f,2f,4f));
    }

    [Fact]
    public void SafeArea_CalculatesMargins_And_Clamps()
    {
        var sa = new SafeArea(0.1f, 0.1f, minPixels: 10, maxPixels: 50);
        var margins = sa.CalculateMargins(new Vector2D<int>(200, 100));

        // 200 * 0.1 = 20 -> between 10 and 50
        Assert.Equal(20, margins.Left);
        // 100 * 0.1 = 10 -> equals minPixels
        Assert.Equal(10, margins.Top);
        Assert.Equal(20, margins.Right);
        Assert.Equal(10, margins.Bottom);

        // Zero safe area yields zero margins when min/max are zero
        var zero = SafeArea.Zero;
        var margins2 = new SafeArea(0,0,0,0,0,0).CalculateMargins(new Vector2D<int>(100,100));
        Assert.Equal(Padding.Zero, margins2);
    }

    [Fact]
    public void GradientStop_Validate_RejectsInvalidValues()
    {
        var valid = new GradientStop(0.5f, new Vector4D<float>(1,0,0,1));
        // Should not throw
        valid.Validate();

        var invalidPos = new GradientStop(float.NaN, new Vector4D<float>(1,1,1,1));
        Assert.Throws<ArgumentException>(() => invalidPos.Validate());

        var invalidColor = new GradientStop(0.2f, new Vector4D<float>(float.NaN, 0,0,1));
        Assert.Throws<ArgumentException>(() => invalidColor.Validate());
    }

    [Fact]
    public void GradientDefinition_TwoAndThreeColor_ValidateAndBounds()
    {
        var g2 = GradientDefinition.TwoColor(new Vector4D<float>(0,0,0,1), new Vector4D<float>(1,1,1,1));
        Assert.Equal(2, g2.Stops.Length);
        g2.Validate();

        var g3 = GradientDefinition.ThreeColor(new Vector4D<float>(0,0,0,1), new Vector4D<float>(0.5f,0.5f,0.5f,1), new Vector4D<float>(1,1,1,1), 0.25f);
        Assert.Equal(3, g3.Stops.Length);
        g3.Validate();

        // Too few stops
        var bad = new GradientDefinition { Stops = new GradientStop[] { new GradientStop(0, new Vector4D<float>(0,0,0,1)) } };
        Assert.Throws<ArgumentException>(() => bad.Validate());

        // Too many stops
        var many = Enumerable.Range(0, GradientDefinition.MaxStops + 1)
            .Select(i => new GradientStop(i / (float)(GradientDefinition.MaxStops + 1), new Vector4D<float>(0,0,0,1)))
            .ToArray();
        var gd = new GradientDefinition { Stops = many };
        Assert.Throws<ArgumentException>(() => gd.Validate());
    }

    [Fact]
    public void TextStyle_FactoryMethods_SetProperties()
    {
        var font = new FontDefinition();
        var tsDefault = TextStyle.Default(font);
        Assert.Equal(font, tsDefault.Font);
        Assert.Equal(TextAlignment.Left, tsDefault.Alignment);
        Assert.Equal(new Vector4D<float>(1,1,1,1), tsDefault.Color);

        var color = new Vector4D<float>(0.2f, 0.3f, 0.4f, 1f);
        var tsColor = TextStyle.WithColor(font, color);
        Assert.Equal(color, tsColor.Color);

        var tsAlign = TextStyle.WithAlignment(font, TextAlignment.Center);
        Assert.Equal(TextAlignment.Center, tsAlign.Alignment);
    }
}
