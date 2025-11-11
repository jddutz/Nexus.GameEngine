using System.Collections.Concurrent;

namespace Nexus.GameEngine.Graphics.Descriptors;

/// <summary>
/// Implements Vulkan descriptor management: pools, layouts, and sets.
/// Thread-safe implementation using concurrent collections for multi-threaded loading.
/// </summary>
public unsafe class DescriptorManager(IGraphicsContext context) : IDescriptorManager
{    
    // Descriptor pools (create new pools when exhausted)
    private readonly List<DescriptorPool> _descriptorPools = [];
    private int _currentPoolIndex = 0;
    private const uint DescriptorsPerPool = 1000; // How many descriptor sets per pool
    
    // Cached descriptor set layouts (keyed by binding configuration hash)
    private readonly ConcurrentDictionary<int, DescriptorSetLayout> _layoutCache = new();
    
    // Track allocations for debugging/monitoring
    private int _totalSetsAllocated = 0;
    private int _totalPoolsCreated = 0;
    
    /// <inheritdoc/>
    public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutBinding[] bindings)
    {
        if (bindings == null || bindings.Length == 0)
        {
            throw new ArgumentException("Descriptor set layout must have at least one binding", nameof(bindings));
        }
        
        // Compute hash of bindings for caching
        var hash = ComputeBindingsHash(bindings);

        // Return cached layout if exists
        if (_layoutCache.TryGetValue(hash, out var cachedLayout)) return cachedLayout;
        
        // Create new layout
        fixed (DescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = pBindings
            };
            
            DescriptorSetLayout layout;
            var result = context.VulkanApi.CreateDescriptorSetLayout(context.Device, &layoutInfo, null, &layout);
            
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create descriptor set layout: {result}");
            }
            
            _layoutCache.TryAdd(hash, layout);

            return layout;
        }
    }
    
    /// <inheritdoc/>
    public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout layout)
    {
        if (layout.Handle == 0)
        {
            throw new ArgumentException("Invalid descriptor set layout", nameof(layout));
        }
        
        // Get or create descriptor pool
        var pool = GetOrCreatePool();
        
        // Allocate descriptor set from pool
        var layouts = stackalloc DescriptorSetLayout[] { layout };
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts
        };
        
        DescriptorSet descriptorSet;
    var result = context.VulkanApi.AllocateDescriptorSets(context.Device, &allocInfo, &descriptorSet);
        
        if (result == Result.ErrorOutOfPoolMemory || result == Result.ErrorFragmentedPool)
        {
            // Pool exhausted - create new pool and retry
            _currentPoolIndex++;
            pool = GetOrCreatePool();
            allocInfo.DescriptorPool = pool;
            
            result = context.VulkanApi.AllocateDescriptorSets(context.Device, &allocInfo, &descriptorSet);
        }
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to allocate descriptor set: {result}");
        }

        _totalSetsAllocated++;

        return descriptorSet;
    }
    
    /// <inheritdoc/>
    public void UpdateDescriptorSet(DescriptorSet descriptorSet, Silk.NET.Vulkan.Buffer buffer, ulong size, uint binding = 0)
    {
        if (descriptorSet.Handle == 0)
        {
            throw new ArgumentException("Invalid descriptor set", nameof(descriptorSet));
        }
        
        if (buffer.Handle == 0)
        {
            throw new ArgumentException("Invalid buffer", nameof(buffer));
        }
        
        // Describe the buffer binding
        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = buffer,
            Offset = 0,
            Range = size
        };
        
        // Create write descriptor set
        var descriptorWrite = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };
        
        // Update the descriptor set
        context.VulkanApi.UpdateDescriptorSets(context.Device, 1, &descriptorWrite, 0, null);
    }
    
    /// <inheritdoc/>
    public void UpdateDescriptorSet(
        DescriptorSet descriptorSet,
        ImageView imageView,
        Sampler sampler,
        ImageLayout imageLayout,
        uint binding = 0)
    {
        if (descriptorSet.Handle == 0)
        {
            throw new ArgumentException("Invalid descriptor set", nameof(descriptorSet));
        }
        
        if (imageView.Handle == 0)
        {
            throw new ArgumentException("Invalid image view", nameof(imageView));
        }
        
        if (sampler.Handle == 0)
        {
            throw new ArgumentException("Invalid sampler", nameof(sampler));
        }
        
        // Describe the image binding
        var imageInfo = new DescriptorImageInfo
        {
            ImageView = imageView,
            Sampler = sampler,
            ImageLayout = imageLayout
        };
        
        // Create write descriptor set
        var descriptorWrite = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = descriptorSet,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo
        };
        
        // Update the descriptor set
        context.VulkanApi.UpdateDescriptorSets(context.Device, 1, &descriptorWrite, 0, null);
    }
    
    /// <inheritdoc/>
    public void ResetPools()
    {
        
        foreach (var pool in _descriptorPools)
        {
            context.VulkanApi.ResetDescriptorPool(context.Device, pool, 0);
        }
        
        _currentPoolIndex = 0;
        _totalSetsAllocated = 0;
    }
    
    public void Dispose()
    {
        
        // Wait for device to finish
        context.VulkanApi.DeviceWaitIdle(context.Device);
        
        // Destroy descriptor pools
        foreach (var pool in _descriptorPools)
        {
            context.VulkanApi.DestroyDescriptorPool(context.Device, pool, null);
        }
        _descriptorPools.Clear();
        
        // Destroy descriptor set layouts
        foreach (var layout in _layoutCache.Values)
        {
            context.VulkanApi.DestroyDescriptorSetLayout(context.Device, layout, null);
        }
        _layoutCache.Clear();
    }
    
    /// <summary>
    /// Gets the current descriptor pool or creates a new one if needed.
    /// </summary>
    private DescriptorPool GetOrCreatePool()
    {
        // Return existing pool if available
        if (_currentPoolIndex < _descriptorPools.Count)
        {
            return _descriptorPools[_currentPoolIndex];
        }
        
        // Create new pool
        var pool = CreateDescriptorPool();
        _descriptorPools.Add(pool);
        _totalPoolsCreated++;
        
        return pool;
    }
    
    /// <summary>
    /// Creates a new Vulkan descriptor pool.
    /// </summary>
    private DescriptorPool CreateDescriptorPool()
    {
        // Define pool sizes for different descriptor types
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new DescriptorPoolSize
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = DescriptorsPerPool
            },
            new DescriptorPoolSize
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = DescriptorsPerPool
            }
        };
        
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2, // Uniform buffers + combined image samplers
            PPoolSizes = poolSizes,
            MaxSets = DescriptorsPerPool,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit // Allow individual set freeing (optional)
        };
        
        DescriptorPool pool;
        var result = context.VulkanApi.CreateDescriptorPool(context.Device, &poolInfo, null, &pool);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create descriptor pool: {result}");
        }
        
        return pool;
    }
    
    /// <summary>
    /// Computes a hash code for descriptor bindings to use as cache key.
    /// </summary>
    private static int ComputeBindingsHash(DescriptorSetLayoutBinding[] bindings)
    {
        var hash = new HashCode();
        
        foreach (var binding in bindings)
        {
            hash.Add(binding.Binding);
            hash.Add(binding.DescriptorType);
            hash.Add(binding.DescriptorCount);
            hash.Add(binding.StageFlags);
        }
        
        return hash.ToHashCode();
    }
}
