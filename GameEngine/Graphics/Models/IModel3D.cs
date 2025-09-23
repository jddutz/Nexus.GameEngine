using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Behavior interface for components that render 3D models.
/// Implement this interface for 3D mesh and model rendering.
/// </summary>
public interface IModel3D
{
    /// <summary>
    /// The path or identifier of the 3D model to render.
    /// </summary>
    string? ModelPath { get; set; }

    /// <summary>
    /// The array of texture paths applied to the model.
    /// Multiple textures can be used for different material slots.
    /// </summary>
    string[]? TexturePaths { get; set; }

    /// <summary>
    /// The world transformation matrix for the model.
    /// Combines position, rotation, and scale into a single matrix.
    /// </summary>
    Matrix4X4<float> WorldMatrix { get; set; }

    /// <summary>
    /// The material properties for the model rendering.
    /// Controls how light interacts with the model surface.
    /// </summary>
    Material Material { get; set; }

    /// <summary>
    /// Whether the model casts shadows.
    /// </summary>
    bool CastsShadows { get; set; }

    /// <summary>
    /// Whether the model receives shadows from other objects.
    /// </summary>
    bool ReceivesShadows { get; set; }

    /// <summary>
    /// The level of detail (LOD) to use for rendering.
    /// Higher values use more detailed models, lower values use simplified models.
    /// </summary>
    int LevelOfDetail { get; set; }
}