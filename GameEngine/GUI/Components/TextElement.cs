using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// A UI component that displays text.
/// </summary>
public class TextElement : RuntimeComponent, IRenderable, ITextController
{
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// The text content to display.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The color of the text.
        /// </summary>
        public Vector4D<float> Color { get; set; } = new(1, 1, 1, 1); // White

        /// <summary>
        /// The font size in pixels.
        /// </summary>
        public float FontSize { get; set; } = 12f;

        /// <summary>
        /// The font family name.
        /// </summary>
        public string FontName { get; set; } = "DefaultFont";

        /// <summary>
        /// Text alignment within the component bounds.
        /// </summary>
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;

        /// <summary>
        /// Whether the text element should be rendered.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    private string? _text;
    private Vector4D<float> _color = new(1, 1, 1, 1); // White
    private float _fontSize = 12f;
    private string _fontName = "DefaultFont";
    private TextAlignment _alignment = TextAlignment.Left;
    private bool _isVisible = true;

    /// <summary>
    /// The text content to display.
    /// </summary>
    public string? Text
    {
        get => _text;
        private set
        {
            if (_text != value)
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The color of the text.
    /// </summary>
    public Vector4D<float> Color
    {
        get => _color;
        private set
        {
            if (_color != value)
            {
                _color = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The font size in pixels.
    /// </summary>
    public float FontSize
    {
        get => _fontSize;
        private set
        {
            if (_fontSize != value)
            {
                _fontSize = Math.Max(1f, value); // Ensure minimum size
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The font family name.
    /// </summary>
    public string FontName
    {
        get => _fontName;
        private set
        {
            if (_fontName != value)
            {
                _fontName = value ?? "DefaultFont";
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Text alignment within the component bounds.
    /// </summary>
    public TextAlignment Alignment
    {
        get => _alignment;
        private set
        {
            if (_alignment != value)
            {
                _alignment = value;
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether the text element should be rendered.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                NotifyPropertyChanged();
            }
        }
    }

    public bool ShouldRender => IsVisible;
    public uint RenderPriority => 1; // UI text layer

    /// <summary>
    /// Bounding box for text elements. Returns minimal box since these are UI elements.
    /// </summary>
    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    /// <summary>
    /// Text elements participate in UI render pass (pass 1).
    /// </summary>
    public uint RenderPassFlags => 1u << 1; // UI pass

    /// <summary>
    /// Text elements are leaf components and don't render children.
    /// </summary>
    public bool ShouldRenderChildren => false;

    /// <summary>
    /// Configure the text element using the provided template.
    /// </summary>
    /// <param name="componentTemplate">Template containing configuration data</param>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            _text = template.Text;
            _color = template.Color;
            _fontSize = Math.Max(1f, template.FontSize); // Ensure minimum size
            _fontName = template.FontName ?? "DefaultFont";
            _alignment = template.Alignment;
            _isVisible = template.IsVisible;
        }
    }

    // IRenderable.SetVisible implementation
    public void SetVisible(bool visible)
    {
        QueueUpdate(() => IsVisible = visible);
    }

    // ITextController implementations
    public void SetText(string? text)
    {
        QueueUpdate(() => Text = text);
    }

    public void SetColor(Vector4D<float> color)
    {
        QueueUpdate(() => Color = color);
    }

    public void SetColor(float r, float g, float b, float a = 1.0f)
    {
        QueueUpdate(() => Color = new Vector4D<float>(r, g, b, a));
    }

    public void SetColor(string colorName)
    {
        QueueUpdate(() =>
        {
            // Use reflection to get named colors from Colors class
            var colorProperty = typeof(Colors).GetProperty(colorName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (colorProperty?.GetValue(null) is Vector4D<float> namedColor)
            {
                Color = namedColor;
            }
            else
            {
                Logger?.LogWarning("Unknown color name: {ColorName}", colorName);
            }
        });
    }

    public void SetFontSize(float fontSize)
    {
        QueueUpdate(() => FontSize = fontSize);
    }

    public void SetFontName(string fontName)
    {
        QueueUpdate(() => FontName = fontName);
    }

    public void SetAlignment(TextAlignment alignment)
    {
        QueueUpdate(() => Alignment = alignment);
    }

    public void AnimateColor(Vector4D<float> targetColor, float factor)
    {
        QueueUpdate(() =>
        {
            var currentColor = Color;
            var interpolatedColor = new Vector4D<float>(
                currentColor.X + (targetColor.X - currentColor.X) * factor,
                currentColor.Y + (targetColor.Y - currentColor.Y) * factor,
                currentColor.Z + (targetColor.Z - currentColor.Z) * factor,
                currentColor.W + (targetColor.W - currentColor.W) * factor
            );
            Color = interpolatedColor;
        });
    }

    public void ScaleFontSize(float scaleFactor)
    {
        QueueUpdate(() => FontSize = FontSize * scaleFactor);
    }

    public IEnumerable<GLState> OnRender(IViewport viewport, double deltaTime)
    {
        var GLState = new GLState();

        // TODO: Implement text rendering by declaring render state requirements
        // For now, just return empty render state
        yield return GLState;
    }
}