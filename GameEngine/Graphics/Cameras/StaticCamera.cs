using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Rendering;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Simple orthographic camera for rendering UI components and textures. Fixed position and orientation, placed at high Z.
/// </summary>
public class StaticCamera : RuntimeComponent, ICamera
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

    private float _orthographicSize = 200000f;
    private float _nearPlane = -50000f;
    private float _farPlane = 50000f;

    // Fixed position at high Z value to avoid conflicts with 3D objects
    public Vector3D<float> Position { get; } = new Vector3D<float>(0, 0, 100000f);
    public Vector3D<float> Forward { get; } = -Vector3D<float>.UnitZ;
    public Vector3D<float> Up { get; } = Vector3D<float>.UnitY;
    public Vector3D<float> Right { get; } = Vector3D<float>.UnitX;

    public Matrix4X4<float> ViewMatrix { get; private set; }
    public Matrix4X4<float> ProjectionMatrix { get; private set; }

    /// <summary>
    /// Render passes that this camera should execute.
    /// Static cameras typically render UI elements with specific pass configuration.
    /// </summary>
    public List<RenderPassConfiguration> RenderPasses { get; set; } = new()
    {
        new RenderPassConfiguration { Id = 1, Name = "UI", DepthTestEnabled = false, BlendingMode = BlendingMode.Alpha }
    };

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
        ProjectionMatrix = Matrix4X4.CreateOrthographic(_orthographicSize, _orthographicSize, _nearPlane, _farPlane);
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
        var worldX = normalizedX * _orthographicSize * 0.5f;
        var worldY = normalizedY * _orthographicSize * 0.5f;

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
        var normalizedX = rightProjection / (_orthographicSize * 0.5f);
        var normalizedY = upProjection / (_orthographicSize * 0.5f);

        // Convert to screen coordinates
        var screenX = (int)((normalizedX + 1.0f) * 0.5f * screenWidth);
        var screenY = (int)((1.0f - normalizedY) * 0.5f * screenHeight);

        return new Vector2D<int>(screenX, screenY);
    }

    /// <summary>
    /// Configure the component using the specified template.
    /// </summary>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            _orthographicSize = template.OrthographicSize;
            _nearPlane = template.NearPlane;
            _farPlane = template.FarPlane;

            // Reinitialize matrices with new configuration
            InitializeMatrices();
        }
    }
}