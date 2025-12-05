using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Performance;

/// <summary>
/// RAII-style performance measurement scope for automatic timing of code blocks.
/// Implemented as a ref struct to force stack allocation and eliminate GC pressure.
/// 
/// Usage:
/// <code>
/// public void Render()
/// {
///     using var scope = new PerformanceScope("Render");
///     // Rendering code is automatically timed
/// }
/// </code>
/// </summary>
public readonly ref struct PerformanceScope
{
    private readonly long _startTimestamp;
    private readonly string _label;
    private readonly IProfiler? _profiler;

    /// <summary>
    /// Creates a new performance scope and starts timing.
    /// </summary>
    /// <param name="label">Identifier for the operation being measured.</param>
    /// <param name="profiler">Profiler service to record the sample (optional for testing).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PerformanceScope(string label, IProfiler? profiler = null)
    {
        _label = label;
        _profiler = profiler;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Disposes the scope, calculates elapsed time, and records the sample.
    /// Automatically called when the using statement block exits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_profiler?.IsEnabled != true)
            return;

        long endTimestamp = Stopwatch.GetTimestamp();
        long elapsedTicks = endTimestamp - _startTimestamp;

        // Convert ticks to milliseconds: (ticks * 1000.0) / frequency
        // Multiply before division to maintain precision
        double elapsedMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;

        _profiler.RecordSample(_label, elapsedMs);
    }
}
