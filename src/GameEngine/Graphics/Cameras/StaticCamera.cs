namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Orthographic camera for UI rendering. Creates a pixel-to-NDC projection matrix
/// that transforms pixel coordinates to normalized device coordinates.
/// The viewport dimensions must be set via <see cref="SetViewportSize"/> before rendering.
/// </summary>
public partial class StaticCamera : RuntimeComponent, ICamera
{
    /// <summary>
    /// Template for configuring Static cameras.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Near clipping plane distance.
        /// Default: -1 (UI elements can be at Z=0)
        /// </summary>
        public float NearPlane { get; set; } = -1f;

        /// <summary>
        /// Far clipping plane distance.
        /// Default: 1 (UI elements can be at Z=0)
        /// </summary>
        public float FarPlane { get; set; } = 1f;
    }

    // Viewport dimensions in pixels
    private float _viewportWidth = 1920f;  // Default fallback
    private float _viewportHeight = 1080f; // Default fallback

    // Clipping plane distances
    public float NearPlane { get; private set; } = -1f;
    public float FarPlane { get; private set; } = 1f;

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
        // Identity view matrix (UI coordinates are in screen space)
        ViewMatrix = Matrix4X4<float>.Identity;

        // Create pixel-to-NDC orthographic projection
        // CreateOrthographicOffCenter maps:
        // (0, 0) in pixel space -> (-1, 1) in NDC (top-left)
        // (width, height) in pixel space -> (1, -1) in NDC (bottom-right)
        ProjectionMatrix = Matrix4X4.CreateOrthographicOffCenter(
            0f,              // left
            _viewportWidth,  // right
            _viewportHeight, // bottom (flipped for Vulkan)
            0f,              // top (flipped for Vulkan)
            NearPlane,
            FarPlane);

        // Mark cached matrix as dirty
        _viewProjectionDirty = true;
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

    /// <summary>
    /// Configure the component using the specified template.
    /// </summary>
    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            NearPlane = template.NearPlane;
            FarPlane = template.FarPlane;
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Initialize matrices with default viewport size
        // Viewport will call SetViewportSize() with actual dimensions
        InitializeMatrices();
    }
}