using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Viewport manages a rendering region with an associated camera and content tree.
/// </summary>
public partial class Viewport : RuntimeComponent, IViewport
{
    public new record Template : RuntimeComponent.Template
    {
        public ICamera? Camera { get; init; }
        public IRuntimeComponent? Content { get; init; }
        public float X { get; init; } = 0f;
        public float Y { get; init; } = 0f;
        public float Width { get; init; } = 1f;
        public float Height { get; init; } = 1f;
    }

    private ICamera? _camera;
    private IRuntimeComponent? _content;

    [ComponentProperty]
    private float _x = 0f;

    [ComponentProperty]
    private float _y = 0f;

    [ComponentProperty]
    private float _width = 1f;

    [ComponentProperty]
    private float _height = 1f;

    [ComponentProperty]
    private Vector4D<float> _backgroundColor = new(0.0f, 0.0f, 0.2f, 1.0f); // Dark blue default

    public ICamera? Camera
    {
        get => _camera;
        set
        {
            if (_camera != value)
            {
                _camera = value;
                Logger?.LogDebug("Viewport camera changed to {CameraName}", value?.Name ?? "null");
            }
        }
    }

    public IRuntimeComponent? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                // Remove old content from children
                if (_content != null && Children.Contains(_content))
                {
                    RemoveChild(_content);
                }

                _content = value;

                // Add new content to children
                if (_content != null && !Children.Contains(_content))
                {
                    AddChild(_content);
                }

                Logger?.LogDebug("Viewport content changed to {ContentName}", value?.Name ?? "null");
            }
        }
    }

    // Note: All properties (X, Y, Width, Height, BackgroundColor) are auto-generated from [ComponentProperty] attributes

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            // TODO: Configure properties
        }
    }
}
