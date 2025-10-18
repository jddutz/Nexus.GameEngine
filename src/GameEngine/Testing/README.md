# Testing Infrastructure

This folder contains testing-specific services and utilities for the Nexus Game Engine.

⚠️ **WARNING**: These services impact performance and should only be used in test/debug builds, never in production.

## Pixel Sampling

The pixel sampling service allows test components to validate rendered output by reading pixel colors from the screen.

### Services

#### `IPixelSampler`
Interface for sampling pixel colors from rendered output.

**Key Methods:**
- `SamplePixel(int x, int y)` - Sample a single pixel at screen coordinates
- `SamplePixels(Vector2D<int>[] coordinates)` - Batch sample multiple pixels efficiently
- `IsAvailable` - Check if sampling is currently available
- `Enabled { get; set; }` - Enable/disable sampling to control performance impact

#### `VulkanPixelSampler`
Vulkan implementation of pixel sampling (currently a stub - TODO: implement actual Vulkan image readback).

### Usage in Tests

1. **Register the service** in your test application startup:

```csharp
services.AddPixelSampling();
```

2. **Inject into test components**:

```csharp
public class MyTestComponent(IPixelSampler pixelSampler) : RuntimeComponent(), ITestComponent
{
    protected override void OnActivate()
    {
        base.OnActivate();
        pixelSampler.Enabled = true; // Enable for this test
    }
    
    protected override void OnUpdate(double deltaTime)
    {
        if (readyToSample)
        {
            var centerColor = pixelSampler.SamplePixel(screenWidth / 2, screenHeight / 2);
            
            if (PixelAssertions.ColorsMatch(centerColor, PixelAssertions.TestColors.Red))
            {
                // Test passed!
            }
        }
    }
    
    protected override void OnDeactivate()
    {
        pixelSampler.Enabled = false; // Disable when done
        base.OnDeactivate();
    }
}
```

3. **Use helper assertions**:

```csharp
using static Nexus.GameEngine.Testing.PixelAssertions;

// Check if pixel matches expected color
bool matches = ColorsMatch(actualColor, TestColors.Red, tolerance: 0.01f);

// Get human-readable description for test reports
string description = DescribeColor(actualColor);
// Output: "RGBA(1.000, 0.000, 0.000, 1.000)"
```

### Performance Considerations

Pixel sampling requires copying the GPU framebuffer to CPU-readable memory, which:
- Adds GPU-to-CPU synchronization overhead
- Requires additional memory allocation
- May cause pipeline stalls

**Best Practices:**
- Only enable when actively sampling
- Batch multiple samples with `SamplePixels()` instead of multiple `SamplePixel()` calls
- Disable immediately after test completes
- Never leave enabled in production builds

### TODO: Implementation

The current `VulkanPixelSampler` is a stub. Full implementation requires:

1. **Staging Buffer Creation**: Allocate host-visible buffer for image transfer
2. **Image Copy**: After frame rendering, copy swap chain image to staging buffer
3. **Memory Mapping**: Map staging buffer memory to CPU-accessible pointer
4. **Pixel Readback**: Read RGBA values at specified coordinates
5. **Synchronization**: Ensure frame is complete before reading (fence/semaphore)
6. **Format Conversion**: Handle different swap chain formats (BGRA vs RGBA, etc.)

This will require access to:
- `IGraphicsContext` for device and command buffers
- `ISwapChain` for image format and dimensions
- Vulkan command buffer for image copy operations

### Test Color Constants

Common test colors are provided in `PixelAssertions.TestColors`:
- Red, Green, Blue
- White, Black
- Transparent
- Magenta (useful as "error" color)
- Yellow, Cyan
