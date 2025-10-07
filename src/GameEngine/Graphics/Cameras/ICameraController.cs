using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Cameras;

/// <summary>
/// Interface for camera components that support position and view control operations.
/// Used for runtime discovery and polymorphic control of camera movement.
/// </summary>
public interface ICameraController
{
    /// <summary>
    /// Sets the camera position in world space. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="position">New world position</param>
    void SetPosition(Vector3D<float> position);

    /// <summary>
    /// Moves the camera by the specified offset. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="offset">Position offset to apply</param>
    void Translate(Vector3D<float> offset);

    /// <summary>
    /// Sets the camera's forward direction. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="forward">New forward direction (will be normalized)</param>
    void SetForward(Vector3D<float> forward);

    /// <summary>
    /// Sets the camera's up direction. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="up">New up direction (will be normalized)</param>
    void SetUp(Vector3D<float> up);

    /// <summary>
    /// Points the camera to look at a specific world position. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="target">World position to look at</param>
    void LookAt(Vector3D<float> target);
}

/// <summary>
/// Interface for perspective camera components that support projection parameter control.
/// Used for runtime discovery and polymorphic control of camera projection settings.
/// </summary>
public interface IPerspectiveController
{
    /// <summary>
    /// Sets the field of view angle in radians. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="fov">Field of view in radians</param>
    void SetFieldOfView(float fov);

    /// <summary>
    /// Sets the near clipping plane distance. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="nearPlane">Near plane distance</param>
    void SetNearPlane(float nearPlane);

    /// <summary>
    /// Sets the far clipping plane distance. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="farPlane">Far plane distance</param>
    void SetFarPlane(float farPlane);

    /// <summary>
    /// Sets the aspect ratio (width/height). Change is applied at next frame boundary.
    /// </summary>
    /// <param name="aspectRatio">Aspect ratio</param>
    void SetAspectRatio(float aspectRatio);
}

/// <summary>
/// Interface for orthographic camera components that support projection parameter control.
/// Used for runtime discovery and polymorphic control of orthographic camera settings.
/// </summary>
public interface IOrthographicController
{
    /// <summary>
    /// Sets the width of the orthographic projection. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="width">Orthographic projection width</param>
    void SetWidth(float width);

    /// <summary>
    /// Sets the height of the orthographic projection. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="height">Orthographic projection height</param>
    void SetHeight(float height);

    /// <summary>
    /// Sets both width and height of the orthographic projection. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="width">Orthographic projection width</param>
    /// <param name="height">Orthographic projection height</param>
    void SetSize(float width, float height);

    /// <summary>
    /// Sets the near clipping plane distance. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="nearPlane">Near plane distance</param>
    void SetNearPlane(float nearPlane);

    /// <summary>
    /// Sets the far clipping plane distance. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="farPlane">Far plane distance</param>
    void SetFarPlane(float farPlane);
}