using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Runtime;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Manages Vulkan graphics pipelines with caching, lifecycle management, and hot-reload support.
/// Thread-safe implementation using ConcurrentDictionary for multi-threaded loading.
/// Subscribes to window resize events for automatic pipeline invalidation.
/// </summary>
public unsafe class PipelineManager : IPipelineManager
{
    private readonly ILogger _logger;
    private readonly IGraphicsContext _context;
    private readonly IWindowService _windowService;
    private readonly Vk _vk;

    // Thread-safe pipeline cache
    private readonly ConcurrentDictionary<string, CachedPipeline> _pipelines = new();
    
    // Track shader dependencies for invalidation
    private readonly ConcurrentDictionary<string, HashSet<string>> _shaderToPipelines = new();

    // Statistics tracking
    private int _totalCreateRequests;
    private int _cacheHits;
    private int _cacheMisses;
    private int _compilationFailures;
    private int _invalidationCount;
    private double _totalCreationTimeMs;

    // Error pipeline for visual debugging
    private Pipeline? _errorPipeline;

    public PipelineManager(
        IGraphicsContext context,
        IWindowService windowService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(PipelineManager));
        _context = context;
        _windowService = windowService;
        _vk = _context.VulkanApi;

        // Subscribe to window resize events
        var window = _windowService.GetWindow();
        window.Resize += OnWindowResize;

        _logger.LogDebug("PipelineManager initialized and subscribed to window resize events");
    }

    /// <inheritdoc/>
    public Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        Interlocked.Increment(ref _totalCreateRequests);

        // Try to get from cache
        if (_pipelines.TryGetValue(descriptor.Name, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            cached.AccessCount++;
            cached.LastAccessedAt = DateTime.UtcNow;
            _logger.LogTrace("Pipeline cache hit: {PipelineName}", descriptor.Name);
            return cached.Handle;
        }

        // Cache miss - create new pipeline
        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("Creating new pipeline: {PipelineName}", descriptor.Name);

        var stopwatch = Stopwatch.StartNew();
        var pipeline = CreatePipeline(descriptor);
        stopwatch.Stop();

        _totalCreationTimeMs += stopwatch.Elapsed.TotalMilliseconds;

        if (pipeline.Handle == 0)
        {
            Interlocked.Increment(ref _compilationFailures);
            _logger.LogError("Pipeline creation failed: {PipelineName}, returning error pipeline", descriptor.Name);
            return GetErrorPipeline(descriptor.RenderPass);
        }

        // Cache the pipeline
        var cachedPipeline = new CachedPipeline
        {
            Descriptor = descriptor,
            Handle = pipeline,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            AccessCount = 1
        };

        _pipelines.TryAdd(descriptor.Name, cachedPipeline);

        // Track shader dependencies
        TrackShaderDependency(descriptor.VertexShaderPath, descriptor.Name);
        TrackShaderDependency(descriptor.FragmentShaderPath, descriptor.Name);
        if (descriptor.GeometryShaderPath != null)
            TrackShaderDependency(descriptor.GeometryShaderPath, descriptor.Name);

        _logger.LogInformation(
            "Pipeline created: {PipelineName} in {CreationTimeMs:F2}ms (Total: {CacheMisses} created, {CacheHits} cached)",
            descriptor.Name, stopwatch.Elapsed.TotalMilliseconds, _cacheMisses, _cacheHits);

        return pipeline;
    }

    /// <inheritdoc/>
    public Pipeline GetSpritePipeline(RenderPass renderPass)
    {
        var descriptor = new PipelineDescriptor
        {
            Name = "SpritePipeline",
            VertexShaderPath = "shaders/sprite.vert.spv",
            FragmentShaderPath = "shaders/sprite.frag.spv",
            VertexInputDescription = GetSpriteVertexDescription(),
            Topology = PrimitiveTopology.TriangleList,
            RenderPass = renderPass,
            EnableDepthTest = false,
            EnableDepthWrite = false,
            EnableBlending = true,
            SrcBlendFactor = BlendFactor.SrcAlpha,
            DstBlendFactor = BlendFactor.OneMinusSrcAlpha,
            CullMode = CullModeFlags.None
        };

        return GetOrCreatePipeline(descriptor);
    }

    /// <inheritdoc/>
    public Pipeline GetMeshPipeline(RenderPass renderPass)
    {
        var descriptor = new PipelineDescriptor
        {
            Name = "MeshPipeline",
            VertexShaderPath = "shaders/mesh.vert.spv",
            FragmentShaderPath = "shaders/mesh.frag.spv",
            VertexInputDescription = GetMeshVertexDescription(),
            Topology = PrimitiveTopology.TriangleList,
            RenderPass = renderPass,
            EnableDepthTest = true,
            EnableDepthWrite = true,
            DepthCompareOp = CompareOp.Less,
            EnableBlending = false,
            CullMode = CullModeFlags.BackBit
        };

        return GetOrCreatePipeline(descriptor);
    }

    /// <inheritdoc/>
    public Pipeline GetUIPipeline(RenderPass renderPass)
    {
        var descriptor = new PipelineDescriptor
        {
            Name = "UIPipeline",
            VertexShaderPath = "shaders/ui.vert.spv",
            FragmentShaderPath = "shaders/ui.frag.spv",
            VertexInputDescription = GetUIVertexDescription(),
            Topology = PrimitiveTopology.TriangleList,
            RenderPass = renderPass,
            EnableDepthTest = false,
            EnableDepthWrite = false,
            EnableBlending = true,
            SrcBlendFactor = BlendFactor.SrcAlpha,
            DstBlendFactor = BlendFactor.OneMinusSrcAlpha,
            CullMode = CullModeFlags.None
        };

        return GetOrCreatePipeline(descriptor);
    }

    /// <inheritdoc/>
    public bool InvalidatePipeline(string pipelineName)
    {
        if (_pipelines.TryRemove(pipelineName, out var cached))
        {
            Interlocked.Increment(ref _invalidationCount);
            DestroyPipeline(cached);
            _logger.LogDebug("Pipeline invalidated: {PipelineName}", pipelineName);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public int InvalidatePipelinesUsingShader(string shaderPath)
    {
        if (!_shaderToPipelines.TryGetValue(shaderPath, out var pipelineNames))
            return 0;

        int count = 0;
        foreach (var pipelineName in pipelineNames)
        {
            if (InvalidatePipeline(pipelineName))
                count++;
        }

        _logger.LogInformation(
            "Invalidated {Count} pipelines using shader: {ShaderPath}",
            count, shaderPath);

        return count;
    }

    /// <inheritdoc/>
    public void ReloadAllShaders()
    {
        _logger.LogInformation("Reloading all shaders - waiting for GPU idle");

        // Wait for GPU to finish all operations
        _vk.DeviceWaitIdle(_context.Device);

        // Destroy all pipelines
        foreach (var cached in _pipelines.Values)
        {
            DestroyPipeline(cached);
        }

        // Clear caches
        _pipelines.Clear();
        _shaderToPipelines.Clear();

        _logger.LogInformation("All pipelines destroyed - will be recreated on next access");
    }

    /// <inheritdoc/>
    public PipelineStatistics GetStatistics()
    {
        return new PipelineStatistics
        {
            CachedPipelineCount = _pipelines.Count,
            TotalCreateRequests = _totalCreateRequests,
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            CompilationFailures = _compilationFailures,
            InvalidationCount = _invalidationCount,
            TotalCreationTimeMs = _totalCreationTimeMs,
            EstimatedMemoryUsageBytes = _pipelines.Count * 1024 // Rough estimate
        };
    }

    /// <inheritdoc/>
    public IEnumerable<PipelineInfo> GetAllPipelines()
    {
        foreach (var kvp in _pipelines)
        {
            var cached = kvp.Value;
            yield return new PipelineInfo
            {
                Name = kvp.Key,
                Handle = cached.Handle,
                VertexShaderPath = cached.Descriptor.VertexShaderPath,
                FragmentShaderPath = cached.Descriptor.FragmentShaderPath,
                GeometryShaderPath = cached.Descriptor.GeometryShaderPath,
                AccessCount = cached.AccessCount,
                CreatedAt = cached.CreatedAt,
                LastAccessedAt = cached.LastAccessedAt,
                EstimatedMemoryUsageBytes = 1024, // Rough estimate
                IsSpecialized = false,
                Topology = cached.Descriptor.Topology,
                DepthTestEnabled = cached.Descriptor.EnableDepthTest,
                BlendingEnabled = cached.Descriptor.EnableBlending
            };
        }
    }

    /// <inheritdoc/>
    public bool ValidatePipelineDescriptor(PipelineDescriptor descriptor)
    {
        // TODO: Implement proper validation
        // - Check shader file exists
        // - Validate vertex input matches shader expectations
        // - Check render pass compatibility
        // - Validate descriptor set layouts

        if (string.IsNullOrEmpty(descriptor.Name))
        {
            _logger.LogError("Pipeline descriptor has empty name");
            return false;
        }

        if (string.IsNullOrEmpty(descriptor.VertexShaderPath))
        {
            _logger.LogError("Pipeline descriptor has empty vertex shader path");
            return false;
        }

        if (string.IsNullOrEmpty(descriptor.FragmentShaderPath))
        {
            _logger.LogError("Pipeline descriptor has empty fragment shader path");
            return false;
        }

        if (descriptor.RenderPass.Handle == 0)
        {
            _logger.LogError("Pipeline descriptor has invalid render pass");
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public Pipeline GetErrorPipeline(RenderPass renderPass)
    {
        // TODO: Create actual error pipeline with pink/magenta shader
        // For now, return null handle
        if (_errorPipeline == null || _errorPipeline.Value.Handle == 0)
        {
            _logger.LogWarning("Error pipeline not yet implemented");
        }
        return _errorPipeline ?? default;
    }

    /// <summary>
    /// Handles window resize events.
    /// Invalidates pipelines that depend on viewport dimensions.
    /// </summary>
    private void OnWindowResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        _logger.LogDebug("Window resized to {Width}x{Height} - checking for viewport-dependent pipelines", 
            newSize.X, newSize.Y);

        // TODO: Only invalidate pipelines that have viewport-dependent state
        // For now, we'll let pipelines with DynamicViewport=true handle it themselves
        // Static viewport pipelines would need to be recreated here
    }

    /// <summary>
    /// Creates a Vulkan graphics pipeline from a descriptor.
    /// </summary>
    private Pipeline CreatePipeline(PipelineDescriptor descriptor)
    {
        if (!ValidatePipelineDescriptor(descriptor))
        {
            return default;
        }

        try
        {
            // Load and create shader modules
            var vertShaderModule = CreateShaderModule(descriptor.VertexShaderPath);
            var fragShaderModule = CreateShaderModule(descriptor.FragmentShaderPath);

            if (vertShaderModule.Handle == 0 || fragShaderModule.Handle == 0)
            {
                _logger.LogError("Failed to create shader modules for pipeline: {PipelineName}", descriptor.Name);
                return default;
            }

            // Define shader stages
            var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
            
            shaderStages[0] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            shaderStages[1] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            // Vertex input state
            var vertexInputInfo = CreateVertexInputState(descriptor.VertexInputDescription);

            // Input assembly state
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = descriptor.Topology,
                PrimitiveRestartEnable = false
            };

            // Viewport and scissor (dynamic or static)
            var viewportState = CreateViewportState(descriptor);

            // Rasterization state
            var rasterizer = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = descriptor.PolygonMode,
                LineWidth = descriptor.LineWidth,
                CullMode = descriptor.CullMode,
                FrontFace = descriptor.FrontFace,
                DepthBiasEnable = false
            };

            // Multisampling state (no MSAA for now)
            var multisampling = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };

            // Depth/stencil state
            var depthStencil = CreateDepthStencilState(descriptor);

            // Color blend state
            var colorBlendAttachment = CreateColorBlendAttachment(descriptor);
            var colorBlending = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment
            };

            // Dynamic state
            var dynamicStates = stackalloc DynamicState[2];
            dynamicStates[0] = DynamicState.Viewport;
            dynamicStates[1] = DynamicState.Scissor;

            var dynamicState = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };

            // Pipeline layout (TODO: Add descriptor sets and push constants)
            var pipelineLayoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 0,
                PushConstantRangeCount = 0
            };

            PipelineLayout pipelineLayout;
            var result = _vk.CreatePipelineLayout(_context.Device, &pipelineLayoutInfo, null, &pipelineLayout);
            if (result != Result.Success)
            {
                _logger.LogError("Failed to create pipeline layout: {Result}", result);
                CleanupShaderModules(vertShaderModule, fragShaderModule, shaderStages);
                return default;
            }

            // Create graphics pipeline
            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicState,
                Layout = pipelineLayout,
                RenderPass = descriptor.RenderPass,
                Subpass = descriptor.Subpass,
                BasePipelineHandle = default
            };

            Pipeline pipeline;
            result = _vk.CreateGraphicsPipelines(
                _context.Device,
                default,
                1,
                &pipelineInfo,
                null,
                &pipeline);

            // Cleanup shader modules (no longer needed after pipeline creation)
            CleanupShaderModules(vertShaderModule, fragShaderModule, shaderStages);

            if (result != Result.Success)
            {
                _logger.LogError("Failed to create graphics pipeline: {Result}", result);
                _vk.DestroyPipelineLayout(_context.Device, pipelineLayout, null);
                return default;
            }

            _logger.LogDebug("Successfully created pipeline: {PipelineName}", descriptor.Name);
            return pipeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating pipeline: {PipelineName}", descriptor.Name);
            return default;
        }
    }

    /// <summary>
    /// Creates a Vulkan shader module from a SPIR-V file.
    /// </summary>
    private ShaderModule CreateShaderModule(string shaderPath)
    {
        try
        {
            if (!File.Exists(shaderPath))
            {
                _logger.LogError("Shader file not found: {ShaderPath}", shaderPath);
                return default;
            }

            var code = File.ReadAllBytes(shaderPath);
            
            fixed (byte* codePtr = code)
            {
                var createInfo = new ShaderModuleCreateInfo
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)code.Length,
                    PCode = (uint*)codePtr
                };

                ShaderModule shaderModule;
                var result = _vk.CreateShaderModule(_context.Device, &createInfo, null, &shaderModule);
                
                if (result != Result.Success)
                {
                    _logger.LogError("Failed to create shader module from {ShaderPath}: {Result}", 
                        shaderPath, result);
                    return default;
                }

                return shaderModule;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating shader module from {ShaderPath}", shaderPath);
            return default;
        }
    }

    /// <summary>
    /// Creates vertex input state from descriptor.
    /// </summary>
    private PipelineVertexInputStateCreateInfo CreateVertexInputState(VertexInputDescription vertexInput)
    {
        // TODO: Properly allocate and pin binding/attribute descriptions
        // For now, return empty state
        return new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0
        };
    }

    /// <summary>
    /// Creates viewport state.
    /// </summary>
    private PipelineViewportStateCreateInfo CreateViewportState(PipelineDescriptor descriptor)
    {
        // Using dynamic viewport/scissor, so we just specify count
        return new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1
        };
    }

    /// <summary>
    /// Creates depth/stencil state from descriptor.
    /// </summary>
    private PipelineDepthStencilStateCreateInfo CreateDepthStencilState(PipelineDescriptor descriptor)
    {
        return new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = descriptor.EnableDepthTest,
            DepthWriteEnable = descriptor.EnableDepthWrite,
            DepthCompareOp = descriptor.DepthCompareOp,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false
        };
    }

    /// <summary>
    /// Creates color blend attachment state from descriptor.
    /// </summary>
    private PipelineColorBlendAttachmentState CreateColorBlendAttachment(PipelineDescriptor descriptor)
    {
        return new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | 
                           ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = descriptor.EnableBlending,
            SrcColorBlendFactor = descriptor.SrcBlendFactor,
            DstColorBlendFactor = descriptor.DstBlendFactor,
            ColorBlendOp = descriptor.BlendOp,
            SrcAlphaBlendFactor = descriptor.SrcBlendFactor,
            DstAlphaBlendFactor = descriptor.DstBlendFactor,
            AlphaBlendOp = descriptor.BlendOp
        };
    }

    /// <summary>
    /// Cleans up shader modules after pipeline creation.
    /// </summary>
    private void CleanupShaderModules(ShaderModule vert, ShaderModule frag, PipelineShaderStageCreateInfo* stages)
    {
        if (vert.Handle != 0)
            _vk.DestroyShaderModule(_context.Device, vert, null);
        
        if (frag.Handle != 0)
            _vk.DestroyShaderModule(_context.Device, frag, null);

        // Free string pointers
        if (stages != null)
        {
            SilkMarshal.Free((nint)stages[0].PName);
            SilkMarshal.Free((nint)stages[1].PName);
        }
    }

    /// <summary>
    /// Tracks shader dependency for invalidation.
    /// </summary>
    private void TrackShaderDependency(string shaderPath, string pipelineName)
    {
        _shaderToPipelines.AddOrUpdate(
            shaderPath,
            _ => new HashSet<string> { pipelineName },
            (_, set) =>
            {
                lock (set)
                {
                    set.Add(pipelineName);
                }
                return set;
            });
    }

    /// <summary>
    /// Destroys a cached pipeline and its resources.
    /// </summary>
    private void DestroyPipeline(CachedPipeline cached)
    {
        if (cached.Handle.Handle != 0)
        {
            _vk.DestroyPipeline(_context.Device, cached.Handle, null);
        }
    }

    /// <summary>
    /// Gets sprite vertex description (pos, uv, color).
    /// </summary>
    private static VertexInputDescription GetSpriteVertexDescription()
    {
        // TODO: Implement actual vertex descriptions
        return new VertexInputDescription
        {
            Bindings = [],
            Attributes = []
        };
    }

    /// <summary>
    /// Gets mesh vertex description (pos, normal, uv, tangent).
    /// </summary>
    private static VertexInputDescription GetMeshVertexDescription()
    {
        // TODO: Implement actual vertex descriptions
        return new VertexInputDescription
        {
            Bindings = [],
            Attributes = []
        };
    }

    /// <summary>
    /// Gets UI vertex description (pos, uv, color).
    /// </summary>
    private static VertexInputDescription GetUIVertexDescription()
    {
        // TODO: Implement actual vertex descriptions
        return new VertexInputDescription
        {
            Bindings = [],
            Attributes = []
        };
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing PipelineManager - destroying {Count} cached pipelines", _pipelines.Count);

        // Unsubscribe from window events
        try
        {
            var window = _windowService.GetWindow();
            window.Resize -= OnWindowResize;
        }
        catch
        {
            // Window may already be disposed
        }

        // Wait for GPU idle before destroying pipelines
        _vk.DeviceWaitIdle(_context.Device);

        // Destroy all cached pipelines
        foreach (var cached in _pipelines.Values)
        {
            DestroyPipeline(cached);
        }

        _pipelines.Clear();
        _shaderToPipelines.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Internal cache entry for a pipeline.
    /// </summary>
    private class CachedPipeline
    {
        public required PipelineDescriptor Descriptor { get; init; }
        public required Pipeline Handle { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
    }
}
