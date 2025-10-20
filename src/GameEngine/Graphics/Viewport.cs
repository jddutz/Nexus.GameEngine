using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Viewport manages a rendering region with an associated camera and content tree.
/// </summary>
public partial class Viewport(
    IWindowService windowService,
    IOptions<GraphicsSettings> settings)
    : RuntimeComponent, IViewport
{
    public new record Template : RuntimeComponent.Template
    {
        public ICamera? Camera { get; init; }
        public IRuntimeComponent? Content { get; init; }
        public float X { get; init; } = 0f;
        public float Y { get; init; } = 0f;
        public float Width { get; init; } = 1f;
        public float Height { get; init; } = 1f;
        public Vector4D<float> BackgroundColor = Colors.DarkSlateBlue;
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
    private Vector4D<float> _backgroundColor = settings.Value.BackgroundColor ?? Colors.DarkBlue;

    // Cached Vulkan viewport and scissor - computed lazily on first access or after window resize
    private Silk.NET.Vulkan.Viewport? _vulkanViewport;
    private Rect2D? _vulkanScissor;
    private bool _vulkanStateNeedsUpdate = true;
    
    // Cached clear color value - updated when BackgroundColor changes
    private ClearValue _clearColorValue;

    public ICamera? Camera
    {
        get => _camera;
        set
        {
            if (_camera != value)
            {
                _camera = value;
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

            }
        }
    }

    // Note: All properties (X, Y, Width, Height, BackgroundColor) are auto-generated from [ComponentProperty] attributes

    /// <summary>
    /// Gets the cached Vulkan clear color value.
    /// This is updated automatically when BackgroundColor changes (in OnUpdate phase).
    /// </summary>
    public ClearValue ClearColorValue => _clearColorValue;

    public Silk.NET.Vulkan.Viewport VulkanViewport
    {
        get
        {
            if (_vulkanStateNeedsUpdate || !_vulkanViewport.HasValue)
            {
                UpdateVulkanViewportAndScissor();
            }
            return _vulkanViewport!.Value;
        }
    }
    
    public Rect2D VulkanScissor
    {
        get
        {
            if (_vulkanStateNeedsUpdate || !_vulkanScissor.HasValue)
            {
                UpdateVulkanViewportAndScissor();
            }
            return _vulkanScissor!.Value;
        }
    }

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            Camera = template.Camera;
            Content = template.Content;
            SetX(template.X);
            SetY(template.Y);
            SetWidth(template.Width);
            SetHeight(template.Height);
            SetBackgroundColor(template.BackgroundColor);
            
            // Compute Vulkan viewport and scissor from normalized coordinates
            UpdateVulkanViewportAndScissor();
            
            // Initialize clear color value
            UpdateClearColorValue();
        }
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Ensure clear color value is initialized even if OnConfigure wasn't called with a template
        // This happens when ContentManager creates Viewport using componentFactory.Create<Viewport>()
        if (_clearColorValue.Color.Float32_0 == 0 && _clearColorValue.Color.Float32_1 == 0 &&
            _clearColorValue.Color.Float32_2 == 0 && _clearColorValue.Color.Float32_3 == 0)
        {
            UpdateClearColorValue();
        }
    }

    /// <summary>
    /// Computes Vulkan viewport and scissor rectangles from normalized coordinates.
    /// Should be called during OnConfigure and when window is resized.
    /// </summary>
    private void UpdateVulkanViewportAndScissor()
    {
        var window = windowService.GetWindow();
        var framebufferSize = window.FramebufferSize;
        
        // Convert normalized coordinates (0-1) to pixel coordinates
        var pixelX = X * framebufferSize.X;
        var pixelY = Y * framebufferSize.Y;
        var pixelWidth = Width * framebufferSize.X;
        var pixelHeight = Height * framebufferSize.Y;
        
        _vulkanViewport = new Silk.NET.Vulkan.Viewport
        {
            X = pixelX,
            Y = pixelY,
            Width = pixelWidth,
            Height = pixelHeight,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        
        _vulkanScissor = new Rect2D
        {
            Offset = new Offset2D((int)pixelX, (int)pixelY),
            Extent = new Extent2D((uint)pixelWidth, (uint)pixelHeight)
        };
        
        _vulkanStateNeedsUpdate = false;
        
    }
    
    /// <summary>
    /// Updates the cached clear color value from BackgroundColor.
    /// Called when BackgroundColor changes (via property change callback).
    /// </summary>
    private void UpdateClearColorValue()
    {
        _clearColorValue = new ClearValue
        {
            Color = new ClearColorValue
            {
                Float32_0 = BackgroundColor.X,
                Float32_1 = BackgroundColor.Y,
                Float32_2 = BackgroundColor.Z,
                Float32_3 = BackgroundColor.W
            }
        };
    }
    
    /// <summary>
    /// Property change callback - updates clear color when background color changes.
    /// This happens during the Update phase, so conversion is done once per change, not per frame.
    /// </summary>
    partial void OnBackgroundColorChanged(Vector4D<float> oldValue)
    {
        UpdateClearColorValue();
    }
}
