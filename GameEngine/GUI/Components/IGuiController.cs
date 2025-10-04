using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Interface for GUI components that support runtime property control operations.
/// Used for runtime discovery and polymorphic control of GUI component properties.
/// </summary>
public interface IGuiController
{
    /// <summary>
    /// Sets the visibility of the GUI component. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="visible">Whether the component should be visible</param>
    void SetVisible(bool visible);
}

/// <summary>
/// Interface for background layer components that support runtime control operations.
/// Used for runtime discovery and polymorphic control of background appearance.
/// </summary>
public interface IBackgroundController : IGuiController
{
    /// <summary>
    /// Sets the background color (RGBA). Change is applied at next frame boundary.
    /// </summary>
    /// <param name="color">New background color</param>
    void SetBackgroundColor(Vector4D<float> color);

    /// <summary>
    /// Sets the background color with individual RGBA components. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="r">Red component (0-1)</param>
    /// <param name="g">Green component (0-1)</param>
    /// <param name="b">Blue component (0-1)</param>
    /// <param name="a">Alpha component (0-1)</param>
    void SetBackgroundColor(float r, float g, float b, float a = 1.0f);

    /// <summary>
    /// Sets the background to a predefined color. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="colorName">Name of the predefined color (e.g., "Red", "Blue", "CornflowerBlue")</param>
    void SetBackgroundColor(string colorName);

    /// <summary>
    /// Fades the background color to a target color over time. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="targetColor">Target color to fade to</param>
    /// <param name="factor">Interpolation factor (0-1, where 0 = current color, 1 = target color)</param>
    void FadeToColor(Vector4D<float> targetColor, float factor);
}

/// <summary>
/// Controller interface for TextElement components.
/// Provides methods for runtime control of text content, appearance, and positioning.
/// </summary>
public interface ITextController : IGuiController
{
    /// <summary>
    /// Set the text content to display. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="text">The text content</param>
    void SetText(string? text);

    /// <summary>
    /// Set the text color using Vector4D values. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="color">The color as RGBA values (0.0 to 1.0)</param>
    void SetColor(Vector4D<float> color);

    /// <summary>
    /// Set the text color with individual RGBA components. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="r">Red component (0-1)</param>
    /// <param name="g">Green component (0-1)</param>
    /// <param name="b">Blue component (0-1)</param>
    /// <param name="a">Alpha component (0-1)</param>
    void SetColor(float r, float g, float b, float a = 1.0f);

    /// <summary>
    /// Set the text color using a named color. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="colorName">The name of the color (e.g., "Red", "Blue", "White")</param>
    void SetColor(string colorName);

    /// <summary>
    /// Set the font size in pixels. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="fontSize">The font size (minimum 1.0)</param>
    void SetFontSize(float fontSize);

    /// <summary>
    /// Set the font family name. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="fontName">The font family name</param>
    void SetFontName(string fontName);

    /// <summary>
    /// Set the text alignment within the component bounds. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="alignment">The text alignment</param>
    void SetAlignment(TextAlignment alignment);

    /// <summary>
    /// Animate the text color over time. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="targetColor">The target color to animate to</param>
    /// <param name="factor">Interpolation factor (0-1, where 0 = current color, 1 = target color)</param>
    void AnimateColor(Vector4D<float> targetColor, float factor);

    /// <summary>
    /// Scale the font size by a factor. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="scaleFactor">The scale factor (1.0 = current size, 2.0 = double size, 0.5 = half size)</param>
    void ScaleFontSize(float scaleFactor);
}