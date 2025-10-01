using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Graphics.Textures;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Sprites;

/// <summary>
/// Interface for sprite components that support runtime property control operations.
/// Used for runtime discovery and polymorphic control of sprite appearance and behavior.
/// </summary>
public interface ISpriteController
{
    /// <summary>
    /// Sets the sprite size in world units. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="size">New sprite size</param>
    void SetSize(Vector2D<float> size);

    /// <summary>
    /// Sets the sprite size with individual width and height. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="width">Sprite width</param>
    /// <param name="height">Sprite height</param>
    void SetSize(float width, float height);

    /// <summary>
    /// Scales the current sprite size by a multiplier. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="scale">Scale multiplier</param>
    void Scale(float scale);

    /// <summary>
    /// Scales the current sprite size by separate X and Y multipliers. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="scaleX">X-axis scale multiplier</param>
    /// <param name="scaleY">Y-axis scale multiplier</param>
    void Scale(float scaleX, float scaleY);

    /// <summary>
    /// Sets the sprite tint color (RGBA). Change is applied at next frame boundary.
    /// </summary>
    /// <param name="tint">New tint color</param>
    void SetTint(Vector4D<float> tint);

    /// <summary>
    /// Sets the sprite tint color with individual RGBA components. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="r">Red component (0-1)</param>
    /// <param name="g">Green component (0-1)</param>
    /// <param name="b">Blue component (0-1)</param>
    /// <param name="a">Alpha component (0-1)</param>
    void SetTint(float r, float g, float b, float a = 1.0f);

    /// <summary>
    /// Sets the sprite horizontal flip state. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="flipX">Whether to flip horizontally</param>
    void SetFlipX(bool flipX);

    /// <summary>
    /// Sets the sprite vertical flip state. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="flipY">Whether to flip vertically</param>
    void SetFlipY(bool flipY);

    /// <summary>
    /// Sets both flip states simultaneously. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="flipX">Whether to flip horizontally</param>
    /// <param name="flipY">Whether to flip vertically</param>
    void SetFlip(bool flipX, bool flipY);

    /// <summary>
    /// Sets the sprite visibility. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="visible">Whether sprite should be visible</param>
    void SetVisible(bool visible);

    /// <summary>
    /// Sets the sprite texture asset. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="textureAsset">New texture asset reference</param>
    void SetTexture(AssetReference<ManagedTexture> textureAsset);
}