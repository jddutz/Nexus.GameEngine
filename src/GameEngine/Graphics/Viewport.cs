using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Viewport manages a rendering region with an associated camera and content tree.
/// </summary>
public partial class Viewport(IWindowService windowService) : RuntimeComponent, IViewport
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

    // Cached Vulkan viewport and scissor - computed lazily on first access or after window resize
    private Silk.NET.Vulkan.Viewport? _vulkanViewport;
    private Rect2D? _vulkanScissor;
    private bool _vulkanStateNeedsUpdate = true;

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

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            Camera = template.Camera;
            Content = template.Content;
            X = template.X;
            Y = template.Y;
            Width = template.Width;
            Height = template.Height;
            
            // Compute Vulkan viewport and scissor from normalized coordinates
            UpdateVulkanViewportAndScissor();
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
        
        Logger?.LogDebug("Updated Vulkan viewport: ({X}, {Y}, {Width}x{Height})", 
            _vulkanViewport.Value.X, _vulkanViewport.Value.Y, _vulkanViewport.Value.Width, _vulkanViewport.Value.Height);
    }
}
