using Silk.NET.Maths;

using Microsoft.Extensions.Logging;

using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Component Template
/// </summary>
public record BorderTemplate
{
}

/// <summary>
/// Runtime border component that implements event-driven rendering behavior.
/// Templates configure the visual properties, runtime components subscribe to events and implement behavior.
/// </summary>
public class Border()
    : RuntimeComponent
{
    private BorderStyle _style = BorderStyle.Rectangle;
    private Vector4D<float> _backgroundColor = Vector4D<float>.Zero; // Transparent (0,0,0,0)
    private Vector4D<float> _borderColor = new(0, 0, 0, 1); // Black (0,0,0,1)
    private Thickness _borderThickness = new(0);
    private float _cornerRadius = 0f;
    private string? _backgroundImage;
    private string? _borderImage;
    private bool _isVisible = true;
    private float _opacity = 1.0f;

    /// <summary>
    /// The rendering style for this border.
    /// </summary>
    public BorderStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// The background fill color. Use Vector4D.Transparent for no background.
    /// </summary>
    public Vector4D<float> BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// The color of the border outline.
    /// </summary>
    public Vector4D<float> BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor != value)
            {
                _borderColor = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// The thickness of the border on each side.
    /// </summary>
    public Thickness BorderThickness
    {
        get => _borderThickness;
        set
        {
            if (_borderThickness != value)
            {
                _borderThickness = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// The radius for rounded corners (when Style is RoundedRect).
    /// </summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            if (_cornerRadius != value)
            {
                _cornerRadius = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// Path to the background image texture (when Style is Image).
    /// </summary>
    public string? BackgroundImage
    {
        get => _backgroundImage;
        set
        {
            if (_backgroundImage != value)
            {
                _backgroundImage = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// Path to the border image texture (when Style is NinePatch).
    /// </summary>
    public string? BorderImage
    {
        get => _borderImage;
        set
        {
            if (_borderImage != value)
            {
                _borderImage = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// Whether this component should be rendered.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// Opacity (0.0 = transparent, 1.0 = opaque).
    /// </summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            var clampedValue = Math.Clamp(value, 0.0f, 1.0f);
            if (_opacity != clampedValue)
            {
                _opacity = clampedValue;
                OnRenderPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// Event fired when render properties change.
    /// </summary>
    public event EventHandler? RenderPropertiesChanged;

    /// <summary>
    /// Activate phase - Subscribe to required events.
    /// No interfaces needed - components self-register for events they care about.
    /// </summary>
    protected override void OnActivate()
    {
        // Border components are rendered through the IRenderable interface
        // when the renderer walks the component tree
    }

    /// <summary>
    /// Render event handler - Implements actual rendering behavior.
    /// Called automatically when UserInterfaceManager fires render events.
    /// </summary>
    private void OnRender(object sender, EventArgs e)
    {
        if (!IsVisible || Opacity <= 0.0f)
            return;

        switch (Style)
        {
            case BorderStyle.Rectangle:
                RenderRectangle();
                break;

            case BorderStyle.RoundedRect:
                RenderRoundedRectangle();
                break;

            case BorderStyle.Image:
                RenderImage();
                break;

            case BorderStyle.NinePatch:
                RenderNinePatch();
                break;
        }
    }

    private void RenderRectangle()
    {
        // TODO: Implement rectangle rendering using IGraphicsRenderContext.DrawRectangle
        // This will require:
        // 1. Get component bounds from Transform
        // 2. Draw background rectangle if BackgroundColor != Transparent
        // 3. Draw border rectangles if BorderThickness > 0

        // Placeholder implementation
        Logger?.LogDebug($"Rendering Rectangle Border: Background={BackgroundColor}, Border={BorderColor}, Thickness={BorderThickness}");
    }

    private void RenderRoundedRectangle()
    {
        // TODO: Future implementation for rounded corners
        Logger?.LogDebug($"Rendering RoundedRect Border (not implemented): CornerRadius={CornerRadius}");
    }

    private void RenderImage()
    {
        // TODO: Future implementation for image backgrounds
        Logger?.LogDebug($"Rendering Image Border (not implemented): Image={BackgroundImage}");
    }

    private void RenderNinePatch()
    {
        // TODO: Future implementation for ninepatch borders
        Logger?.LogDebug($"Rendering NinePatch Border (not implemented): Image={BorderImage}");
    }

    private void OnRenderPropertiesChanged()
    {
        RenderPropertiesChanged?.Invoke(this, EventArgs.Empty);
    }
}
