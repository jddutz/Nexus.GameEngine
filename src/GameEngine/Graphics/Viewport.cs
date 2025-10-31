namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Represents a rendering region within a window, managing normalized coordinates, camera, and content for Vulkan-based rendering.
/// <para>
/// The <see cref="Viewport"/> class encapsulates Vulkan viewport and scissor state, and provides integration points for camera selection and root content rendering.
/// It is designed for multi-viewport scenarios, supporting independent background colors, priorities, and activation state.
/// </para>
/// <para>
/// Normalized coordinates (0-1) allow the viewport to scale with window size. Vulkan state is automatically updated on property changes or window resize.
/// </para>
/// <para>
/// Usage: Assign a camera and content component, set normalized bounds, and add to the renderer's viewport collection.
/// </para>
/// </summary>
public class Viewport(ILogger logger, IWindow window)
{
    private bool _isValid = true;
    
    private Silk.NET.Vulkan.Viewport? _vulkanViewport;
    private Rect2D? _vulkanScissor;
    private bool _vulkanStateNeedsUpdate = true;
    private ClearValue _clearColorValue;
    private Vector4D<float> _backgroundColor;


    /// <summary>
    /// Gets or sets the camera used for rendering this viewport.
    /// The camera determines the view/projection matrices for rendering content.
    /// </summary>
    public ICamera? Camera { get; set; }

    /// <summary>
    /// Gets or sets the root <see cref="IComponent"/> to render in this viewport.
    /// This is typically a scene graph or UI root. Setting this property invalidates the viewport.
    /// </summary>
    private IComponent? _content;
    public IComponent? Content
    {
        get => _content;
        set
        {
            if (!Equals(_content, value))
            {
                _content = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the X position of the viewport (normalized 0-1).
    /// 0 = left edge of window, 1 = right edge.
    /// </summary>
    private float _x = 0f;
    public float X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the Y position of the viewport (normalized 0-1).
    /// 0 = top edge of window, 1 = bottom edge.
    /// </summary>
    private float _y = 0f;
    public float Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the width of the viewport (normalized 0-1).
    /// 1 = full window width. Must be > 0 and <= 1.
    /// </summary>
    private float _width = 1f;
    public float Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the height of the viewport (normalized 0-1).
    /// 1 = full window height. Must be > 0 and <= 1.
    /// </summary>
    private float _height = 1f;
    public float Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the background color for the viewport.
    /// This color is used to clear the framebuffer before rendering content.
    /// </summary>
    public Vector4D<float> BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!_backgroundColor.Equals(value))
            {
                _backgroundColor = value;
                UpdateClearColorValue();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the render priority for this viewport.
    /// Lower values render first. Used for sorting in multi-viewport rendering scenarios.
    /// </summary>
    public int RenderPriority { get; set; } = 0;

    /// <summary>
    /// Gets the cached Vulkan clear color value for this viewport.
    /// Used during Vulkan render pass setup.
    /// </summary>
    public ClearValue ClearColorValue => _clearColorValue;

    /// <summary>
    /// Gets the Vulkan viewport structure (in pixel coordinates).
    /// Automatically updated from normalized coordinates and window size.
    /// </summary>
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

    /// <summary>
    /// Gets the Vulkan scissor rectangle (in pixel coordinates).
    /// Automatically updated from normalized coordinates and window size.
    /// </summary>
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

    private bool _active = false;
    /// <summary>
    /// Returns whether the viewport is currently active and valid for rendering.
    /// Triggers validation if the state is invalid.
    /// </summary>
    /// <returns>True if the viewport is active and valid; otherwise, false.</returns>
    public bool IsActive()
    {
        if (_isValid) return _active;
        Validate();
        return _active;
    }


    /// <summary>
    /// Activates the viewport if it is valid. Logs reasons for failure if activation does not succeed.
    /// Ensures Vulkan state is updated before validation.
    /// </summary>
    /// <returns>True if activation succeeded; otherwise, false.</returns>
    public bool Activate()
    {
        // Ensure Vulkan state is updated before validation
        if (_vulkanStateNeedsUpdate || !_vulkanViewport.HasValue || !_vulkanScissor.HasValue)
        {
            UpdateVulkanViewportAndScissor();
        }

        bool validDimensions = Width > 0f && Height > 0f && Width <= 1f && Height <= 1f;
        bool validContent = Content != null;
        bool validVulkanState = _vulkanViewport.HasValue && _vulkanScissor.HasValue && !_vulkanStateNeedsUpdate;
        bool valid = validDimensions && validContent && validVulkanState;
        _active = valid;
        if (!valid)
        {
            if (logger != null)
            {
                if (!validDimensions)
                    Log.Warning($"Viewport activation failed: Invalid dimensions (Width={Width}, Height={Height})");
                if (!validContent)
                    Log.Warning("Viewport activation failed: Content is null");
                if (!validVulkanState)
                    Log.Warning("Viewport activation failed: Vulkan state is invalid (Viewport/Scissor missing or needs update)");
            }
        }
        return _active;
    }

    /// <summary>
    /// Deactivates the viewport, preventing it from being rendered.
    /// </summary>
    public void Deactivate() => _active = false;

    /// <summary>
    /// Marks the Vulkan viewport state as needing update (e.g., after window resize or property change).
    /// Triggers recalculation of viewport and scissor rectangles on next access.
    /// </summary>

    /// <summary>
    /// Invalidates the viewport, marking it as needing validation and Vulkan state update.
    /// </summary>
    public void Invalidate()
    {
        _isValid = false;
    }


    /// <summary>
    /// Validates the viewport's configuration and Vulkan state.
    /// Checks dimensions, content, and Vulkan state validity.
    /// </summary>
    /// <returns>True if the viewport is valid and ready for rendering; otherwise, false.</returns>
    public bool Validate()
    {
        // Check that width and height are positive and within normalized range
        bool validDimensions = Width > 0f && Height > 0f && Width <= 1f && Height <= 1f;
        // Content must not be null
        bool validContent = Content != null;
        // Vulkan state must be up to date
        bool validVulkanState = _vulkanViewport.HasValue && _vulkanScissor.HasValue && !_vulkanStateNeedsUpdate;
        // Mark as valid only if all checks pass
        _isValid = validDimensions && validContent && validVulkanState;
        return _isValid;
    }

    /// <summary>
    /// Computes Vulkan viewport and scissor rectangles from normalized coordinates and current window size.
    /// Called automatically when state is invalidated.
    /// </summary>
    private void UpdateVulkanViewportAndScissor()
    {
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

        // Notify StaticCamera of viewport size changes
        if (Camera is StaticCamera staticCamera)
        {
            staticCamera.SetViewportSize(pixelWidth, pixelHeight);
        }

        _vulkanStateNeedsUpdate = false;
    }

    /// <summary>
    /// Updates the cached Vulkan clear color value from <see cref="BackgroundColor"/>.
    /// Called automatically when the background color changes.
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
}
