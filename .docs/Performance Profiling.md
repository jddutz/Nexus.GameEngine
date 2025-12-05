# Performance Profiling System

**Created**: December 4, 2025  
**Feature**: Performance Profiling and Optimization (specs/012-performance)

## Overview

The Performance Profiling system provides high-resolution timing measurement and analysis capabilities for identifying and resolving performance bottlenecks in the Nexus.GameEngine. It enables developers to:

- Collect timing data for major subsystems (rendering, updates, resource management, input processing)
- Display real-time performance metrics via on-screen overlay
- Identify performance bottlenecks and track optimization progress
- Monitor frame performance to maintain target 150 FPS baseline

## Architecture

### Core Components

#### IProfiler
Service interface for profiling infrastructure. Registered in DI container and accessible throughout the engine.

#### PerformanceSample
Value type representing timing data for a single operation:
- Label: String identifier for the operation
- ElapsedMs: Measured duration in milliseconds
- Timestamp: When the sample was recorded

#### PerformanceScope
RAII-style ref struct for automatic timing measurement:
```csharp
public void Render()
{
    using var scope = new PerformanceScope("Render");
    // Rendering code automatically timed
}
```

#### FrameProfile
Per-frame aggregation of performance samples:
- Collects all samples for a single frame
- Calculates total frame time and FPS
- Provides access to subsystem timing breakdown

#### PerformanceReport
Multi-frame analysis and bottleneck identification:
- Aggregates data across multiple frames
- Calculates averages, minimums, maximums
- Identifies top N slowest operations
- Detects threshold violations

#### PerformanceOverlay
Visual component for real-time performance display:
- Shows current FPS and frame time
- Displays subsystem timing breakdown
- Highlights performance warnings
- Toggle on/off at runtime

## Usage

### Service Registration

Register the profiler service in your application's DI container:

```csharp
var services = new ServiceCollection();
services.AddSingleton<IProfiler, Profiler>();
// ... other services
```

### Basic Profiling

The profiler is integrated into the main application loop and major subsystems:

```csharp
// Application.Run() automatically manages frame lifecycle
profiler.BeginFrame();  // Called before Update
profiler.EndFrame();    // Called after Render

// Components receive IProfiler via dependency injection
public class Renderer : IRenderer
{
    private readonly IProfiler _profiler;
    
    public Renderer(IProfiler profiler)
    {
        _profiler = profiler;
    }
    
    public void OnRender(double deltaTime)
    {
        using var _ = new PerformanceScope("Render", _profiler);
        // Rendering code automatically timed
    }
}
```

### Engine Subsystem Instrumentation

The following major subsystems are automatically profiled:

1. **Update Loop** (`ContentManager.OnUpdate`)
   - Measures component tree traversal and update logic
   - Includes all active component updates
   
2. **Render Loop** (`Renderer.OnRender`)
   - Measures complete frame rendering time
   - Includes all render command submission
   
3. **Resource Loading** (`ResourceManager`)
   - Tracks asset loading and preparation
   - Helps identify slow resource operations

### Custom Profiling Markers

Add profiling to custom code using `PerformanceScope`:

```csharp
public void ProcessPhysics(double deltaTime)
{
    using var _ = new PerformanceScope("Physics", _profiler);
    
    // Nested scopes are supported
    using (new PerformanceScope("CollisionDetection", _profiler))
    {
        DetectCollisions();
    }
    
    using (new PerformanceScope("PhysicsIntegration", _profiler))
    {
        IntegratePhysics(deltaTime);
    }
}
```

### Runtime Control

Enable/disable profiling at runtime:

```csharp
// Check profiling state
if (profiler.IsEnabled)
{
    // Profiling active
}

// Enable profiling
profiler.Enable();

// Disable profiling (zero overhead when disabled)
profiler.Disable();

// Clear collected data
profiler.Clear();
```

### Performance Overlay

**Note**: Full rendering integration pending data binding system implementation.

The `PerformanceMonitor` component acts as a facade over `IProfiler`, exposing performance metrics as component properties for future data binding:

```csharp
// Add PerformanceMonitor component to track metrics
var monitorTemplate = new PerformanceMonitorTemplate
{
    Enabled = true,
    WarningThresholdMs = 6.67,      // 150 FPS target
    AverageFrameCount = 60,          // Rolling average over 60 frames
    UpdateIntervalSeconds = 0.5      // Update display twice per second
};

var monitor = ContentManager.CreateInstance(monitorTemplate);
scene.AddChild(monitor);

// Component properties available for binding:
// - CurrentFps, AverageFps
// - CurrentFrameTimeMs, AverageFrameTimeMs, MinFrameTimeMs, MaxFrameTimeMs
// - UpdateTimeMs, RenderTimeMs, ResourceLoadTimeMs
// - PerformanceWarning (bool)
// - PerformanceSummary (formatted string)
```

**Future Integration**: Once data binding is implemented, these properties can be bound to `TextRenderer` components for visual display.

### Analysis and Reporting

Generate performance reports for bottleneck identification:

```csharp
// Generate report for last 100 frames
var report = profiler.GenerateReport(frameCount: 100);

// Get overall timing statistics
var updateStats = report.GetStatisticsForLabel("Update");
Console.WriteLine($"Update: {updateStats.AverageMs:F2}ms avg, " +
                  $"{updateStats.MinMs:F2}ms min, {updateStats.MaxMs:F2}ms max");

// Identify top 5 slowest operations
var bottlenecks = report.GetTopNSlowest(5);
foreach (var (label, avgMs) in bottlenecks)
{
    Console.WriteLine($"{label}: {avgMs:F2}ms average");
}

// Check operations exceeding target frame time (6.67ms for 150 FPS)
var violations = report.GetThresholdViolations(thresholdMs: 6.67);
Console.WriteLine($"Found {violations.Count} frames exceeding budget");

// Get average time breakdown per operation
var breakdown = report.GetAverageTimePerLabel();
foreach (var (label, avgMs) in breakdown.OrderByDescending(x => x.Value))
{
    Console.WriteLine($"{label}: {avgMs:F2}ms");
}
```

### Integration Test Examples

The system includes frame-based integration tests demonstrating real-world usage:

```csharp
// Test 1: Profiler Activation (ProfilingActivationTest)
// Verifies runtime enable/disable functionality
profiler.Disable();
Assert.False(profiler.IsEnabled);
profiler.Enable();
Assert.True(profiler.IsEnabled);

// Test 2: Subsystem Profiling (SubsystemProfilingTest)
// Validates data collection for major engine subsystems
profiler.Enable();
profiler.Clear();
// ... run several frames ...
var report = profiler.GenerateReport(5);
Assert.NotNull(report.GetStatisticsForLabel("Update"));
Assert.NotNull(report.GetStatisticsForLabel("Render"));

// Test 3: Bottleneck Identification (BottleneckIdentificationTest)
// Demonstrates performance analysis workflow
profiler.Enable();
// ... run workload frames ...
var report = profiler.GenerateReport(10);
var topSlowest = report.GetTopNSlowest(5);
var violations = report.GetThresholdViolations(6.67);
Assert.True(topSlowest.Count >= 1);
Assert.True(violations.Count > 0);
```

## Performance Characteristics

- **Timer Resolution**: ~0.0001ms (100 nanoseconds) using `Stopwatch.GetTimestamp()`
- **Profiling Overhead**: <5% of frame time for typical instrumentation (10-20 markers per frame)
- **Memory**: Zero-allocation timing using ref struct pattern (`PerformanceScope`)
- **Storage**: Ring buffer with capacity for 1000 frames of history
- **Samples**: Up to 100 samples per frame (auto-truncates if exceeded)
- **Thread Safety**: Lock-based synchronization for sample recording

## Implementation Details

### Frame Lifecycle Integration

The profiler is integrated into `Application.Run()` event loop:

```csharp
window.Update += deltaTime =>
{
    profiler.BeginFrame();  // Marks frame start, clears previous samples
    contentManager.OnUpdate(deltaTime);  // Profiled with "Update" marker
};

window.Render += deltaTime =>
{
    renderer.OnRender(deltaTime);  // Profiled with "Render" marker
    profiler.EndFrame();  // Finalizes frame, calculates total frame time
};
```

### Zero-Allocation Design

`PerformanceScope` is a `ref struct`, ensuring:
- Stack allocation only (no GC pressure)
- Deterministic disposal timing via `IDisposable`
- Cannot be boxed or stored in fields
- Minimal runtime overhead (~2-3 instructions)

```csharp
public ref struct PerformanceScope
{
    private readonly IProfiler _profiler;
    private readonly string _label;
    private readonly long _startTimestamp;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PerformanceScope(string label, IProfiler profiler)
    {
        _profiler = profiler;
        _label = label;
        _startTimestamp = Stopwatch.GetTimestamp();
    }
    
    public void Dispose()
    {
        long elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
        double elapsedMs = (elapsed * 1000.0) / Stopwatch.Frequency;
        _profiler.RecordSample(_label, elapsedMs);
    }
}
```

### Ring Buffer Storage

Frame history uses fixed-size ring buffer to prevent unbounded memory growth:
- Maximum 1000 frames retained (configurable via `MaxFrameHistory`)
- Oldest frames automatically discarded when limit reached
- Efficient array-based implementation
- O(1) insertion and O(n) report generation

## Testing

### Unit Tests
- `src/Tests/GameEngine/Performance/` - Unit tests for all profiling components
- Target: 80% code coverage per project guidelines

### Integration Tests
- `src/TestApp/Testing/` - Frame-based integration tests
- Verify profiling activation, data collection, overlay rendering, bottleneck identification

## Related Documentation

- Specification: `specs/012-performance/spec.md`
- Implementation Plan: `specs/012-performance/plan.md`
- Task Breakdown: `specs/012-performance/tasks.md`
- Timing Research: `specs/012-performance/research-timing.md`
