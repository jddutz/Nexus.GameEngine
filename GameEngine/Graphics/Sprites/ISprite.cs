using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Sprites;

/// <summary>
/// Behavior interface for components that render 2D sprites.
/// Implement this interface for 2D sprite-based rendering with texture support.
/// </summary>
public interface ISprite
{
    /// <summary>
    /// The path or identifier of the texture/sprite to render.
    /// </summary>
    string? TexturePath { get; set; }

    /// <summary>
    /// The source rectangle within the texture to render.
    /// If null, renders the entire texture.
    /// </summary>
    Rectangle<float>? SourceRectangle { get; set; }

    /// <summary>
    /// The tint color applied to the sprite.
    /// White (255,255,255,255) renders the sprite with original colors.
    /// </summary>
    Vector4D<float> Tint { get; set; }

    /// <summary>
    /// The opacity/alpha value of the sprite.
    /// 1.0 is fully opaque, 0.0 is fully transparent.
    /// </summary>
    float Opacity { get; set; }

    /// <summary>
    /// The sprite rendering effects (flip horizontally, vertically, etc.).
    /// </summary>
    SpriteEffectsEnum Effects { get; set; }

    /// <summary>
    /// The depth/layer for sprite sorting.
    /// Higher values are rendered in front of lower values.
    /// </summary>
    float Depth { get; set; }
}