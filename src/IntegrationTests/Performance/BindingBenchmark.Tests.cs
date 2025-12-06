using System.Diagnostics;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Xunit;
using Xunit.Abstractions;
using IntegrationTests.PropertyBinding; // For ManualBindings

namespace IntegrationTests.Performance;

public class BindingBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public BindingBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class BenchmarkComponent : RuntimeComponent
    {
        private float _value;
        public float Value 
        { 
            get => _value; 
            set 
            { 
                float old = _value;
                _value = value; 
                ValueChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value)); 
            } 
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? ValueChanged;
    }

    [Fact]
    public void Benchmark_BindingOverhead()
    {
        const int ITERATIONS = 100_000;
        
        // Setup Direct
        var sourceDirect = new BenchmarkComponent();
        var targetDirect = new BenchmarkComponent();
        
        // Setup Binding
        var sourceBinding = new BenchmarkComponent();
        var targetBinding = new BenchmarkComponent();
        
        var bindings = new ManualBindings();
        bindings.Add("Value", Binding.FromParent<BenchmarkComponent>(p => p.Value));
        
        sourceBinding.Load(new Template());
        sourceBinding.AddChild(targetBinding);
        targetBinding.Load(new Template { Bindings = bindings });
        sourceBinding.Activate();
        
        // Warmup
        for (int i = 0; i < 100; i++)
        {
            sourceDirect.Value = i;
            targetDirect.Value = sourceDirect.Value;
            
            sourceBinding.Value = i;
        }
        
        // Measure Direct
        var swDirect = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            sourceDirect.Value = i;
            targetDirect.Value = sourceDirect.Value;
        }
        swDirect.Stop();
        
        // Measure Binding
        var swBinding = Stopwatch.StartNew();
        for (int i = 0; i < ITERATIONS; i++)
        {
            sourceBinding.Value = i;
        }
        swBinding.Stop();
        
        double directMs = swDirect.Elapsed.TotalMilliseconds;
        double bindingMs = swBinding.Elapsed.TotalMilliseconds;
        double overhead = bindingMs - directMs;
        double overheadPerOp = (overhead * 1000) / ITERATIONS; // microseconds
        
        _output.WriteLine($"Iterations: {ITERATIONS}");
        _output.WriteLine($"Direct: {directMs:F4} ms");
        _output.WriteLine($"Binding: {bindingMs:F4} ms");
        _output.WriteLine($"Overhead: {overhead:F4} ms total, {overheadPerOp:F4} us/op");
        _output.WriteLine($"Ratio: {bindingMs / directMs:F2}x");

        // Assert that overhead is reasonable (e.g. < 10 microseconds per update)
        Assert.True(overheadPerOp < 10.0, $"Overhead per operation ({overheadPerOp:F4} us) should be < 10 us");
    }
}
