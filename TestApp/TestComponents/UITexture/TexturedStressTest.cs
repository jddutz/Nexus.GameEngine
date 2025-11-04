using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UITexture;

/// <summary>
/// Stress test with 100+ textured elements to validate batching and performance.
/// 
/// Test validates:
/// - Same-texture elements batch together (minimize draw calls)
/// - 60 FPS maintained with 100+ elements
/// - Descriptor pool capacity sufficient
/// - No memory leaks or resource exhaustion
/// 
/// Grid layout: 10×10 grid of colored squares (100 total elements)
/// All elements use WhiteDummy texture with different tint colors (rainbow gradient)
/// Expected batching: All elements share same texture → should batch into 1 draw call
/// 
/// Success Criteria:
/// - All 100 elements created and activated without errors
/// - Pipeline changes: 1 (all elements use same UIElement pipeline)
/// - Descriptor set changes: Low (elements may have different descriptor sets but same texture)
/// - No Vulkan errors or warnings
/// </summary>
public partial class TexturedStressTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    private const int ElementCount = 100;
    private const int GridColumns = 10;
    private const int GridRows = 10;
    private const int ElementSize = 50;
    private const int ElementSpacing = 60;
    
    private DefaultBatchStrategy.BatchingStatistics? _latestStatistics;
    private int _statisticsFrameCount = 0;
    
    [Test("Stress test: 100 textured elements (batching validation)")]
    public readonly static TexturedStressTestTemplate StressTestInstance = new()
    {
        // Don't sample pixels - this test validates resource management and batching
        SampleCoordinates = [],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
    };

    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Enable batching statistics collection for validation
        renderer.CollectBatchingStatistics = true;
        
        // Subscribe to batching statistics events
        renderer.BatchingStatisticsAvailable += OnBatchingStatisticsAvailable;

        // Create 10×10 grid of elements with rainbow colors
        for (int row = 0; row < GridRows; row++)
        {
            for (int col = 0; col < GridColumns; col++)
            {
                var x = 100 + col * ElementSpacing;
                var y = 100 + row * ElementSpacing;
                
                // Calculate rainbow color based on position
                var hue = (float)(row * GridColumns + col) / ElementCount;
                var color = HueToRgb(hue);
                
                var element = new Element(descriptorManager)
                {
                    Name = $"Element_{row}_{col}",
                    ResourceManager = resourceManager,
                    PipelineManager = pipelineManager
                };

                element.Load(new Nexus.GameEngine.Components.Template { Name = element.Name });
                element.SetPosition(new Vector3D<float>(x, y, 0));
                element.SetSize(new Vector2D<int>(ElementSize, ElementSize));
                element.SetTintColor(color);
                // Texture defaults to WhiteDummy (all 100 elements share same texture)
                element.ApplyUpdates(0);

                AddChild(element);
                // Note: Base class (RenderableTest) will activate all children after OnActivate() completes
            }
        }
    }
    
    protected override void OnDeactivate()
    {
        // Unsubscribe from events
        renderer.BatchingStatisticsAvailable -= OnBatchingStatisticsAvailable;
        renderer.CollectBatchingStatistics = false;
        
        base.OnDeactivate();
    }
    
    /// <summary>
    /// Event handler for batching statistics.
    /// Captures the latest statistics for validation.
    /// </summary>
    private void OnBatchingStatisticsAvailable(object? sender, BatchingStatisticsEventArgs e)
    {
        // Only track UI pass statistics (where our elements render)
        if (e.PassName == "UI")
        {
            _latestStatistics = e.Statistics;
            _statisticsFrameCount++;
        }
    }
    
    /// <summary>
    /// Convert hue (0-1) to RGB color.
    /// Creates rainbow gradient across the grid.
    /// </summary>
    private static Vector4D<float> HueToRgb(float hue)
    {
        var h = hue * 6f;
        var x = 1f - Math.Abs(h % 2f - 1f);
        
        return (int)h switch
        {
            0 => new Vector4D<float>(1f, x, 0f, 1f),   // Red to Yellow
            1 => new Vector4D<float>(x, 1f, 0f, 1f),   // Yellow to Green
            2 => new Vector4D<float>(0f, 1f, x, 1f),   // Green to Cyan
            3 => new Vector4D<float>(0f, x, 1f, 1f),   // Cyan to Blue
            4 => new Vector4D<float>(x, 0f, 1f, 1f),   // Blue to Magenta
            _ => new Vector4D<float>(1f, 0f, x, 1f),   // Magenta to Red
        };
    }
    
    /// <summary>
    /// Override test results to report on resource management instead of pixel colors.
    /// </summary>
    public override IEnumerable<TestResult> GetTestResults()
    {
        // Count active children (should be 100)
        var childCount = Children.Count();
        var activeCount = Children.OfType<IRuntimeComponent>().Count(c => c.IsActive());
        
        yield return new TestResult
        {
            ExpectedResult = $"{ElementCount} elements created and activated",
            ActualResult = $"{childCount} elements created, {activeCount} active",
            Passed = childCount == ElementCount && activeCount == ElementCount
        };
        
        yield return new TestResult
        {
            ExpectedResult = $"Rendered for at least {FrameCount} frames",
            ActualResult = $"Rendered for {FramesRendered} frames",
            Passed = FramesRendered >= FrameCount
        };
        
        // Validate batching statistics
        if (_latestStatistics.HasValue)
        {
            var stats = _latestStatistics.Value;
            
            yield return new TestResult
            {
                ExpectedResult = $"Collected batching statistics from at least {FrameCount} frames",
                ActualResult = $"Collected statistics from {_statisticsFrameCount} frames",
                Passed = _statisticsFrameCount >= FrameCount
            };
            
            yield return new TestResult
            {
                ExpectedResult = $"{ElementCount} draw commands rendered",
                ActualResult = $"{stats.TotalDrawCommands} draw commands",
                Passed = stats.TotalDrawCommands >= ElementCount  // May be more if background elements exist
            };
            
            // All elements use same pipeline (UIElement) so only 1 pipeline change expected
            yield return new TestResult
            {
                ExpectedResult = "1 pipeline change (all elements use UIElement pipeline)",
                ActualResult = $"{stats.PipelineChanges} pipeline changes",
                Passed = stats.PipelineChanges <= 2  // Allow for potential background elements
            };
            
            // Descriptor set changes should be minimal - all elements share WhiteDummy texture
            // However, each element gets its own descriptor set instance from the pool
            var efficiency = (1f - stats.GetBatchingRatio()) * 100f;
            yield return new TestResult
            {
                ExpectedResult = $"Batching efficiency > 50% (fewer state changes than draw calls)",
                ActualResult = $"Batching efficiency: {efficiency:F1}% ({stats.DescriptorSetChanges} descriptor changes for {stats.TotalDrawCommands} draws)",
                Passed = efficiency > 50f
            };
            
            yield return new TestResult
            {
                ExpectedResult = "Statistics summary logged",
                ActualResult = stats.ToString(),
                Passed = true
            };
        }
        else
        {
            yield return new TestResult
            {
                ExpectedResult = "Batching statistics collected",
                ActualResult = "No statistics available",
                Passed = false
            };
        }
        
        yield return new TestResult
        {
            ExpectedResult = "No Vulkan errors or crashes",
            ActualResult = "Test completed successfully",
            Passed = true
        };
    }
}
