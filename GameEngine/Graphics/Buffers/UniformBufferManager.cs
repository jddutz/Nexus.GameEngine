using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;

namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Manages uniform buffer objects for efficient uniform data sharing across shaders
/// </summary>
public class UniformBufferManager : IUniformBufferManager
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, UniformBlock> _blocks;
    private readonly object _lockObject = new();

    private int _nextBindingPoint;
    private long _currentFrame;
    private int _updatesThisFrame;
    private bool _disposed;

    /// <summary>
    /// Gets whether this manager has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the active uniform blocks for testing purposes
    /// </summary>
    public IEnumerable<UniformBlock> ActiveBlocks => _blocks.Values.ToArray();

    public UniformBufferManager(GL gl, ILogger logger)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blocks = new ConcurrentDictionary<string, UniformBlock>();
        _nextBindingPoint = 0;
        _currentFrame = 0;

        _logger.LogDebug("Created UniformBufferManager");
    }

    /// <summary>
    /// Creates or retrieves a uniform block with the specified name and size
    /// </summary>
    /// <param name="name">Name of the uniform block</param>
    /// <param name="size">Size in bytes (ignored if block already exists)</param>
    /// <returns>The uniform block</returns>
    public UniformBlock CreateBlock(string name, int size)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Block name cannot be null or empty", nameof(name));

        if (size <= 0)
            throw new ArgumentException("Block size must be positive", nameof(size));

        return _blocks.GetOrAdd(name, _ => CreateNewBlock(name, size));
    }

    /// <summary>
    /// Gets an existing uniform block by name
    /// </summary>
    /// <param name="name">Name of the uniform block</param>
    /// <returns>The uniform block, or null if not found</returns>
    public UniformBlock? GetBlock(string name)
    {
        ThrowIfDisposed();

        _blocks.TryGetValue(name, out var block);
        return block;
    }

    /// <summary>
    /// Updates the data in a uniform block
    /// </summary>
    /// <param name="block">The uniform block to update</param>
    /// <param name="data">Data to upload</param>
    /// <param name="offset">Offset in bytes within the block</param>
    public void UpdateBlock(UniformBlock block, ReadOnlySpan<byte> data, int offset = 0)
    {
        ThrowIfDisposed();

        if (block == null)
            throw new ArgumentNullException(nameof(block));

        if (offset < 0 || offset + data.Length > block.Size)
            throw new ArgumentException($"Data does not fit in block. Block size: {block.Size}, Data size: {data.Length}, Offset: {offset}");

        // Bind and update the buffer
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, block.BufferId);

        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                _gl.BufferSubData(BufferTargetARB.UniformBuffer, offset, (nuint)data.Length, dataPtr);
            }
        }

        // Update tracking
        block.LastUpdateFrame = _currentFrame;
        _updatesThisFrame++;

        _logger.LogDebug("Updated uniform block {Name} with {DataSize} bytes at offset {Offset}",
            block.Name, data.Length, offset);
    }

    /// <summary>
    /// Updates the data in a uniform block with raw pointer (for testing)
    /// </summary>
    /// <param name="block">The uniform block to update</param>
    /// <param name="dataPtr">Pointer to data</param>
    /// <param name="dataSize">Size of data in bytes</param>
    /// <param name="offset">Offset in bytes within the block</param>
    public unsafe void UpdateBlockRaw(UniformBlock block, IntPtr dataPtr, int dataSize, int offset = 0)
    {
        ThrowIfDisposed();

        if (block == null)
            throw new ArgumentNullException(nameof(block));

        if (offset < 0 || offset + dataSize > block.Size)
            throw new ArgumentException($"Data does not fit in block. Block size: {block.Size}, Data size: {dataSize}, Offset: {offset}");

        // Bind and update the buffer
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, block.BufferId);
        _gl.BufferSubData(BufferTargetARB.UniformBuffer, offset, (nuint)dataSize, dataPtr.ToPointer());

        // Update tracking
        block.LastUpdateFrame = _currentFrame;
        _updatesThisFrame++;

        _logger.LogDebug("Updated uniform block {Name} with {DataSize} bytes at offset {Offset}",
            block.Name, dataSize, offset);
    }

    /// <summary>
    /// Binds a uniform block to its binding point
    /// </summary>
    /// <param name="block">The uniform block to bind</param>
    public void BindBlock(UniformBlock block)
    {
        ThrowIfDisposed();

        if (block == null)
            throw new ArgumentNullException(nameof(block));

        _gl.BindBufferRange(
            BufferTargetARB.UniformBuffer,
            (uint)block.BindingPoint,
            block.BufferId,
            0,
            (nuint)block.Size);

        block.IsBound = true;

        _logger.LogDebug("Bound uniform block {Name} to binding point {BindingPoint}",
            block.Name, block.BindingPoint);
    }

    /// <summary>
    /// Advances to the next frame, resetting per-frame counters
    /// </summary>
    public void NextFrame()
    {
        ThrowIfDisposed();

        _currentFrame++;
        _updatesThisFrame = 0;

        // Reset bound flags
        foreach (var block in _blocks.Values)
        {
            block.IsBound = false;
        }

        _logger.LogDebug("Advanced to frame {Frame}", _currentFrame);
    }

    /// <summary>
    /// Gets current uniform buffer usage statistics
    /// </summary>
    /// <returns>Usage statistics</returns>
    public UniformBufferStatistics GetStatistics()
    {
        ThrowIfDisposed();

        var blocks = _blocks.Values.ToArray();
        var totalMemory = blocks.Sum(b => b.Size);
        var boundBlocks = blocks.Count(b => b.IsBound);

        return new UniformBufferStatistics(
            blocks.Length,
            totalMemory,
            boundBlocks,
            _updatesThisFrame);
    }

    /// <summary>
    /// Creates a new uniform block with OpenGL buffer
    /// </summary>
    private UniformBlock CreateNewBlock(string name, int size)
    {
        lock (_lockObject)
        {
            // Create OpenGL buffer
            var bufferId = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, bufferId);

            // Allocate buffer storage
            _gl.BufferData(BufferTargetARB.UniformBuffer, (nuint)size, in IntPtr.Zero, BufferUsageARB.DynamicDraw);

            // Assign binding point
            var bindingPoint = _nextBindingPoint++;

            var block = new UniformBlock(name, size, bufferId, bindingPoint);

            _logger.LogDebug("Created uniform block {Name}: Size={Size}, BufferId={BufferId}, BindingPoint={BindingPoint}",
                name, size, bufferId, bindingPoint);

            return block;
        }
    }

    /// <summary>
    /// Throws if the manager has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UniformBufferManager));
    }

    /// <summary>
    /// Disposes the manager and releases all uniform blocks
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing UniformBufferManager with {BlockCount} blocks", _blocks.Count);

        // Delete all buffers
        foreach (var block in _blocks.Values)
        {
            _gl.DeleteBuffer(block.BufferId);
            _logger.LogDebug("Deleted uniform buffer {BufferId} for block {Name}", block.BufferId, block.Name);
        }

        _blocks.Clear();
        _disposed = true;
    }
}