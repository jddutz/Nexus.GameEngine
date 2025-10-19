using System.Reflection;

namespace Nexus.GameEngine.Resources.Textures.Definitions;

/// <summary>
/// Basic texture definition with file path, color space, and source assembly specification.
/// </summary>
/// <param name="FilePath">Path to texture within embedded resources (e.g., "Resources/Textures/uvmap.png")</param>
/// <param name="IsSrgb">True if texture contains sRGB color data, false for linear data. Default is true.</param>
/// <param name="SourceAssembly">Assembly containing the embedded resource. If null, uses GameEngine assembly.</param>
public record SimpleTextureDefinition(
    string FilePath, 
    bool IsSrgb = true,
    Assembly? SourceAssembly = null) : ITextureDefinition
{
    /// <inheritdoc />
    public string Name => Path.GetFileNameWithoutExtension(FilePath);
}
