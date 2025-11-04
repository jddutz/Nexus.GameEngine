using System.Collections.Concurrent;

namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Central manager for command pools across different queue families.
/// Thread-safe pool creation and management.
/// </summary>
public class CommandPoolManager(IGraphicsContext context) : ICommandPoolManager
{
    // Cached pools by type
    private readonly ConcurrentDictionary<CommandPoolType, ICommandPool> _poolsByType = new();
    
    // All managed pools (including custom ones)
    private readonly List<ICommandPool> _allPools = [];
    private readonly object _poolsLock = new();

    private bool _disposed;

    /// <inheritdoc/>
    public ICommandPool GetOrCreatePool(CommandPoolType type)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _poolsByType.GetOrAdd(type, t =>
        {
            var pool = CreatePoolForType(t);
            
            lock (_poolsLock)
            {
                _allPools.Add(pool);
            }
            
            return pool;
        });
    }

    /// <inheritdoc/>
    public ICommandPool CreatePool(uint queueFamilyIndex, bool allowIndividualReset = false, bool transient = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pool = new CommandPool(
            context,
            queueFamilyIndex,
            allowIndividualReset,
            transient);

        lock (_poolsLock)
        {
            _allPools.Add(pool);
        }

        return pool;
    }

    /// <inheritdoc/>
    public ICommandPool GraphicsPool => GetOrCreatePool(CommandPoolType.Graphics);

    /// <inheritdoc/>
    public ICommandPool? TransferPool
    {
        get
        {
            // TODO: Check if device has dedicated transfer queue
            // For now, return null - most devices use graphics queue for transfers
            return null;
        }
    }

    /// <inheritdoc/>
    public ICommandPool? ComputePool
    {
        get
        {
            // TODO: Check if device has dedicated compute queue
            // For now, return null - most devices use graphics queue for compute
            return null;
        }
    }

    /// <inheritdoc/>
    public void ResetAll(CommandPoolResetFlags flags = CommandPoolResetFlags.None)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);


        lock (_poolsLock)
        {
            foreach (var pool in _allPools)
            {
                pool.Reset(flags);
            }
        }

    }

    /// <inheritdoc/>
    public void TrimAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);


        lock (_poolsLock)
        {
            foreach (var pool in _allPools)
            {
                pool.Trim();
            }
        }

    }

    /// <inheritdoc/>
    public CommandPoolManagerStatistics GetStatistics()
    {
        lock (_poolsLock)
        {
            var allStats = _allPools.Select(p => p.GetStatistics()).ToList();

            return new CommandPoolManagerStatistics
            {
                TotalPools = allStats.Count,
                TotalAllocatedBuffers = allStats.Sum(s => s.TotalAllocatedBuffers),
                TotalPrimaryBuffers = allStats.Sum(s => s.PrimaryBufferCount),
                TotalSecondaryBuffers = allStats.Sum(s => s.SecondaryBufferCount),
                TotalAllocationRequests = allStats.Sum(s => s.TotalAllocationRequests),
                TotalFreeRequests = allStats.Sum(s => s.TotalFreeRequests),
                TotalResets = allStats.Sum(s => s.ResetCount),
                TotalEstimatedMemoryBytes = allStats.Sum(s => s.EstimatedMemoryUsageBytes),
                GraphicsPoolCount = _poolsByType.ContainsKey(CommandPoolType.Graphics) ? 1 : 0,
                TransferPoolCount = _poolsByType.ContainsKey(CommandPoolType.Transfer) ? 1 : 0,
                ComputePoolCount = _poolsByType.ContainsKey(CommandPoolType.Compute) ? 1 : 0
            };
        }
    }

    /// <inheritdoc/>
    public IEnumerable<(CommandPoolType Type, CommandPoolStatistics Stats)> GetAllPoolStatistics()
    {
        lock (_poolsLock)
        {
            foreach (var kvp in _poolsByType)
            {
                yield return (kvp.Key, kvp.Value.GetStatistics());
            }
        }
    }

    /// <summary>
    /// Creates a command pool for the specified type.
    /// </summary>
    private ICommandPool CreatePoolForType(CommandPoolType type)
    {
        return type switch
        {
            CommandPoolType.Graphics => CreateGraphicsPool(transient: false),
            CommandPoolType.TransientGraphics => CreateGraphicsPool(transient: true),
            CommandPoolType.Transfer => CreateTransferPool(),
            CommandPoolType.Compute => CreateComputePool(),
            _ => throw new ArgumentException($"Unknown command pool type: {type}", nameof(type))
        };
    }

    /// <summary>
    /// Creates a command pool for graphics queue.
    /// </summary>
    private ICommandPool CreateGraphicsPool(bool transient)
    {
        // TODO: Get actual graphics queue family index from Context
        // For now, assuming queue family 0 is graphics
        uint queueFamilyIndex = 0;

        return new CommandPool(
            context,
            queueFamilyIndex,
            allowIndividualReset: false, // Typically reset entire pool for graphics
            transient: transient);
    }

    /// <summary>
    /// Creates a command pool for transfer queue.
    /// </summary>
    private ICommandPool CreateTransferPool()
    {
        // TODO: Get dedicated transfer queue family index
        // For now, fall back to graphics queue
        uint queueFamilyIndex = 0;


        return new CommandPool(
            context,
            queueFamilyIndex,
            allowIndividualReset: false,
            transient: true);
    }

    /// <summary>
    /// Creates a command pool for compute queue.
    /// </summary>
    private ICommandPool CreateComputePool()
    {
        // TODO: Get dedicated compute queue family index
        // For now, fall back to graphics queue
        uint queueFamilyIndex = 0;


        return new CommandPool(
            context,
            queueFamilyIndex,
            allowIndividualReset: false,
            transient: false);
    }

    public void Dispose()
    {
        if (_disposed)
            return;


        lock (_poolsLock)
        {
            foreach (var pool in _allPools)
            {
                pool.Dispose();
            }

            _allPools.Clear();
            _poolsByType.Clear();
        }

        _disposed = true;
        GC.SuppressFinalize(this);

    }
}
