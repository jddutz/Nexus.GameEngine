namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Manages font resource lifecycle including loading, atlas generation, and caching.
/// </summary>
public interface IFontResourceManager
{
    /// <summary>
    /// Gets an existing font resource or creates a new one from the definition.
    /// Resources are cached and reused for identical definitions.
    /// </summary>
    /// <param name="definition">The font definition specifying source, size, and character range.</param>
    /// <returns>A font resource with GPU texture atlas and glyph metrics.</returns>
    FontResource GetOrCreate(FontDefinition definition);

    /// <summary>
    /// Releases a font resource. The resource is only disposed when no longer referenced.
    /// </summary>
    /// <param name="definition">The font definition to release.</param>
    void Release(FontDefinition definition);
}
