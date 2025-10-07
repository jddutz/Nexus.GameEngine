namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines the blending mode for a rendering pass.
/// Specifies how new pixels are combined with existing pixels in the framebuffer.
/// </summary>
public enum BlendingMode
{
    /// <summary>
    /// No blending - new pixels completely replace old pixels.
    /// Equivalent to disabled blending.
    /// </summary>
    None,

    /// <summary>
    /// Standard alpha blending for transparency.
    /// Result = SrcAlpha * NewColor + (1 - SrcAlpha) * OldColor
    /// </summary>
    Alpha,

    /// <summary>
    /// Additive blending - colors are added together.
    /// Result = NewColor + OldColor
    /// Useful for effects like fire, explosions, lights.
    /// </summary>
    Additive,

    /// <summary>
    /// Multiplicative blending - colors are multiplied together.
    /// Result = NewColor * OldColor
    /// Useful for darkening effects, shadows, overlays.
    /// </summary>
    Multiply,

    /// <summary>
    /// Premultiplied alpha blending.
    /// Result = NewColor + (1 - SrcAlpha) * OldColor
    /// More efficient when alpha is already multiplied into RGB.
    /// </summary>
    PremultipliedAlpha,

    /// <summary>
    /// Subtractive blending - new color is subtracted from old.
    /// Result = OldColor - NewColor
    /// Useful for darkening effects.
    /// </summary>
    Subtract
}