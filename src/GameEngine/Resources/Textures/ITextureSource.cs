using Nexus.GameEngine.Resources.Sources;

namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Source for loading texture data.
/// Implementations handle different texture formats and loading mechanisms.
/// </summary>
public interface ITextureSource : IResourceSource<TextureSourceData>
{
}
