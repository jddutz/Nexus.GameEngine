using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Simple orthographic camera for rendering UI components and textures. Fixed position and orientation, placed at high Z.
/// </summary>
public partial class StaticCamera : RuntimeComponent, ICamera
{
    /// <summary>
    /// Template for configuring Static cameras.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Size of the orthographic projection for UI rendering.
        /// Large enough to accommodate various UI element sizes.
        /// </summary>
        public float OrthographicSize { get; set; } = 200000f;

        /// <summary>
        /// Near clipping plane distance.
        /// </summary>
        public float NearPlane { get; set; } = -50000f;

        /// <summary>
        /// Far clipping plane distance.
        /// </summary>
        public float FarPlane { get; set; } = 50000f;
    }

    // Static readonly properties - configured once at initialization
    public float OrthographicSize { get; private set; } = 200000f;
    public float NearPlane { get; private set; } = -50000f;
    public float FarPlane { get; private set; } = 50000f;

    // Fixed position at high Z value to avoid conflicts with 3D objects
    public Vector3D<float> Position { get; } = new Vector3D<float>(0, 0, 100000f);
    public Vector3D<float> Forward { get; } = -Vector3D<float>.UnitZ;
    public Vector3D<float> Up { get; } = Vector3D<float>.UnitY;
    public Vector3D<float> Right { get; } = Vector3D<float>.UnitX;

    public Matrix4X4<float> ViewMatrix { get; private set; }
    public Matrix4X4<float> ProjectionMatrix { get; private set; }

    public StaticCamera()
    {
        InitializeMatrices();
    }

    private void InitializeMatrices()
    {
        // Create view matrix looking down the negative Z axis
        var target = Position + Forward;
        ViewMatrix = Matrix4X4.CreateLookAt(Position, target, Up);

        // Create a very large orthographic projection for UI rendering
        // Use a large range to accommodate various UI element depths
        ProjectionMatrix = Matrix4X4.CreateOrthographic(OrthographicSize, OrthographicSize, NearPlane, FarPlane);
    }

    public bool IsVisible(Box3D<float> bounds)
    {
        // Always visible for UI/textures - StaticCamera has no depth restrictions
        return true;
    }

    public Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight)
    {
        // Convert screen coordinates to world space for UI interaction
        var normalizedX = (2.0f * screenPoint.X) / screenWidth - 1.0f;
        var normalizedY = 1.0f - (2.0f * screenPoint.Y) / screenHeight;

        // Calculate world position based on the large orthographic projection
        var worldX = normalizedX * OrthographicSize * 0.5f;
        var worldY = normalizedY * OrthographicSize * 0.5f;

        // Ray origin is at the camera position offset by screen coordinates
        var rayOrigin = Position + (Right * worldX) + (Up * worldY);

        // Ray direction is always forward for orthographic projection
        return new Ray3D<float>(rayOrigin, Forward);
    }

    public Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight)
    {
        // Transform world point relative to camera position
        var relativeToCamera = worldPoint - Position;

        // Project onto camera's right and up vectors
        var rightProjection = Vector3D.Dot(relativeToCamera, Right);
        var upProjection = Vector3D.Dot(relativeToCamera, Up);

        // Normalize based on the orthographic projection size
        var normalizedX = rightProjection / (OrthographicSize * 0.5f);
        var normalizedY = upProjection / (OrthographicSize * 0.5f);

        // Convert to screen coordinates
        var screenX = (int)((normalizedX + 1.0f) * 0.5f * screenWidth);
        var screenY = (int)((1.0f - normalizedY) * 0.5f * screenHeight);

        return new Vector2D<int>(screenX, screenY);
    }

    /// <summary>
    /// Configure the component using the specified template.
    /// Static camera properties are set once during configuration and never change.
    /// </summary>
    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            OrthographicSize = template.OrthographicSize;
            NearPlane = template.NearPlane;
            FarPlane = template.FarPlane;

            // Initialize matrices with configured values
            InitializeMatrices();
        }
    }
}