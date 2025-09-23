namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines the rendering style for Border components.
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// Simple rectangular border using DrawRectangle primitives.
    /// </summary>
    Rectangle = 0,

    /// <summary>
    /// Rectangular border with rounded corners.
    /// Future implementation using custom shaders or ninepatch.
    /// </summary>
    RoundedRect = 1,

    /// <summary>
    /// Border rendered using a single background image texture.
    /// Future implementation.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Scalable border using ninepatch image technique.
    /// Future implementation for complex border designs.
    /// </summary>
    NinePatch = 3
}
