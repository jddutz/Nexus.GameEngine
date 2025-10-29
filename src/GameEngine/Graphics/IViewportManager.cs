using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for managing viewport creation and lifecycle.
/// </summary>
public interface IViewportManager : IDisposable
{
    /// <summary>
    /// Gets all managed viewports, sorted by render priority.
    /// </summary>
    IReadOnlyCollection<Viewport> Viewports { get; }

    /// <summary>
    /// Creates a new viewport with the specified parameters.
    /// </summary>
    /// <param name="x">X position (normalized 0-1)</param>
    /// <param name="y">Y position (normalized 0-1)</param>
    /// <param name="width">Width (normalized 0-1)</param>
    /// <param name="height">Height (normalized 0-1)</param>
    /// <param name="backgroundColor">Optional background color</param>
    /// <returns>The created viewport</returns>
    Viewport CreateViewport(
        float x = 0f,
        float y = 0f,
        float width = 1f,
        float height = 1f,
        Vector4D<float>? backgroundColor = null);

    /// <summary>
    /// Adds a viewport to management.
    /// </summary>
    /// <param name="viewport">The viewport to add</param>
    void AddViewport(Viewport viewport);

    /// <summary>
    /// Removes a viewport from management.
    /// </summary>
    /// <param name="viewport">The viewport to remove</param>
    void RemoveViewport(Viewport viewport);

    /// <summary>
    /// Invalidates all viewport Vulkan state (e.g., after window resize).
    /// </summary>
    void InvalidateAllViewports();
}
