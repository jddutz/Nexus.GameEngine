using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Runtime.Extensions;

/// <summary>
/// Extension methods for the resource system to provide easy access to common resource operations.
/// </summary>
public static class ResourceSystemExtensions
{
    /// <summary>
    /// Gets or creates a geometry resource from a definition.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The geometry definition.</param>
    /// <returns>The geometry resource, or null if creation failed.</returns>
    public static IGeometryResource? GetGeometry(this IResourceSystem system, GeometryDefinition definition)
    {
        return ((ResourceSystem)system).ResourceManager.Geometry.GetOrCreate(definition);
    }

    /// <summary>
    /// Releases a geometry resource.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The geometry definition to release.</param>
    public static void ReleaseGeometry(this IResourceSystem system, GeometryDefinition definition)
    {
        ((ResourceSystem)system).ResourceManager.Geometry.Release(definition);
    }

    /// <summary>
    /// Gets or creates a font resource from a definition.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The font definition.</param>
    /// <returns>The font resource.</returns>
    public static FontResource GetFont(this IResourceSystem system, FontDefinition definition)
    {
        return ((ResourceSystem)system).ResourceManager.Fonts.GetOrCreate(definition);
    }

    /// <summary>
    /// Releases a font resource.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The font definition to release.</param>
    public static void ReleaseFont(this IResourceSystem system, FontDefinition definition)
    {
        ((ResourceSystem)system).ResourceManager.Fonts.Release(definition);
    }

    /// <summary>
    /// Gets or creates a texture resource from a definition.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The texture definition.</param>
    /// <returns>The texture resource.</returns>
    public static TextureResource GetTexture(this IResourceSystem system, TextureDefinition definition)
    {
        return ((ResourceSystem)system).ResourceManager.Textures.GetOrCreate(definition);
    }

    /// <summary>
    /// Releases a texture resource.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The texture definition to release.</param>
    public static void ReleaseTexture(this IResourceSystem system, TextureDefinition definition)
    {
        ((ResourceSystem)system).ResourceManager.Textures.Release(definition);
    }

    /// <summary>
    /// Gets or creates a shader resource from a definition.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="definition">The shader definition.</param>
    /// <returns>The shader resource.</returns>
    public static ShaderResource GetShader(this IResourceSystem system, ShaderDefinition definition)
    {
        return ((ResourceSystem)system).ResourceManager.Shaders.GetOrCreate(definition);
    }

    /// <summary>
    /// Creates a vertex buffer from data.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="data">The vertex data.</param>
    /// <returns>A tuple containing the buffer and its memory.</returns>
    public static (Buffer, DeviceMemory) CreateVertexBuffer(this IResourceSystem system, ReadOnlySpan<byte> data)
    {
        return ((ResourceSystem)system).BufferManager.CreateVertexBuffer(data);
    }

    /// <summary>
    /// Creates a uniform buffer of a specified size.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>A tuple containing the buffer and its memory.</returns>
    public static (Buffer, DeviceMemory) CreateUniformBuffer(this IResourceSystem system, ulong size)
    {
        return ((ResourceSystem)system).BufferManager.CreateUniformBuffer(size);
    }

    /// <summary>
    /// Loads a texture from a file path.
    /// </summary>
    /// <param name="system">The resource system.</param>
    /// <param name="path">The path to the texture file.</param>
    /// <returns>The loaded texture resource.</returns>
    public static TextureResource GetTexture(this IResourceSystem system, string path)
    {
        return ((ResourceSystem)system).ResourceManager.Textures.GetOrCreate(new TextureDefinition
        {
            Name = path,
            Source = new EmbeddedPngTextureSource(path, System.Reflection.Assembly.GetEntryAssembly()!)
        });
    }
}
