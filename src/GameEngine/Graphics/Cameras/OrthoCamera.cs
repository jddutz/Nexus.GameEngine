namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Orthographic camera for simulating 2D using 3D world coordinates. Orientation is fixed.
/// </summary>
public partial class OrthoCamera : RuntimeComponent, ICamera
{
    private readonly IBufferManager _bufferManager;
    private readonly IDescriptorManager _descriptorManager;
    private readonly IGraphicsContext _graphicsContext;

    // ViewProjection UBO resources
    private Silk.NET.Vulkan.Buffer _viewProjectionBuffer;
    private DeviceMemory _viewProjectionMemory;
    private DescriptorSet _viewProjectionDescriptorSet;
    private DescriptorSetLayout _viewProjectionDescriptorLayout;
    private bool _uboInitialized;

    public OrthoCamera(
        IBufferManager bufferManager,
        IDescriptorManager descriptorManager,
        IGraphicsContext graphicsContext)
    {
        _bufferManager = bufferManager;
        _descriptorManager = descriptorManager;
        _graphicsContext = graphicsContext;
    }

    // Template is auto-generated from [TemplateProperty] fields below

    // Animated properties for smooth camera movements
    [ComponentProperty]
    [TemplateProperty]
    private float _width = 10f;

    [ComponentProperty]
    [TemplateProperty]
    private float _height = 10f;

    [ComponentProperty]
    [TemplateProperty]
    private float _nearPlane = -1000f;

    [ComponentProperty]
    [TemplateProperty]
    private float _farPlane = 1000f;

    [ComponentProperty]
    [TemplateProperty]
    private Vector3D<float> _position = Vector3D<float>.Zero;

    // Viewport-related properties (ICamera interface)
    [ComponentProperty]
    [TemplateProperty]
    private Rectangle<float> _screenRegion = new(0, 0, 1, 1);

    [ComponentProperty]
    [TemplateProperty]
    private Vector4D<float> _clearColor = new(0, 0, 0, 1);

    [ComponentProperty]
    [TemplateProperty]
    private int _renderPriority = 0;

    [ComponentProperty]
    [TemplateProperty]
    private uint _renderPassMask = RenderPasses.All;

    private bool _matricesDirty = true;

    // Fixed orientation for orthographic camera
    public Vector3D<float> Forward { get; } = -Vector3D<float>.UnitZ;
    public Vector3D<float> Up { get; } = Vector3D<float>.UnitY;
    public Vector3D<float> Right { get; } = Vector3D<float>.UnitX;

    public Matrix4X4<float> ViewMatrix
    {
        get
        {
            if (_matricesDirty) UpdateMatrices();
            return _viewMatrix;
        }
    }

    public Matrix4X4<float> ProjectionMatrix
    {
        get
        {
            if (_matricesDirty) UpdateMatrices();
            return _projectionMatrix;
        }
    }

    private Matrix4X4<float> _viewMatrix;
    private Matrix4X4<float> _projectionMatrix;
    private Matrix4X4<float> _viewProjectionMatrix;
    private bool _viewProjectionDirty = true;

    // OnLoad is auto-generated from template

    private void UpdateMatrices()
    {
        // Create view matrix using look-at with fixed orientation
        var target = _position + Forward;
        _viewMatrix = Matrix4X4.CreateLookAt(_position, target, Up);

        // Create orthographic projection matrix
        var left = -_width * 0.5f;
        var right = _width * 0.5f;
        var bottom = -_height * 0.5f;
        var top = _height * 0.5f;

        _projectionMatrix = Matrix4X4.CreateOrthographic(_width, _height, _nearPlane, _farPlane);

        _matricesDirty = false;
        _viewProjectionDirty = true; // Mark combined matrix as dirty
        
        // Update UBO with new matrices
        if (_uboInitialized)
        {
            UpdateViewProjectionUBO();
        }
    }

    public Matrix4X4<float> GetViewProjectionMatrix()
    {
        // Ensure view and projection are up to date first
        if (_matricesDirty) UpdateMatrices();
        
        if (_viewProjectionDirty)
        {
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
            _viewProjectionDirty = false;
        }
        return _viewProjectionMatrix;
    }

    public Viewport GetViewport()
    {
        // For ortho camera, we need to get the actual screen dimensions from somewhere
        // Using reasonable defaults based on the orthographic dimensions
        var width = (uint)Math.Max(1920, _width);
        var height = (uint)Math.Max(1080, _height);

        return new Viewport
        {
            Extent = new Extent2D(width, height),
            ClearColor = _clearColor,
            RenderPassMask = _renderPassMask
        };
    }

    /// <summary>
    /// Static accessors for axial directions.
    /// </summary>
    public static Vector3D<float> AxialForward => -Vector3D<float>.UnitZ;
    public static Vector3D<float> AxialUp => Vector3D<float>.UnitY;
    public static Vector3D<float> AxialRight => Vector3D<float>.UnitX;

    public bool IsVisible(Box3D<float> bounds)
    {
        // Check if bounding box intersects with orthographic view volume
        var center = (bounds.Min + bounds.Max) * 0.5f;
        var size = bounds.Max - bounds.Min;

        // Transform to camera space
        var relativeToCamera = center - _position;

        // Check if within orthographic bounds
        var halfWidth = _width * 0.5f;
        var halfHeight = _height * 0.5f;

        // Project onto camera's right and up vectors
        var rightProjection = Vector3D.Dot(relativeToCamera, Right);
        var upProjection = Vector3D.Dot(relativeToCamera, Up);
        var forwardProjection = Vector3D.Dot(relativeToCamera, Forward);

        // Check bounds
        var rightExtent = Vector3D.Dot(size, Right) * 0.5f;
        var upExtent = Vector3D.Dot(size, Up) * 0.5f;
        var forwardExtent = Vector3D.Dot(size, Forward) * 0.5f;

        return Math.Abs(rightProjection) - rightExtent <= halfWidth &&
               Math.Abs(upProjection) - upExtent <= halfHeight &&
               forwardProjection - forwardExtent >= _nearPlane &&
               forwardProjection + forwardExtent <= _farPlane;
    }

    public Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight)
    {
        // Convert screen coordinates to world space for orthographic projection
        var normalizedX = (2.0f * screenPoint.X) / screenWidth - 1.0f;
        var normalizedY = 1.0f - (2.0f * screenPoint.Y) / screenHeight;

        // Calculate world position on the camera plane
        var worldX = normalizedX * _width * 0.5f;
        var worldY = normalizedY * _height * 0.5f;

        // Ray origin is on the camera plane at the specified screen coordinates
        var rayOrigin = _position + (Right * worldX) + (Up * worldY);

        // Ray direction is always forward for orthographic projection
        return new Ray3D<float>(rayOrigin, Forward);
    }

    public Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight)
    {
        // Transform world point to camera space
        var relativeToCamera = worldPoint - _position;

        // Project onto camera's right and up vectors
        var rightProjection = Vector3D.Dot(relativeToCamera, Right);
        var upProjection = Vector3D.Dot(relativeToCamera, Up);

        // Normalize to [-1, 1] range
        var normalizedX = rightProjection / (_width * 0.5f);
        var normalizedY = upProjection / (_height * 0.5f);

        // Convert to screen coordinates
        var screenX = (int)((normalizedX + 1.0f) * 0.5f * screenWidth);
        var screenY = (int)((1.0f - normalizedY) * 0.5f * screenHeight);

        return new Vector2D<int>(screenX, screenY);
    }

    public void Translate(Vector3D<float> translation) => SetPosition(Position + translation);

    public void LookAt(Vector3D<float> target)
    {
        // OrthoCamera has fixed orientation, but we can move to position camera for the target
        // This effectively centers the orthographic view on the target
        SetPosition(new Vector3D<float>(target.X, target.Y, Position.Z));
    }

    public void SetSize(float width, float height)
    {
        SetWidth(width);
        SetHeight(height);
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        InitializeViewProjectionUBO();
    }

    protected override void OnDeactivate()
    {
        CleanupViewProjectionUBO();
        base.OnDeactivate();
    }

    private unsafe void InitializeViewProjectionUBO()
    {
        if (_uboInitialized) return;

        const ulong uboSize = 64; // Size of ViewProjectionUBO (single mat4)

        // Create uniform buffer for ViewProjection matrix
        (_viewProjectionBuffer, _viewProjectionMemory) = _bufferManager.CreateUniformBuffer(uboSize);

        // Create descriptor set layout for the UBO
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
    }

    private unsafe void UpdateViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        var viewProjMatrix = GetViewProjectionMatrix();
        var ubo = ViewProjectionUBO.FromMatrix(viewProjMatrix);
        
        ReadOnlySpan<ViewProjectionUBO> uboSpan = [ubo];
        _bufferManager.UpdateUniformBuffer(_viewProjectionMemory, System.Runtime.InteropServices.MemoryMarshal.AsBytes(uboSpan));
    }

    private void CleanupViewProjectionUBO()
    {
        if (!_uboInitialized) return;

        _bufferManager.DestroyBuffer(_viewProjectionBuffer, _viewProjectionMemory);
        _uboInitialized = false;
    }

    public DescriptorSet GetViewProjectionDescriptorSet()
    {
        return _uboInitialized ? _viewProjectionDescriptorSet : default;
    }
}