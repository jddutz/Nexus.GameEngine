using Microsoft.Extensions.Logging;
using VkCommandPool = Silk.NET.Vulkan.CommandPool;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Vulkan command pool implementation for command buffer allocation and management.
/// Thread-specific pool for allocating command buffers from a specific queue family.
/// </summary>
/// <remarks>
/// <para><strong>Design Notes:</strong></para>
/// <list type="bullet">
/// <item>One instance per thread for thread safety</item>
/// <item>Optimized for single-use command buffers (transient flag)</item>
/// <item>Tracks allocations for statistics and debugging</item>
/// <item>Supports both primary and secondary command buffers</item>
/// </list>
/// </remarks>
public unsafe class CommandPool : ICommandPool
{
    private readonly ILogger _logger;
    private readonly IGraphicsContext _context;
    private readonly Vk _vk;
    private readonly uint _queueFamilyIndex;
    private readonly bool _allowIndividualReset;

    private VkCommandPool _pool;
    private readonly DateTime _createdAt;
    private DateTime? _lastResetAt;

    // Statistics tracking
    private int _totalAllocatedBuffers;
    private int _primaryBufferCount;
    private int _secondaryBufferCount;
    private int _totalAllocationRequests;
    private int _totalFreeRequests;
    private int _resetCount;
    private int _trimCount;

    // Track allocated buffers for cleanup
    private readonly List<CommandBuffer> _allocatedBuffers = [];
    private bool _disposed;

    /// <summary>
    /// Creates a new command pool for the specified queue family.
    /// </summary>
    /// <param name="context">Graphics context providing Vulkan device access</param>
    /// <param name="queueFamilyIndex">Queue family index for this pool</param>
    /// <param name="allowIndividualReset">Whether individual command buffers can be reset</param>
    /// <param name="transient">Whether buffers are short-lived (optimization hint)</param>
    /// <param name="loggerFactory">Logger factory for diagnostic output</param>
    public CommandPool(
        IGraphicsContext context,
        uint queueFamilyIndex,
        bool allowIndividualReset,
        bool transient,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger($"{nameof(CommandPool)}:QF{queueFamilyIndex}");
        _context = context;
        _vk = context.VulkanApi;
        _queueFamilyIndex = queueFamilyIndex;
        _allowIndividualReset = allowIndividualReset;
        _createdAt = DateTime.UtcNow;

        CreateCommandPool(transient);
    }

    /// <inheritdoc/>
    public VkCommandPool Pool => _pool;

    /// <inheritdoc/>
    public uint QueueFamilyIndex => _queueFamilyIndex;

    /// <inheritdoc/>
    public bool AllowIndividualReset => _allowIndividualReset;

    /// <inheritdoc/>
    public CommandBuffer[] AllocateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (count == 0)
        {
            return Array.Empty<CommandBuffer>();
        }

        _totalAllocationRequests++;

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _pool,
            Level = level,
            CommandBufferCount = count
        };

        var commandBuffers = new CommandBuffer[count];
        fixed (CommandBuffer* pCommandBuffers = commandBuffers)
        {
            var result = _vk.AllocateCommandBuffers(_context.Device, &allocInfo, pCommandBuffers);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to allocate command buffers: {result}");
            }
        }

        // Track allocations
        _totalAllocatedBuffers += (int)count;
        if (level == CommandBufferLevel.Primary)
            _primaryBufferCount += (int)count;
        else
            _secondaryBufferCount += (int)count;

        lock (_allocatedBuffers)
        {
            _allocatedBuffers.AddRange(commandBuffers);
        }

        return commandBuffers;
    }

    /// <inheritdoc/>
    public void FreeCommandBuffers(CommandBuffer[] commandBuffers)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (commandBuffers == null || commandBuffers.Length == 0)
        {
            return;
        }

        _totalFreeRequests++;

        fixed (CommandBuffer* pCommandBuffers = commandBuffers)
        {
            _vk.FreeCommandBuffers(_context.Device, _pool, (uint)commandBuffers.Length, pCommandBuffers);
        }

        // Update tracking
        _totalAllocatedBuffers -= commandBuffers.Length;
        
        lock (_allocatedBuffers)
        {
            foreach (var buffer in commandBuffers)
            {
                _allocatedBuffers.Remove(buffer);
            }
        }
    }

    /// <inheritdoc/>
    public void Reset(CommandPoolResetFlags flags = CommandPoolResetFlags.None)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = _vk.ResetCommandPool(_context.Device, _pool, flags);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to reset command pool: {result}");
        }

        _resetCount++;
        _lastResetAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public void Trim()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // vkTrimCommandPool is a Vulkan 1.1 feature
        // For now, just track the call - actual implementation requires checking feature support
        _trimCount++;

    }

    /// <inheritdoc/>
    public CommandPoolStatistics GetStatistics()
    {
        return new CommandPoolStatistics
        {
            TotalAllocatedBuffers = _totalAllocatedBuffers,
            PrimaryBufferCount = _primaryBufferCount,
            SecondaryBufferCount = _secondaryBufferCount,
            TotalAllocationRequests = _totalAllocationRequests,
            TotalFreeRequests = _totalFreeRequests,
            ResetCount = _resetCount,
            TrimCount = _trimCount,
            EstimatedMemoryUsageBytes = _totalAllocatedBuffers * 1024, // Rough estimate
            QueueFamilyIndex = _queueFamilyIndex,
            AllowsIndividualReset = _allowIndividualReset,
            CreatedAt = _createdAt,
            LastResetAt = _lastResetAt
        };
    }

    /// <summary>
    /// Creates the native Vulkan command pool.
    /// </summary>
    private void CreateCommandPool(bool transient)
    {
        var flags = CommandPoolCreateFlags.None;

        if (_allowIndividualReset)
            flags |= CommandPoolCreateFlags.ResetCommandBufferBit;

        if (transient)
            flags |= CommandPoolCreateFlags.TransientBit;

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _queueFamilyIndex,
            Flags = flags
        };

        VkCommandPool pool;
        var result = _vk.CreateCommandPool(_context.Device, &poolInfo, null, out pool);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create command pool: {result}");
        }

        _pool = pool;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Free all tracked command buffers
        if (_allocatedBuffers.Count > 0)
        {
            
            lock (_allocatedBuffers)
            {
                var buffers = _allocatedBuffers.ToArray();
                if (buffers.Length > 0)
                {
                    fixed (CommandBuffer* pBuffers = buffers)
                    {
                        _vk.FreeCommandBuffers(_context.Device, _pool, (uint)buffers.Length, pBuffers);
                    }
                }
                _allocatedBuffers.Clear();
            }
        }

        // Destroy the command pool
        if (_pool.Handle != 0)
        {
            _vk.DestroyCommandPool(_context.Device, _pool, null);
            _pool = default;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

    }
}
