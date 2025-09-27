namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Interface for managing uniform buffer objects for efficient uniform data sharing across shaders
/// </summary>
public interface IUniformBufferManager : IDisposable
{
    /// <summary>
    /// Gets whether this manager has been disposed
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets the active uniform blocks for testing purposes
    /// </summary>
    IEnumerable<UniformBlock> ActiveBlocks { get; }

    /// <summary>
    /// Creates or retrieves a uniform block with the specified name and size
    /// </summary>
    /// <param name="name">Name of the uniform block</param>
    /// <param name="size">Size in bytes (ignored if block already exists)</param>
    /// <returns>The uniform block</returns>
    UniformBlock CreateBlock(string name, int size);

    /// <summary>
    /// Gets an existing uniform block by name
    /// </summary>
    /// <param name="name">Name of the uniform block</param>
    /// <returns>The uniform block, or null if not found</returns>
    UniformBlock? GetBlock(string name);

    /// <summary>
    /// Updates the data in a uniform block
    /// </summary>
    /// <param name="block">The uniform block to update</param>
    /// <param name="data">Data to upload</param>
    /// <param name="offset">Offset in bytes within the block</param>
    void UpdateBlock(UniformBlock block, ReadOnlySpan<byte> data, int offset = 0);

    /// <summary>
    /// Updates the data in a uniform block with raw pointer (for testing)
    /// </summary>
    /// <param name="block">The uniform block to update</param>
    /// <param name="dataPtr">Pointer to data</param>
    /// <param name="dataSize">Size of data in bytes</param>
    /// <param name="offset">Offset in bytes within the block</param>
    void UpdateBlockRaw(UniformBlock block, IntPtr dataPtr, int dataSize, int offset = 0);

    /// <summary>
    /// Binds a uniform block to its binding point
    /// </summary>
    /// <param name="block">The uniform block to bind</param>
    void BindBlock(UniformBlock block);

    /// <summary>
    /// Advances to the next frame, resetting per-frame counters
    /// </summary>
    void NextFrame();

    /// <summary>
    /// Gets current uniform buffer usage statistics
    /// </summary>
    /// <returns>Usage statistics</returns>
    UniformBufferStatistics GetStatistics();
}