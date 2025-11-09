using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Vulkan;

namespace Tests;

/// <summary>
/// Tests for BatchingStatistics functionality.
/// Validates that batching analysis correctly detects state changes.
/// </summary>
public class BatchingStatisticsTests
{
    /// <summary>
    /// Test: AnalyzeBatching correctly counts state changes between draw commands.
    /// </summary>
    [Fact]
    public void AnalyzeBatching_CountsStateChanges_Correctly()
    {
        // Arrange
        var strategy = new DefaultBatchStrategy();
        
        // Create mock draw commands with varying state
        var commands = new List<DrawCommand>
        {
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1), // No changes
            CreateDrawCommand(pipeline: 1, descriptor: 2, vertex: 1, index: 1), // Descriptor change
            CreateDrawCommand(pipeline: 2, descriptor: 2, vertex: 1, index: 1), // Pipeline change
        };
        
        // Act
        var stats = strategy.AnalyzeBatching(commands);
        
        // Assert
        Assert.Equal(4, stats.TotalDrawCommands);
        Assert.Equal(2, stats.PipelineChanges);  // Initial + 1 change
        Assert.Equal(2, stats.DescriptorSetChanges);  // Initial + 1 change
        Assert.Equal(1, stats.VertexBufferChanges);  // Initial only
        Assert.Equal(1, stats.IndexBufferChanges);  // Initial only
    }
    
    /// <summary>
    /// Test: Batching ratio calculation is correct.
    /// </summary>
    [Fact]
    public void BatchingRatio_CalculatesCorrectly()
    {
        // Arrange - perfect batching (all same state)
        var strategy = new DefaultBatchStrategy();
        var commands = new List<DrawCommand>
        {
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
        };
        
        // Act
        var stats = strategy.AnalyzeBatching(commands);
        
        // Assert - with perfect batching, only 1 pipeline change (initial bind)
        var ratio = stats.GetBatchingRatio();
        Assert.True(ratio > 0f && ratio < 1f, $"Expected ratio between 0 and 1, got {ratio}");
        Assert.Equal(1, stats.PipelineChanges);  // Only initial bind
        Assert.Equal(3, stats.TotalDrawCommands);
    }
    
    /// <summary>
    /// Test: Statistics ToString() provides readable summary.
    /// </summary>
    [Fact]
    public void Statistics_ToString_ProvidesReadableSummary()
    {
        // Arrange
        var strategy = new DefaultBatchStrategy();
        var commands = new List<DrawCommand>
        {
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
            CreateDrawCommand(pipeline: 1, descriptor: 1, vertex: 1, index: 1),
        };
        
        // Act
        var stats = strategy.AnalyzeBatching(commands);
        var summary = stats.ToString();
        
        // Assert
        Assert.Contains("Draw Commands: 2", summary);
        Assert.Contains("Pipeline Changes: 1", summary);
        Assert.Contains("Batching Efficiency:", summary);
    }
    
    /// <summary>
    /// Test: Empty command list returns zero statistics.
    /// </summary>
    [Fact]
    public void AnalyzeBatching_EmptyList_ReturnsZeroStats()
    {
        // Arrange
        var strategy = new DefaultBatchStrategy();
        var commands = new List<DrawCommand>();
        
        // Act
        var stats = strategy.AnalyzeBatching(commands);
        
        // Assert
        Assert.Equal(0, stats.TotalDrawCommands);
        Assert.Equal(0, stats.PipelineChanges);
        Assert.Equal(0, stats.DescriptorSetChanges);
        Assert.Equal(0, stats.VertexBufferChanges);
        Assert.Equal(0, stats.IndexBufferChanges);
        Assert.Equal(0f, stats.GetBatchingRatio());
    }
    
    /// <summary>
    /// Helper method to create a draw command with specified state handles.
    /// </summary>
    private static DrawCommand CreateDrawCommand(ulong pipeline, ulong descriptor, ulong vertex, ulong index)
    {
        return new DrawCommand
        {
            Pipeline = new PipelineHandle(
                new Pipeline(pipeline),
                new PipelineLayout(0),
                $"TestPipeline_{pipeline}"
            ),
            DescriptorSet = new DescriptorSet(descriptor),
            VertexBuffer = new Silk.NET.Vulkan.Buffer(vertex),
            IndexBuffer = new Silk.NET.Vulkan.Buffer(index),
            VertexCount = 6,
            FirstVertex = 0,
            RenderPriority = 100,
            RenderMask = 1,
            PushConstants = null
        };
    }
}
