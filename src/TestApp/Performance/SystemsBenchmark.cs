using System.Diagnostics;
using Nexus.GameEngine.Runtime.Systems;
using Nexus.GameEngine.Runtime.Extensions;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Graphics.Commands;
using Silk.NET.Vulkan;

namespace TestApp.Performance;

public class SystemsBenchmark
{
    private const int Iterations = 10_000_000;
    private readonly IGraphicsSystem _graphicsSystem;
    private readonly GraphicsSystem _internalGraphicsSystem;
    private readonly PipelineDescriptor _descriptor;

    // Manual stubs
    private class StubPipelineManager : IPipelineManager
    {
        private readonly Dictionary<string, PipelineHandle> _cache = new();

        public PipelineHandle GetOrCreatePipeline(PipelineDescriptor descriptor) 
        {
            if (!_cache.TryGetValue(descriptor.Name, out var handle))
            {
                handle = new PipelineHandle(default, default, "Stub");
                _cache[descriptor.Name] = handle;
            }
            return handle;
        }
        public PipelineHandle Get(string name) => throw new NotImplementedException();
        public void Dispose() { }
        
        public PipelineHandle GetOrCreate(PipelineDefinition definition) => throw new NotImplementedException();
        public IPipelineBuilder GetBuilder() => throw new NotImplementedException();
        public bool InvalidatePipeline(string name) => throw new NotImplementedException();
        public int InvalidatePipelinesUsingShader(string shaderName) => throw new NotImplementedException();
        public void ReloadAllShaders() => throw new NotImplementedException();
        public PipelineStatistics GetStatistics() => throw new NotImplementedException();
        public IEnumerable<PipelineInfo> GetAllPipelines() => throw new NotImplementedException();
        public bool ValidatePipelineDescriptor(PipelineDescriptor descriptor) => throw new NotImplementedException();
        public PipelineHandle GetErrorPipeline(RenderPass renderPass) => throw new NotImplementedException();
    }

    private class StubServiceProvider : IServiceProvider
    {
        private readonly IPipelineManager _pipelineManager = new StubPipelineManager();
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IPipelineManager)) return _pipelineManager;
            return null;
        }
    }

    public SystemsBenchmark()
    {
        var stubServiceProvider = new StubServiceProvider();
        var stubPipelineManager = new StubPipelineManager();

        // Create system
        _internalGraphicsSystem = new GraphicsSystem(
            null!, // Context
            stubServiceProvider,
            stubPipelineManager,
            null!, // DescriptorManager
            null!, // SwapChain
            null!, // SyncManager
            null!  // CommandPoolManager
        );
        _graphicsSystem = _internalGraphicsSystem;
        
        _descriptor = new PipelineDescriptor 
        { 
            Name = "Benchmark",
            VertexInputDescription = new VertexInputDescription 
            { 
                Bindings = [], 
                Attributes = [] 
            },
            RenderPass = new RenderPass()
        };
    }

    public void Run()
    {
        Console.WriteLine($"Running Systems Benchmark ({Iterations:N0} iterations)...");
        
        // Warmup
        DirectCall();
        ExtensionCall();

        // Benchmark Direct
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            DirectCall();
        }
        sw.Stop();
        var directTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Direct Call: {directTime}ms");

        // Benchmark Extension
        sw.Restart();
        for (int i = 0; i < Iterations; i++)
        {
            ExtensionCall();
        }
        sw.Stop();
        var extensionTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"Extension Call: {extensionTime}ms");
        
        // Analysis
        var diff = extensionTime - directTime;
        var percent = (double)diff / directTime * 100;
        Console.WriteLine($"Difference: {diff}ms ({percent:F2}%)");
        
        if (Math.Abs(percent) < 5.0)
        {
            Console.WriteLine("RESULT: PASS (Difference < 5%)");
        }
        else
        {
            Console.WriteLine("RESULT: WARNING (Difference > 5%)");
        }
    }

    private void DirectCall()
    {
        _internalGraphicsSystem.PipelineManager.GetOrCreatePipeline(_descriptor);
    }

    private void ExtensionCall()
    {
        _graphicsSystem.GetPipeline(_descriptor);
    }
}
