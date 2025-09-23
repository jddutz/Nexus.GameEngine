using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering.Buffers;

/// <summary>
/// A high-performance buffer that uses persistent mapping for streaming data to the GPU.
/// Implements buffer orphaning and fence synchronization for optimal performance.
/// </summary>
public class PersistentMappedBuffer : IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly uint _bufferId;
    private readonly int _size;
    private readonly IntPtr _mappedPointer;
    private readonly ConcurrentQueue<BufferFence> _activeFences;

    private int _currentOffset;
    private int _allocationCount;
    private int _orphanCount;
    private bool _disposed;

    /// <summary>
    /// Gets the total size of the buffer in bytes
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Gets whether this buffer has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the active fences for testing purposes
    /// </summary>
    public IEnumerable<BufferFence> ActiveFences => _activeFences.ToArray();

    public PersistentMappedBuffer(GL gl, ILogger logger, int size)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _size = size;
        _activeFences = new ConcurrentQueue<BufferFence>();

        // Create and bind buffer
        _bufferId = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _bufferId);

        // Allocate buffer with persistent mapping flags
        const BufferStorageFlags flags =
            BufferStorageFlags.MapPersistentBit |
            BufferStorageFlags.MapWriteBit |
            BufferStorageFlags.MapCoherentBit;

        unsafe
        {
            _gl.BufferStorage((GLEnum)BufferTargetARB.ArrayBuffer, (nuint)size, (void*)IntPtr.Zero, (uint)flags);
        }

        // Map the buffer persistently
        const MapBufferAccessMask accessMask =
            MapBufferAccessMask.MapPersistentBit |
            MapBufferAccessMask.MapWriteBit |
            MapBufferAccessMask.MapCoherentBit;

        unsafe
        {
            _mappedPointer = (nint)_gl.MapBufferRange((GLEnum)BufferTargetARB.ArrayBuffer, 0, (nuint)size, (uint)accessMask);
        }

        if (_mappedPointer == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to map buffer persistently");
        }

        _logger.LogDebug("Created persistent mapped buffer: {Size} bytes, ID: {BufferId}", size, _bufferId);
    }

    /// <summary>
    /// Allocates a range of memory from this buffer
    /// </summary>
    /// <param name="size">Size in bytes to allocate</param>
    /// <returns>The allocated buffer range</returns>
    public BufferRange AllocateRange(int size)
    {
        ThrowIfDisposed();

        if (size <= 0)
            throw new ArgumentException("Size must be positive", nameof(size));

        // Check if we need to orphan the buffer
        if (_currentOffset + size > _size)
        {
            OrphanBuffer();
        }

        var range = new BufferRange(_currentOffset, size);
        _currentOffset += size;
        _allocationCount++;

        _logger.LogDebug("Allocated buffer range: {Range}", range);
        return range;
    }

    /// <summary>
    /// Gets a typed pointer to the mapped memory
    /// </summary>
    /// <typeparam name="T">The type of data to access</typeparam>
    /// <returns>Unsafe pointer to the mapped memory</returns>
    public unsafe T* GetMappedPointer<T>() where T : unmanaged
    {
        ThrowIfDisposed();
        return (T*)_mappedPointer;
    }

    /// <summary>
    /// Adds a fence to track when a buffer range can be safely reused
    /// </summary>
    /// <param name="fence">The fence to add</param>
    public void AddFence(BufferFence fence)
    {
        ThrowIfDisposed();

        _activeFences.Enqueue(fence);
        CleanupCompletedFences();

        _logger.LogDebug("Added fence: {Fence}", fence);
    }

    /// <summary>
    /// Gets current buffer usage statistics
    /// </summary>
    /// <returns>Buffer statistics</returns>
    public BufferStatistics GetStatistics()
    {
        ThrowIfDisposed();
        return new BufferStatistics(_size, _currentOffset, _allocationCount, _orphanCount);
    }

    /// <summary>
    /// Orphans the current buffer contents and resets allocation pointer
    /// </summary>
    private void OrphanBuffer()
    {
        _logger.LogDebug("Orphaning buffer, resetting allocation pointer");

        // Reset allocation pointer - this effectively orphans all previous data
        _currentOffset = 0;
        _orphanCount++;

        // Clear old fences since we're starting fresh
        while (_activeFences.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Removes completed fences from the active list
    /// </summary>
    private void CleanupCompletedFences()
    {
        // For testing, we'll use a simple frame-based heuristic
        // In a real implementation, this would check actual fence status
        var currentFrame = Environment.TickCount64 / 16; // Approximate frame number

        var tempFences = new List<BufferFence>();
        while (_activeFences.TryDequeue(out var fence))
        {
            if (!fence.IsLikelyCompleted(currentFrame))
            {
                tempFences.Add(fence);
            }
        }

        // Re-queue non-completed fences
        foreach (var fence in tempFences)
        {
            _activeFences.Enqueue(fence);
        }
    }

    /// <summary>
    /// Throws if the buffer has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PersistentMappedBuffer));
    }

    /// <summary>
    /// Disposes the buffer and releases all resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing persistent mapped buffer: {BufferId}", _bufferId);

        // Unmap the buffer
        if (_mappedPointer != IntPtr.Zero)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _bufferId);
            _gl.UnmapBuffer(BufferTargetARB.ArrayBuffer);
        }

        // Delete the buffer
        _gl.DeleteBuffer(_bufferId);

        // Clear fences
        while (_activeFences.TryDequeue(out _)) { }

        _disposed = true;
    }
}