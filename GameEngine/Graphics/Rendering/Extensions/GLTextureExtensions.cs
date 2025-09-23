using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering.Extensions;

/// <summary>
/// Extension methods for GL that provide texture-related functionality.
/// </summary>
public static class GLTextureExtensions
{
    /// <summary>
    /// Helper method to bind texture to specified slot
    /// </summary>
    public static void SetTexture(this GL gl, uint textureId, int slot = 0)
    {
        gl.ActiveTexture(TextureUnit.Texture0 + slot);
        gl.BindTexture(TextureTarget.Texture2D, textureId);
    }

    /// <summary>
    /// Helper method to create texture from raw data
    /// </summary>
    public static uint CreateTexture(this GL gl, ReadOnlySpan<byte> data, int width, int height)
    {
        var texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);

        unsafe
        {
            fixed (byte* ptr = data)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        return texture;
    }
}