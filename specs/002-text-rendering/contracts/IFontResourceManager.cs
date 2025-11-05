// Contract: IFontResourceManager Interface
// Purpose: Font resource lifecycle management (creation, caching, disposal)
// Package: GameEngine.Resources.Fonts

using Nexus.GameEngine.Resources.Fonts;

namespace Nexus.GameEngine.Resources
{
    /// <summary>
    /// Manages font resource lifecycle including font atlas texture generation,
    /// shared geometry creation, glyph metrics calculation, and resource caching.
    /// Follows standard resource manager pattern with GetOrCreate and Release semantics.
    /// </summary>
    public interface IFontResourceManager
    {
        /// <summary>
        /// Gets an existing cached font resource or creates a new one from the specified definition.
        /// </summary>
        /// <param name="definition">Font definition specifying source, size, and character range</param>
        /// <returns>Cached or newly created font resource</returns>
        /// <remarks>
        /// Implementation details:
        /// - Cache key = definition.Source.GetUniqueName() + Size + CharacterRange
        /// - If cached, increments reference count and returns existing resource
        /// - If not cached, generates font atlas texture + shared geometry + glyph metrics
        /// - Font atlas generation includes:
        ///   1. Load TrueType font data via IFontSource
        ///   2. Rasterize glyphs using StbTrueType at specified size
        ///   3. Pack glyphs into atlas texture using row-based packing
        ///   4. Upload atlas to GPU as R8_UNORM texture
        ///   5. Generate shared geometry buffer (charCount × 4 vertices with pre-baked UVs)
        ///   6. Upload geometry to GPU via GeometryResourceManager
        ///   7. Build GlyphInfo dictionary with metrics and UV coordinates
        /// - Expected performance: <100ms for ASCII printable at 16pt
        /// </remarks>
        FontResource GetOrCreate(FontDefinition definition);

        /// <summary>
        /// Releases a font resource reference, disposing GPU resources if no references remain.
        /// </summary>
        /// <param name="resource">Font resource to release</param>
        /// <remarks>
        /// Implementation details:
        /// - Decrements reference count for resource
        /// - If reference count reaches zero:
        ///   1. Dispose descriptor sets (caller responsibility, not manager)
        ///   2. Release shared geometry via GeometryResourceManager.Release()
        ///   3. Dispose atlas texture GPU resources (image, image view, sampler)
        ///   4. Remove from cache
        /// - Does not throw if resource already released or not found
        /// </remarks>
        void Release(FontResource resource);

        /// <summary>
        /// Gets count of currently cached font resources (for diagnostics/testing).
        /// </summary>
        int CachedResourceCount { get; }

        /// <summary>
        /// Clears all cached font resources, releasing GPU memory.
        /// </summary>
        /// <remarks>
        /// WARNING: Calling this while TextElements are active will cause dangling references.
        /// Only use during application shutdown or testing scenarios where all text elements
        /// have been deactivated.
        /// </remarks>
        void ClearCache();
    }
}
