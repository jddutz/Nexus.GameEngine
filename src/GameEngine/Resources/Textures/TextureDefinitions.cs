using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Default texture definitions for common procedural textures.
/// These are static instances of TextureDefinition that can be used throughout the application.
/// </summary>
public static class TextureDefinitions
{
    /// <summary>
    /// 1x1 white texture (1, 1, 1, 1) - useful as default or for solid color tinting.
    /// </summary>
    public static readonly TextureDefinition White = new()
    {
        Name = "__white_1x1",
        Source = new ArgbArrayTextureSource(
            width: 1,
            height: 1,
            colors: [Colors.White],
            format: Format.R8G8B8A8Unorm)
    };
    
    /// <summary>
    /// 1x1 black texture (0, 0, 0, 1) - useful for solid black rendering.
    /// </summary>
    public static readonly TextureDefinition Black = new()
    {
        Name = "__black_1x1",
        Source = new ArgbArrayTextureSource(
            width: 1,
            height: 1,
            colors: [Colors.Black],
            format: Format.R8G8B8A8Unorm)
    };
    
    /// <summary>
    /// 2x2 checker pattern (white/black) - useful for testing and UV visualization.
    /// </summary>
    public static readonly TextureDefinition Checker = new()
    {
        Name = "__checker_2x2",
        Source = new ArgbArrayTextureSource(
            width: 2,
            height: 2,
            colors: 
            [
                Colors.White, Colors.Black,  // Top row
                Colors.Black, Colors.White   // Bottom row
            ],
            format: Format.R8G8B8A8Unorm)
    };
    
    /// <summary>
    /// 2x2 magenta/black pattern - indicates missing or error texture.
    /// </summary>
    public static readonly TextureDefinition MissingTexture = new()
    {
        Name = "__missing_2x2",
        Source = new ArgbArrayTextureSource(
            width: 2,
            height: 2,
            colors: 
            [
                Colors.Magenta, Colors.Black,  // Top row
                Colors.Black, Colors.Magenta   // Bottom row
            ],
            format: Format.R8G8B8A8Unorm)
    };
}
