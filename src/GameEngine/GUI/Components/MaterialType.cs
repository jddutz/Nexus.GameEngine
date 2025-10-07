namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Specifies the type of material to use for background rendering.
/// </summary>
public enum MaterialType
{
    /// <summary>
    /// Solid color background with optional effects.
    /// </summary>
    SolidColor,

    /// <summary>
    /// Texture-based background loaded from an image asset.
    /// </summary>
    ImageAsset,

    /// <summary>
    /// Procedurally generated background using shader parameters.
    /// </summary>
    ProceduralTexture
}

/// <summary>
/// Specifies how textures should wrap at the edges.
/// </summary>
public enum TextureWrapMode
{
    /// <summary>
    /// Repeat the texture (GL_REPEAT).
    /// </summary>
    Repeat,

    /// <summary>
    /// Clamp to edge (GL_CLAMP_TO_EDGE).
    /// </summary>
    Clamp,

    /// <summary>
    /// Mirror the texture (GL_MIRRORED_REPEAT).
    /// </summary>
    Mirror
}

/// <summary>
/// Specifies how this layer blends with content behind it.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Replace content behind (no blending).
    /// </summary>
    Replace,

    /// <summary>
    /// Alpha blending (standard transparency).
    /// </summary>
    Alpha,

    /// <summary>
    /// Additive blending (brighten).
    /// </summary>
    Additive,

    /// <summary>
    /// Multiplicative blending (darken).
    /// </summary>
    Multiply
}