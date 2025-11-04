# Batching Validation Example

This example demonstrates how to use the batching statistics system to validate rendering performance without pixel tests.

## Quick Start

```csharp
// Enable batching statistics
renderer.CollectBatchingStatistics = true;
renderer.BatchingStatisticsAvailable += (sender, e) => {
    Console.WriteLine($"{e.PassName}: {e.Statistics}");
};
```

## When to Use

Use batching statistics validation when:
- Optimizing render performance
- Validating batching effectiveness
- Debugging descriptor/buffer management
- Testing without pixel sampling

See `.docs/Batching Validation Guide.md` for complete documentation.
