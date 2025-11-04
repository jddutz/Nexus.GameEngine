namespace Nexus.GameEngine.Resources.Textures.Definitions;

/// <summary>
/// 1x1 white uniform color texture - used as default texture for solid color UI elements.
/// This allows all UI elements to use the same uber-shader pipeline, eliminating pipeline switches.
/// Elements without an explicit texture use this to enable tint color multiplication.
/// </summary>
public static partial class TextureDefinitions
{
    public static readonly TextureDefinition UniformColor = new()
    {
        Name = "__uniform_color_1x1",
        Source = new EmbeddedPngTextureSource(
            "EmbeddedResources/Textures/uniform_color.png",
            typeof(TextureDefinitions).Assembly)
    };
}