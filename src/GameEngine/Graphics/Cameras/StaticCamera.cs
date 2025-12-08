using Nexus.GameEngine.Runtime.Systems;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Orthographic camera for UI rendering with top-left origin coordinate system.
/// Creates a projection matrix that transforms pixel coordinates (top-left at origin)
/// to normalized device coordinates. Origin (0,0) is at the top-left of the viewport,
/// with coordinates ranging from (0, 0) to (width, height).
/// The viewport dimensions must be set via <see cref="SetViewportSize"/> before rendering.
/// Manages its own ViewProjection UBO buffer and descriptor set for efficient rendering.
/// </summary>
public partial class StaticCamera : Component, ICamera
{
    public StaticCamera()
    {
    }

    // UBO for ViewProjection matrix
    private Silk.NET.Vulkan.Buffer _viewProjectionBuffer;
    private DeviceMemory _viewProjectionMemory;
    private DescriptorSet _viewProjectionDescriptorSet;
    private DescriptorSetLayout _viewProjectionDescriptorLayout;
    private bool _uboInitialized = false;

    // Viewport dimensions in pixels
    // DELIBERATELY WRONG defaults to catch initialization bugs - must call SetViewportSize()
    private float _viewportWidth = 640f;   // Obvious fallback
    private float _viewportHeight = 480f;  // Obvious fallback

    // Clipping plane distances
    [ComponentProperty]
    [TemplateProperty]
    private float _nearPlane = -1f;

    [ComponentProperty]
    [TemplateProperty]
    private float _farPlane = 1f;

    // Viewport-related properties (ICamera interface)
    // ComponentProperty attributes make these configurable via template and provide deferred updates
    [ComponentProperty]
    [TemplateProperty]
    private Rectangle<float> _screenRegion = new(-1, -1, 2, 2);

    [ComponentProperty]
    [TemplateProperty]
    private Vector4D<float> _clearColor = new(0, 0, 0, 1);

    [ComponentProperty]
    [TemplateProperty]
    private int _renderPriority = 0;

    [ComponentProperty]
    [TemplateProperty]
    private uint _renderPassMask = RenderPasses.All;

    // Fixed position for UI rendering (identity view matrix)
    public Vector3D<float> Position { get; } = Vector3D<float>.Zero;
    public Vector3D<float> Forward { get; } = -Vector3D<float>.UnitZ;
    public Vector3D<float> Up { get; } = Vector3D<float>.UnitY;
    public Vector3D<float> Right { get; } = Vector3D<float>.UnitX;

    public Matrix4X4<float> ViewMatrix { get; private set; }
    public Matrix4X4<float> ProjectionMatrix { get; private set; }

    // Cached view-projection matrix
    private Matrix4X4<float> _viewProjectionMatrix;
    private bool _viewProjectionDirty = true;
    
    // Cached visible rectangle in pixel coordinates (origin.x, origin.y, size.x, size.y)
    private Rectangle<float> _visibleRect;
    private bool _visibleRectDirty = true;

    /// <summary>
    /// Updates the camera's projection matrix to match the viewport dimensions.
    /// This should be called by the viewport whenever its size changes.
    /// </summary>
    /// <param name="width">Viewport width in pixels</param>
    /// <param name="height">Viewport height in pixels</param>
    public void SetViewportSize(float width, float height)
    {
        if (Math.Abs(_viewportWidth - width) < 0.01f && Math.Abs(_viewportHeight - height) < 0.01f)
            return; // No change

        _viewportWidth = width;
        _viewportHeight = height;
        InitializeMatrices();
    }

    private void InitializeMatrices()
    {
        // Identity view matrix (UI coordinates are in screen space)
        ViewMatrix = Matrix4X4<float>.Identity;

        // Create pixel-to-NDC orthographic projection with TOP-LEFT origin
        // Our coordinate system: (0,0) at top-left, +X right, +Y down (standard UI convention)
        // We want: (0, 0) pixel -> (-1, -1) NDC (top-left)
        //          (width, height) pixel -> (1, 1) NDC (bottom-right)
        // Note: Vulkan NDC has Y pointing down, matching our UI coordinate system
        ProjectionMatrix = Matrix4X4.CreateOrthographicOffCenter(
            0f,              // left
            _viewportWidth,  // right
            0f,              // bottom
            _viewportHeight, // top
            _nearPlane,
            _farPlane);

        // Mark cached matrix as dirty
        _viewProjectionDirty = true;
        
        // Mark visible rect dirty (depends on viewport size / screen region)
        _visibleRectDirty = true;

        // Update UBO with new matrix
        UpdateViewProjectionUBO();
    }

    private void UpdateVisibleRect()
    {
        if (!_visibleRectDirty) return;

        // With top-left coordinate system, visible rect spans from (0,0) to (width, height)
        _visibleRect = new Rectangle<float>(
            0f,
            0f,
            _viewportWidth,
            _viewportHeight
        );
        
        _visibleRectDirty = false;
    }

    public Matrix4X4<float> GetViewProjectionMatrix()
    {
        if (_viewProjectionDirty)
        {
            // Use GLSL column-vector convention: projection * view
            // so that shaders can do: viewProjection * model * vec4(pos, 1.0)
            _viewProjectionMatrix = ProjectionMatrix * ViewMatrix;
            _viewProjectionDirty = false;
        }

        return _viewProjectionMatrix;
    }

    public Viewport GetViewport()
    {
        // Convert normalized screen region to pixel coordinates
        var x = (int)(_screenRegion.Origin.X * _viewportWidth);
        var y = (int)(_screenRegion.Origin.Y * _viewportHeight);
        var width = (uint)(_screenRegion.Size.X * _viewportWidth);
        var height = (uint)(_screenRegion.Size.Y * _viewportHeight);

        return new Viewport
        {
            Extent = new Extent2D(width, height),
            ClearColor = _clearColor,
            RenderPassMask = _renderPassMask
        };
    }

    public bool IsVisible(Box3D<float> bounds)
    {
        // Ensure cached visible rect is up to date (only recalculated on viewport/matrix changes)
        UpdateVisibleRect();

        // Check for overlap in X and Y. Return true only if they overlap.
        var overlapX = bounds.Max.X >= _visibleRect.Origin.X && bounds.Min.X <= _visibleRect.Max.X;
        var overlapY = bounds.Max.Y >= _visibleRect.Origin.Y && bounds.Min.Y <= _visibleRect.Max.Y;

        return overlapX && overlapY;
    }

    public Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight)
    {
        // With top-left origin, screen coordinates are the same as world coordinates
        // Screen: (0,0) at top-left -> World: (0,0) at top-left
        var rayOrigin = new Vector3D<float>(screenPoint.X, screenPoint.Y, 0);

        // Ray direction is always forward for orthographic projection
        return new Ray3D<float>(rayOrigin, Forward);
    }

    public Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight)
    {
        // With top-left origin, world coordinates are the same as screen coordinates
        // World: (0,0) at top-left -> Screen: (0,0) at top-left
        var screenX = (int)worldPoint.X;
        var screenY = (int)worldPoint.Y;
        return new Vector2D<int>(screenX, screenY);
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Initialize matrices with default viewport size
        // Renderer will call SetViewportSize() with actual dimensions before first render
        InitializeMatrices();
        
        // Initialize UBO for ViewProjection matrix
        InitializeViewProjectionUBO();
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        
        // Clean up UBO resources
        CleanupViewProjectionUBO();
    }

    private unsafe void InitializeViewProjectionUBO()
    {
        if (_uboInitialized) return;

        var uboSize = (ulong)sizeof(ViewProjectionUBO);

        // Create UBO buffer
        (_viewProjectionBuffer, _viewProjectionMemory) = ((ResourceSystem)Resources).BufferManager.CreateUniformBuffer(uboSize);

        // Create descriptor set layout for UBO (set=0, binding=0)
        var layoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };
        _viewProjectionDescriptorLayout = ((GraphicsSystem)Graphics).DescriptorManager.CreateDescriptorSetLayout([layoutBinding]);

        // Allocate descriptor set
        _viewProjectionDescriptorSet = ((GraphicsSystem)Graphics).DescriptorManager.AllocateDescriptorSet(_viewProjectionDescriptorLayout);

        // Write descriptor set to bind UBO
        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = _viewProjectionBuffer,
            Offset = 0,
            Range = uboSize
        };

        var writeDescriptorSet = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _viewProjectionDescriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        var context = ((GraphicsSystem)Graphics).Context;
        context.VulkanApi.UpdateDescriptorSets(context.Device, 1, &writeDescriptorSet, 0, null);

        // Update UBO with initial matrix
        UpdateViewProjectionUBO();

        _uboInitialized = true;
    }

    private unsafe void UpdateViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        var ubo = ViewProjectionUBO.FromMatrix(GetViewProjectionMatrix());
        var span = new ReadOnlySpan<byte>(&ubo, sizeof(ViewProjectionUBO));
        ((ResourceSystem)Resources).BufferManager.UpdateUniformBuffer(_viewProjectionMemory, span);
    }

    private void CleanupViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        ((ResourceSystem)Resources).BufferManager.DestroyBuffer(_viewProjectionBuffer, _viewProjectionMemory);
        // Descriptor set is freed automatically when pool is reset
        
        _uboInitialized = false;
    }

    public DescriptorSet GetViewProjectionDescriptorSet()
    {
        return _uboInitialized ? _viewProjectionDescriptorSet : default;
    }
}