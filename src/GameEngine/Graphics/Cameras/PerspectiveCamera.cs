using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// A typical 3D perspective camera with configurable FOV, view range, and perspective control.
/// </summary>
public partial class PerspectiveCamera : RuntimeComponent, ICamera
{
    /// <summary>
    /// Template for configuring Perspective cameras.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Field of view angle in radians. Default is PI/4 (45 degrees).
        /// </summary>
        public float FieldOfView { get; set; } = MathF.PI / 4;

        /// <summary>
        /// Near clipping plane distance.
        /// </summary>
        public float NearPlane { get; set; } = 0.1f;

        /// <summary>
        /// Far clipping plane distance.
        /// </summary>
        public float FarPlane { get; set; } = 1000f;

        /// <summary>
        /// Aspect ratio (width/height) of the viewport.
        /// </summary>
        public float AspectRatio { get; set; } = 16f / 9f;

        /// <summary>
        /// Position of the camera in world space.
        /// </summary>
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;

        /// <summary>
        /// Forward direction vector of the camera.
        /// </summary>
        public Vector3D<float> Forward { get; set; } = -Vector3D<float>.UnitZ;

        /// <summary>
        /// Up direction vector of the camera.
        /// </summary>
        public Vector3D<float> Up { get; set; } = Vector3D<float>.UnitY;
    }

    // Animated properties with slow easing for smooth camera movements
    [ComponentProperty]
    private Vector3D<float> _position = Vector3D<float>.Zero;

    [ComponentProperty]
    private Vector3D<float> _forward = -Vector3D<float>.UnitZ;

    [ComponentProperty]
    private Vector3D<float> _up = Vector3D<float>.UnitY;

    [ComponentProperty]
    private float _fieldOfView = MathF.PI / 4; // 45 degrees

    // Instant properties (no animation needed for clipping planes and aspect ratio)
    [ComponentProperty]
    private float _nearPlane = 0.1f;

    [ComponentProperty]
    private float _farPlane = 1000f;

    [ComponentProperty]
    private float _aspectRatio = 16f / 9f;

    // Non-component properties
    private Vector3D<float> _right = Vector3D<float>.UnitX;
    private bool _matricesDirty = true;

    public Vector3D<float> Right => _right;

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

    public PerspectiveCamera()
    {
        UpdateDirectionVectors();
        UpdateMatrices();
    }

    private void UpdateDirectionVectors()
    {
        _right = Vector3D.Normalize(Vector3D.Cross(_forward, _up));
        _up = Vector3D.Normalize(Vector3D.Cross(_right, _forward));
    }

    private void UpdateMatrices()
    {
        // Create view matrix using look-at
        var target = _position + _forward;
        _viewMatrix = Matrix4X4.CreateLookAt(_position, target, _up);

        // Create perspective projection matrix
        _projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, _nearPlane, _farPlane);

        _matricesDirty = false;
    }

    public bool IsVisible(Box3D<float> bounds)
    {
        // Simple frustum culling - check if bounding box intersects with view frustum
        // For now, implement a basic distance check
        var center = (bounds.Min + bounds.Max) * 0.5f;
        var distance = Vector3D.Distance(_position, center);

        // Check if within near/far plane range
        if (distance < _nearPlane || distance > _farPlane)
            return false;

        // Check if in front of camera
        var directionToCenter = Vector3D.Normalize(center - _position);
        var dot = Vector3D.Dot(directionToCenter, _forward);

        return dot > 0; // Object is in front of camera
    }

    public Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight)
    {
        // Convert screen coordinates to normalized device coordinates
        var x = (2.0f * screenPoint.X) / screenWidth - 1.0f;
        var y = 1.0f - (2.0f * screenPoint.Y) / screenHeight;

        // Create ray in clip space
        var rayClip = new Vector4D<float>(x, y, -1.0f, 1.0f);

        // Transform to eye space
        Matrix4X4.Invert(ProjectionMatrix, out var invProjection);
        var rayEye = Vector4D.Transform(rayClip, invProjection);
        rayEye = new Vector4D<float>(rayEye.X, rayEye.Y, -1.0f, 0.0f);

        // Transform to world space
        Matrix4X4.Invert(ViewMatrix, out var invView);
        var rayWorld = Vector4D.Transform(rayEye, invView);
        var rayDirection = Vector3D.Normalize(new Vector3D<float>(rayWorld.X, rayWorld.Y, rayWorld.Z));

        return new Ray3D<float>(_position, rayDirection);
    }

    public Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight)
    {
        // Transform world point to clip space
        var clipSpace = Vector4D.Transform(new Vector4D<float>(worldPoint, 1.0f), ViewMatrix * ProjectionMatrix);

        // Perform perspective divide
        if (Math.Abs(clipSpace.W) < float.Epsilon)
            return new Vector2D<int>(-1, -1); // Point is at infinity or behind camera

        var ndc = new Vector3D<float>(clipSpace.X / clipSpace.W, clipSpace.Y / clipSpace.W, clipSpace.Z / clipSpace.W);

        // Convert to screen coordinates
        var screenX = (int)((ndc.X + 1.0f) * 0.5f * screenWidth);
        var screenY = (int)((1.0f - ndc.Y) * 0.5f * screenHeight);

        return new Vector2D<int>(screenX, screenY);
    }
    
    public void Translate(Vector3D<float> translation)
    {
        SetPosition(Position + translation);
    }

    public void LookAt(Vector3D<float> target)
    {
        SetForward(Vector3D.Normalize(target - Position));
    }

    /// <summary>
    /// Configure the component using the specified template.
    /// </summary>
    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            SetFieldOfView(template.FieldOfView);
            SetNearPlane(template.NearPlane);
            SetFarPlane(template.FarPlane);
            SetAspectRatio(template.AspectRatio);
            SetPosition(template.Position);
            SetForward(template.Forward);
            SetUp(template.Up);
        }
    }

    // Property change callbacks - mark matrices dirty when camera properties change
    partial void OnFieldOfViewChanged(float oldValue) => _matricesDirty = true;
    partial void OnNearPlaneChanged(float oldValue) => _matricesDirty = true;
    partial void OnFarPlaneChanged(float oldValue) => _matricesDirty = true;
    partial void OnAspectRatioChanged(float oldValue) => _matricesDirty = true;
    partial void OnPositionChanged(Vector3D<float> oldValue) => _matricesDirty = true;

    partial void OnForwardChanged(Vector3D<float> oldValue)
    {
        _forward = Vector3D.Normalize(_forward);
        UpdateDirectionVectors();
        _matricesDirty = true;
    }

    partial void OnUpChanged(Vector3D<float> oldValue)
    {
        _up = Vector3D.Normalize(_up);
        UpdateDirectionVectors();
        _matricesDirty = true;
    }
}