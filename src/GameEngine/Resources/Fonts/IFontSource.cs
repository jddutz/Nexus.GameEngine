using Nexus.GameEngine.Resources.Sources;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Source for loading font data.
/// Implementations handle different font formats and loading mechanisms.
/// </summary>
public interface IFontSource : IResourceSource<FontSourceData>
{
}
