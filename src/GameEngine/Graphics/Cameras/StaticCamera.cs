namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Orthographic camera for UI rendering. Creates a pixel-to-NDC projection matrix
/// that transforms pixel coordinates to normalized device coordinates.
/// The viewport dimensions must be set via <see cref="SetViewportSize"/> before rendering.
/// Manages its own ViewProjection UBO buffer and descriptor set for efficient rendering.
/// </summary>
public partial class StaticCamera : RuntimeComponent, ICamera
{
    private readonly IBufferManager _bufferManager;
    private readonly IDescriptorManager _descriptorManager;
    private readonly IGraphicsContext _graphicsContext;

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

    // Constructor with dependency injection
    public StaticCamera(
        IBufferManager bufferManager,
        IDescriptorManager descriptorManager,
        IGraphicsContext graphicsContext)
    {
        _bufferManager = bufferManager;
        _descriptorManager = descriptorManager;
        _graphicsContext = graphicsContext;
    }

    // Clipping plane distances
    [ComponentProperty]
    private float _nearPlane = -1f;

    [ComponentProperty]
    private float _farPlane = 1f;

    // Viewport-related properties (ICamera interface)
    // ComponentProperty attributes make these configurable via template and provide deferred updates
    [ComponentProperty]
    private Rectangle<float> _screenRegion = new(0, 0, 1, 1);

    [ComponentProperty]
    private Vector4D<float> _clearColor = new(0, 0, 0, 1);

    [ComponentProperty]
    private int _renderPriority = 0;

    [ComponentProperty]
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
        Log.Debug($"StaticCamera.InitializeMatrices() called - viewport size: {_viewportWidth}x{_viewportHeight}");
        
        // Identity view matrix (UI coordinates are in screen space)
        ViewMatrix = Matrix4X4<float>.Identity;
        Log.Debug($"  ViewMatrix set to: {ViewMatrix}");

        // Create pixel-to-NDC orthographic projection
        // Our coordinate system: +Y goes DOWN (screen space)
        // Geometry vertices use same convention: (-1,-1) is top-left, (1,1) is bottom-right
        // We want: (0, 0) pixel -> (-1, -1) NDC (top-left)
        //          (width, height) pixel -> (1, 1) NDC (bottom-right)
        // CreateOrthographicOffCenter(left, right, bottom, top) with bottom > top inverts Y
        ProjectionMatrix = Matrix4X4.CreateOrthographicOffCenter(
            0f,              // left
            _viewportWidth,  // right
            0f,              // bottom (NOT flipped - we want Y to go down)
            _viewportHeight, // top (bottom < top means Y increases downward in NDC)
            _nearPlane,
            _farPlane);
        Log.Debug($"  ProjectionMatrix set to: {ProjectionMatrix}");

        // Mark cached matrix as dirty
        _viewProjectionDirty = true;
        
        // Update UBO with new matrix
        UpdateViewProjectionUBO();
    }

    public Matrix4X4<float> GetViewProjectionMatrix()
    {
        if (_viewProjectionDirty)
        {
            _viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
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
        // Always visible for UI/textures - StaticCamera has no depth restrictions
        return true;
    }

    public Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight)
    {
        // For UI camera, screen coordinates ARE world coordinates (pixels)
        // Ray origin is at the screen point in pixel space
        var rayOrigin = new Vector3D<float>(screenPoint.X, screenPoint.Y, 0);

        // Ray direction is always forward for orthographic projection
        return new Ray3D<float>(rayOrigin, Forward);
    }

    public Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight)
    {
        // For UI camera, world coordinates (pixels) ARE screen coordinates
        return new Vector2D<int>((int)worldPoint.X, (int)worldPoint.Y);
    }

    protected override void OnActivate()
    {
        Log.Info($"StaticCamera.OnActivate() START - IsActive={IsActive}");
        base.OnActivate();

        // Initialize matrices with default viewport size
        // Renderer will call SetViewportSize() with actual dimensions before first render
        Log.Debug($"  Calling InitializeMatrices()...");
        InitializeMatrices();
        
        // Initialize UBO for ViewProjection matrix
        Log.Debug($"  Calling InitializeViewProjectionUBO()...");
        InitializeViewProjectionUBO();
        
        Log.Info($"StaticCamera.OnActivate() COMPLETE - _uboInitialized={_uboInitialized}");
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
        (_viewProjectionBuffer, _viewProjectionMemory) = _bufferManager.CreateUniformBuffer(uboSize);

        // Create descriptor set layout for UBO (set=0, binding=0)
        var layoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };
        _viewProjectionDescriptorLayout = _descriptorManager.CreateDescriptorSetLayout(new[] { layoutBinding });

        // Allocate descriptor set
        _viewProjectionDescriptorSet = _descriptorManager.AllocateDescriptorSet(_viewProjectionDescriptorLayout);

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

        _graphicsContext.VulkanApi.UpdateDescriptorSets(_graphicsContext.Device, 1, &writeDescriptorSet, 0, null);

        // Update UBO with initial matrix
        UpdateViewProjectionUBO();

        _uboInitialized = true;
        Log.Info($"StaticCamera: Initialized ViewProjection UBO (size: {uboSize} bytes)");
    }

    private unsafe void UpdateViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        var ubo = ViewProjectionUBO.FromMatrix(GetViewProjectionMatrix());
        var span = new ReadOnlySpan<byte>(&ubo, sizeof(ViewProjectionUBO));
        _bufferManager.UpdateUniformBuffer(_viewProjectionMemory, span);
    }

    private void CleanupViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        _bufferManager.DestroyBuffer(_viewProjectionBuffer, _viewProjectionMemory);
        // Descriptor set is freed automatically when pool is reset
        
        _uboInitialized = false;
        Log.Info("StaticCamera: Cleaned up ViewProjection UBO");
    }

    public DescriptorSet GetViewProjectionDescriptorSet()
    {
        if (!_uboInitialized)
        {
            Log.Warning("StaticCamera: GetViewProjectionDescriptorSet called before UBO initialization");
            return default;
        }
        return _viewProjectionDescriptorSet;
    }
}