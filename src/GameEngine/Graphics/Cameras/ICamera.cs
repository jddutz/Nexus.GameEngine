namespace Nexus.GameEngine.Graphics.Cameras;

public interface ICamera : IRuntimeComponent, IRenderPriority
{
    /// <summary>
    /// Gets the screen region this camera renders to, in normalized coordinates (0-1).
    /// Default is (0, 0, 1, 1) representing the full screen.
    /// </summary>
    Rectangle<float> ScreenRegion { get; }

    /// <summary>
    /// Gets the clear color for this camera's viewport.
    /// Default is black (0, 0, 0, 1).
    /// </summary>
    Vector4D<float> ClearColor { get; }

    // RenderPriority inherited from IRenderPriority

    /// <summary>
    /// Gets the render pass mask determining which render passes this camera participates in.
    /// Default is RenderPasses.All.
    /// </summary>
    uint RenderPassMask { get; }

    /// <summary>
    /// Creates and returns a Viewport record for this camera based on current settings.
    /// The Viewport contains only essential Vulkan rendering state.
    /// </summary>
    /// <returns>A new Viewport record configured for this camera.</returns>
    Viewport GetViewport();

    /// <summary>
    /// Gets the view matrix representing the camera's transformation in world space.
    /// Used by the renderer for transforming objects from world to camera space.
    /// </summary>
    Matrix4X4<float> ViewMatrix { get; }

    /// <summary>
    /// Gets the projection matrix used to project 3D coordinates to 2D screen space.
    /// </summary>
    Matrix4X4<float> ProjectionMatrix { get; }

    /// <summary>
    /// Gets the combined view-projection matrix (ViewMatrix * ProjectionMatrix).
    /// This is commonly used for transforming vertices from world space to NDC.
    /// Implementations should cache this value to avoid recomputing every frame.
    /// </summary>
    /// <returns>The combined view-projection transformation matrix</returns>
    Matrix4X4<float> GetViewProjectionMatrix();

    /// <summary>
    /// Gets the camera's position in world space.
    /// </summary>
    Vector3D<float> Position { get; }

    /// <summary>
    /// Gets the forward direction vector of the camera in world space.
    /// </summary>
    Vector3D<float> Forward { get; }

    /// <summary>
    /// Gets the descriptor set containing the camera's ViewProjection UBO.
    /// This descriptor set should be bound at set=0, binding=0 for shaders that need the transformation matrix.
    /// </summary>
    /// <returns>The descriptor set containing the ViewProjection uniform buffer.</returns>
    DescriptorSet GetViewProjectionDescriptorSet();

    /// <summary>
    /// Gets the up direction vector of the camera in world space.
    /// </summary>
    Vector3D<float> Up { get; }

    /// <summary>
    /// Gets the right direction vector of the camera in world space.
    /// </summary>
    Vector3D<float> Right { get; }

    /// <summary>
    /// Determines if the specified 3D bounding box is visible within the camera's view frustum.
    /// </summary>
    /// <param name="bounds">The 3D bounding box to check.</param>
    /// <returns>True if the bounds are visible; otherwise, false.</returns>
    /// <remarks>
    /// If <paramref name="bounds"/> contains invalid values, the result is implementation-defined. Implementations should handle invalid or degenerate boxes gracefully.
    /// </remarks>
    bool IsVisible(Box3D<float> bounds);

    /// <summary>
    /// Converts a screen-space point to a world-space ray for picking or selection.
    /// </summary>
    /// <param name="screenPoint">The screen-space point (pixel coordinates).</param>
    /// <param name="screenWidth">The width of the screen in pixels.</param>
    /// <param name="screenHeight">The height of the screen in pixels.</param>
    /// <returns>A ray in world space originating from the camera through the screen point.</returns>
    /// <remarks>
    /// If <paramref name="screenPoint"/> is outside the bounds defined by <paramref name="screenWidth"/> and <paramref name="screenHeight"/>, implementations should either clamp the value, throw an exception, or return a default ray. Document the chosen behavior in concrete implementations.
    /// </remarks>
    Ray3D<float> ScreenToWorldRay(Vector2D<int> screenPoint, int screenWidth, int screenHeight);

    /// <summary>
    /// Projects a world-space point to screen-space coordinates.
    /// </summary>
    /// <param name="worldPoint">The world-space point to project.</param>
    /// <param name="screenWidth">The width of the screen in pixels.</param>
    /// <param name="screenHeight">The height of the screen in pixels.</param>
    /// <returns>The screen-space coordinates (pixel position) of the world point.</returns>
    /// <remarks>
    /// If <paramref name="worldPoint"/> cannot be projected (e.g., is behind the camera or outside the view frustum), implementations should return a sentinel value, clamp to screen bounds, or throw an exception. Document the chosen behavior in concrete implementations.
    /// </remarks>
    Vector2D<int> WorldToScreenPoint(Vector3D<float> worldPoint, int screenWidth, int screenHeight);
}