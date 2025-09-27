using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// A UI component that displays text.
/// </summary>
public class TextElement : RuntimeComponent, IRenderable
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
        set
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
        set
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
        set
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
        set
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
        set
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
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                NotifyPropertyChanged();
            }
        }
    }

    public bool ShouldRender => IsVisible;
    public int RenderPriority => 450; // UI text layer

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
            Text = template.Text;
            Color = template.Color;
            FontSize = template.FontSize;
            FontName = template.FontName;
            Alignment = template.Alignment;
            IsVisible = template.IsVisible;
        }
    }

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        // TODO: Implement text rendering using direct GL calls
    }
}