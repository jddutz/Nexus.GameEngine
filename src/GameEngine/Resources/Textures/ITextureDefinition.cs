namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Defines a texture resource to be loaded.
/// </summary>
public interface ITextureDefinition : IResourceDefinition
{
    /// <summary>
    /// Path to the texture file within the embedded resources.
    /// Example: "Resources/Textures/uvmap.png"
    /// </summary>
    string FilePath { get; }
    
    /// <summary>
    /// Whether this texture contains sRGB color data (true) or linear data (false).
    /// 
    /// Use TRUE for:
    /// - Color textures (albedo, diffuse, emissive)
    /// - UI sprites and images created by artists in standard image editors
    /// - Any texture representing visible colors
    /// 
    /// Use FALSE for:
    /// - Normal maps
    /// - Height/displacement maps
    /// - Roughness, metallic, ambient occlusion maps
    /// - Masks and data textures
    /// - Test images with raw coordinate data
    /// 
    /// Default is true (sRGB) as most textures from artists are color data.
    /// </summary>
    bool IsSrgb { get; }
}