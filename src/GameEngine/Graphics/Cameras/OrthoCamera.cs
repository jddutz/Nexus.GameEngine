using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Orthographic camera for simulating 2D using 3D world coordinates. Orientation is fixed.
/// </summary>
public partial class OrthoCamera : RuntimeComponent, ICamera, ICameraController, IOrthographicController
{
    /// <summary>
    /// Template for configuring Orthographic cameras.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Width of the orthographic projection view.
        /// </summary>
        public float Width { get; set; } = 10f;

        /// <summary>
        /// Height of the orthographic projection view.
        /// </summary>
        public float Height { get; set; } = 10f;

        /// <summary>
        /// Near clipping plane distance.
        /// </summary>
        public float NearPlane { get; set; } = -1000f;

        /// <summary>
        /// Far clipping plane distance.
        /// </summary>
        public float FarPlane { get; set; } = 1000f;

        /// <summary>
        /// Position of the camera in world space.
        /// </summary>
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
    }

    // Animated properties for smooth camera movements
    [ComponentProperty(Duration = AnimationDuration.Normal)]
    private float _width = 10f;

    [ComponentProperty(Duration = AnimationDuration.Normal)]
    private float _height = 10f;

    [ComponentProperty]
    private float _nearPlane = -1000f;

    [ComponentProperty]
    private float _farPlane = 1000f;

    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.CubicEaseOut)]
    private Vector3D<float> _position = Vector3D<float>.Zero;

    private bool _matricesDirty = true;

    // Property change callbacks - set matrices dirty when camera properties change
    partial void OnWidthChanged(float oldValue) => _matricesDirty = true;
    partial void OnHeightChanged(float oldValue) => _matricesDirty = true;
    partial void OnNearPlaneChanged(float oldValue) => _matricesDirty = true;
    partial void OnFarPlaneChanged(float oldValue) => _matricesDirty = true;
    partial void OnPositionChanged(Vector3D<float> oldValue) => _matricesDirty = true;

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

    /// <summary>
    /// Render passes that this camera should execute.
    /// Default configuration includes all standard rendering passes.
    /// </summary>
    public List<RenderPassConfiguration> RenderPasses { get; set; } =
    [
        new RenderPassConfiguration { Id = 0, Name = "Main", DepthTestEnabled = true, BlendingMode = BlendingMode.Alpha }
    ];

    private Matrix4X4<float> _viewMatrix;
    private Matrix4X4<float> _projectionMatrix;

    /// <summary>
    /// Configure the component using the specified template.
    /// </summary>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            // Direct assignment during configuration - no deferred updates needed
            _width = template.Width;
            _height = template.Height;
            _nearPlane = template.NearPlane;
            _farPlane = template.FarPlane;
            _position = template.Position;
            _matricesDirty = true;
        }
    }

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

    // ICameraController implementation - properties now handle deferred updates and dirty flag automatically
    public void SetPosition(Vector3D<float> position) => Position = position;

    public void Translate(Vector3D<float> translation) => Position += translation;

    public void SetForward(Vector3D<float> forward)
    {
        // OrthoCamera has fixed forward direction (-Z), but we'll allow it for interface compatibility
        // In practice, this would be ignored since Forward is fixed for orthographic cameras
    }

    public void SetUp(Vector3D<float> up)
    {
        // OrthoCamera has fixed up direction (+Y), but we'll allow it for interface compatibility
        // In practice, this would be ignored since Up is fixed for orthographic cameras
    }

    public void LookAt(Vector3D<float> target)
    {
        // OrthoCamera has fixed orientation, but we can move to position camera for the target
        // This effectively centers the orthographic view on the target
        Position = new Vector3D<float>(target.X, target.Y, Position.Z);
    }

    // IOrthographicController implementation - properties now handle deferred updates and dirty flag automatically
    public void SetWidth(float width) => Width = width;

    public void SetHeight(float height) => Height = height;

    public void SetSize(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public void SetNearPlane(float nearPlane) => NearPlane = nearPlane;

    public void SetFarPlane(float farPlane) => FarPlane = farPlane;

}