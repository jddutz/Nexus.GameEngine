using System.Diagnostics;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using VkFence = Silk.NET.Vulkan.Fence;

namespace Nexus.GameEngine.Graphics.Synchronization;

/// <summary>
/// Manages Vulkan synchronization primitives for the "frames in flight" pattern.
/// Creates and manages semaphores and fences for multiple concurrent frames.
/// </summary>
public sealed class SyncManager : ISyncManager
{
    private readonly IGraphicsContext _context;
    private readonly ILogger _logger;
    private readonly FrameSync[] _frameSyncs;
    private readonly Dictionary<uint, ImageSync> _imageSyncs = [];
    private readonly object _imageSyncLock = new();
    private readonly Stopwatch _waitStopwatch = new();
    
    // Statistics
    private long _totalFenceWaits;
    private long _fenceWaitTimeouts;
    private long _totalFenceResets;
    private double _totalFenceWaitTimeMs;
    private long _deviceWaitIdleCalls;
    private long _queueWaitIdleCalls;
    private long _totalFramesRendered;
    
    private bool _disposed;

    /// <summary>
    /// Creates a new synchronization manager with the specified number of frames in flight.
    /// </summary>
    /// <param name="context">Graphics context providing device and Vulkan API access</param>
    /// <param name="loggerFactory">Logger factory for diagnostics</param>
    /// <param name="maxFramesInFlight">Maximum number of frames that can be processed simultaneously (default: 2)</param>
    public SyncManager(
        IGraphicsContext context,
        ILoggerFactory loggerFactory,
        int maxFramesInFlight = 2)
    {
        if (maxFramesInFlight < 1)
            throw new ArgumentOutOfRangeException(nameof(maxFramesInFlight), "Must be at least 1");

        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = loggerFactory.CreateLogger(nameof(SyncManager));
        MaxFramesInFlight = maxFramesInFlight;


        _frameSyncs = new FrameSync[MaxFramesInFlight];

        try
        {
            // Create synchronization primitives for each frame
            for (int i = 0; i < MaxFramesInFlight; i++)
            {
                _frameSyncs[i] = CreateFrameSync(i);
            }

        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public int MaxFramesInFlight { get; }

    /// <inheritdoc/>
    public FrameSync GetFrameSync(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= MaxFramesInFlight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                $"Frame index must be in range [0, {MaxFramesInFlight})");
        }

        return _frameSyncs[frameIndex];
    }

    /// <inheritdoc/>
    public ImageSync GetImageSync(uint imageIndex)
    {
        // Thread-safe lazy creation of per-image semaphores
        lock (_imageSyncLock)
        {
            if (_imageSyncs.TryGetValue(imageIndex, out var existing))
            {
                return existing;
            }

            var imageSync = CreateImageSync(imageIndex);
            _imageSyncs[imageIndex] = imageSync;
            return imageSync;
        }
    }

    /// <inheritdoc/>
    public bool WaitForFence(VkFence fence, ulong timeoutNanoseconds = ulong.MaxValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var device = _context.Device;
        var vk = _context.VulkanApi;

        _waitStopwatch.Restart();
        _totalFenceWaits++;

        unsafe
        {
            var fenceHandle = fence;
            var result = vk.WaitForFences(device, 1, &fenceHandle, Vk.True, timeoutNanoseconds);

            _waitStopwatch.Stop();
            _totalFenceWaitTimeMs += _waitStopwatch.Elapsed.TotalMilliseconds;

            if (result == Result.Success)
            {
                return true;
            }
            else if (result == Result.Timeout)
            {
                _fenceWaitTimeouts++;
                return false;
            }
            else
            {
                throw new InvalidOperationException($"Failed to wait for fence: {result}");
            }
        }
    }

    /// <inheritdoc/>
    public bool WaitForFences(VkFence[] fences, bool waitAll = true, ulong timeoutNanoseconds = ulong.MaxValue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (fences == null || fences.Length == 0)
            throw new ArgumentException("Fences array cannot be null or empty", nameof(fences));

        var device = _context.Device;
        var vk = _context.VulkanApi;

        _waitStopwatch.Restart();
        _totalFenceWaits += fences.Length;

        unsafe
        {
            fixed (VkFence* pFences = fences)
            {
                var result = vk.WaitForFences(
                    device,
                    (uint)fences.Length,
                    pFences,
                    waitAll ? Vk.True : Vk.False,
                    timeoutNanoseconds);

                _waitStopwatch.Stop();
                _totalFenceWaitTimeMs += _waitStopwatch.Elapsed.TotalMilliseconds;

                if (result == Result.Success)
                {
                    return true;
                }
                else if (result == Result.Timeout)
                {
                    _fenceWaitTimeouts++;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to wait for fences: {result}");
                }
            }
        }
    }

    /// <inheritdoc/>
    public void ResetFence(VkFence fence)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var device = _context.Device;
        var vk = _context.VulkanApi;

        _totalFenceResets++;

        unsafe
        {
            var fenceHandle = fence;
            var result = vk.ResetFences(device, 1, &fenceHandle);

            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to reset fence: {result}");
            }
        }
    }

    /// <inheritdoc/>
    public void ResetFences(VkFence[] fences)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (fences == null || fences.Length == 0)
            throw new ArgumentException("Fences array cannot be null or empty", nameof(fences));

        var device = _context.Device;
        var vk = _context.VulkanApi;

        _totalFenceResets += fences.Length;

        unsafe
        {
            fixed (VkFence* pFences = fences)
            {
                var result = vk.ResetFences(device, (uint)fences.Length, pFences);

                if (result != Result.Success)
                {
                    throw new InvalidOperationException($"Failed to reset fences: {result}");
                }
            }
        }
    }

    /// <inheritdoc/>
    public void DeviceWaitIdle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var device = _context.Device;
        var vk = _context.VulkanApi;

        _deviceWaitIdleCalls++;

        var result = vk.DeviceWaitIdle(device);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to wait for device idle: {result}");
        }
    }

    /// <inheritdoc/>
    public void QueueWaitIdle(Queue queue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var vk = _context.VulkanApi;

        _queueWaitIdleCalls++;

        var result = vk.QueueWaitIdle(queue);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to wait for queue idle: {result}");
        }
    }

    /// <inheritdoc/>
    public SyncStatistics GetStatistics()
    {
        return new SyncStatistics
        {
            MaxFramesInFlight = MaxFramesInFlight,
            TotalFenceWaits = _totalFenceWaits,
            FenceWaitTimeouts = _fenceWaitTimeouts,
            TotalFenceResets = _totalFenceResets,
            TotalFenceWaitTimeMs = _totalFenceWaitTimeMs,
            DeviceWaitIdleCalls = _deviceWaitIdleCalls,
            QueueWaitIdleCalls = _queueWaitIdleCalls,
            CurrentFrameIndex = (int)(_totalFramesRendered % MaxFramesInFlight),
            TotalFramesRendered = _totalFramesRendered,
            ActiveSemaphoreCount = MaxFramesInFlight * 2,
            ActiveFenceCount = MaxFramesInFlight
        };
    }

    /// <summary>
    /// Increments the total frames rendered counter.
    /// Should be called once per frame after presentation.
    /// </summary>
    internal void IncrementFrameCounter()
    {
        _totalFramesRendered++;
    }

    private ImageSync CreateImageSync(uint imageIndex)
    {
        var device = _context.Device;
        var vk = _context.VulkanApi;

        // Create render finished semaphore (one per swapchain image)
        var semaphoreCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        VkSemaphore renderFinishedSemaphore;

        unsafe
        {
            var result = vk.CreateSemaphore(device, &semaphoreCreateInfo, null, out renderFinishedSemaphore);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create render finished semaphore for image {imageIndex}: {result}");
            }
        }

        return new ImageSync
        {
            RenderFinished = renderFinishedSemaphore,
            ImageIndex = imageIndex
        };
    }

    private FrameSync CreateFrameSync(int frameIndex)
    {
        var device = _context.Device;
        var vk = _context.VulkanApi;

        // Create acquire semaphore (one per frame)
        var semaphoreCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        VkSemaphore imageAvailableSemaphore;

        unsafe
        {
            var result = vk.CreateSemaphore(device, &semaphoreCreateInfo, null, out imageAvailableSemaphore);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create image available semaphore for frame {frameIndex}: {result}");
            }
        }

        // Create fence (start signaled so first frame doesn't wait)
        var fenceCreateInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit // Start in signaled state
        };

        VkFence inFlightFence;

        unsafe
        {
            var result = vk.CreateFence(device, &fenceCreateInfo, null, out inFlightFence);
            if (result != Result.Success)
            {
                vk.DestroySemaphore(device, imageAvailableSemaphore, null);
                throw new InvalidOperationException($"Failed to create in-flight fence for frame {frameIndex}: {result}");
            }
        }

        return new FrameSync
        {
            ImageAvailable = imageAvailableSemaphore,
            InFlightFence = inFlightFence,
            FrameIndex = frameIndex
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;


        // Wait for device to finish all work before destroying sync primitives
        DeviceWaitIdle();

        var device = _context.Device;
        var vk = _context.VulkanApi;

        // Destroy all synchronization primitives
        unsafe
        {
            // Destroy per-frame synchronization (acquire semaphore + fence)
            foreach (var frameSync in _frameSyncs)
            {
                if (frameSync != null)
                {
                    vk.DestroySemaphore(device, frameSync.ImageAvailable, null);
                    vk.DestroyFence(device, frameSync.InFlightFence, null);
                }
            }

            // Destroy per-image render semaphores
            foreach (var imageSync in _imageSyncs.Values)
            {
                vk.DestroySemaphore(device, imageSync.RenderFinished, null);
            }
        }

        var stats = GetStatistics();

        _disposed = true;
    }
}
