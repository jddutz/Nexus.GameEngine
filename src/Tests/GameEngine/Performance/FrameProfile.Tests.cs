using System.Diagnostics;
using Xunit;
using Nexus.GameEngine.Performance;

namespace Tests.GameEngine.Performance;

public class FrameProfileTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        int frameNumber = 42;
        long startTime = Stopwatch.GetTimestamp();
        long endTime = startTime + (Stopwatch.Frequency / 100); // ~10ms
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, startTime),
            new("Update", 3.0, startTime)
        };

        // Act
        var profile = new FrameProfile(frameNumber, samples, startTime, endTime);

        // Assert
        Assert.Equal(frameNumber, profile.FrameNumber);
        Assert.Equal(2, profile.Samples.Count);
        Assert.Equal(startTime, profile.FrameStartTimestamp);
        Assert.Equal(endTime, profile.FrameEndTimestamp);
        Assert.True(profile.TotalFrameTimeMs > 0);
    }

    [Fact]
    public void TotalFrameTimeMs_CalculatesFromTimestamps()
    {
        // Arrange
        long startTime = Stopwatch.GetTimestamp();
        long endTime = startTime + (Stopwatch.Frequency / 100); // ~10ms
        var samples = new List<PerformanceSample>();

        // Act
        var profile = new FrameProfile(1, samples, startTime, endTime);

        // Assert
        Assert.True(profile.TotalFrameTimeMs >= 9.0 && profile.TotalFrameTimeMs <= 11.0,
            $"Expected ~10ms, got {profile.TotalFrameTimeMs}ms");
    }

    [Fact]
    public void FPS_CalculatesFromTotalFrameTime()
    {
        // Arrange - simulate 16.67ms frame time (60 FPS)
        long startTime = Stopwatch.GetTimestamp();
        long endTime = startTime + (Stopwatch.Frequency * 16670 / 1000000); // 16.67ms
        var samples = new List<PerformanceSample>();

        // Act
        var profile = new FrameProfile(1, samples, startTime, endTime);

        // Assert
        Assert.True(profile.FPS >= 58 && profile.FPS <= 62,
            $"Expected ~60 FPS, got {profile.FPS:F2} FPS");
    }

    [Fact]
    public void FPS_ReturnsZeroForZeroFrameTime()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>();

        // Act
        var profile = new FrameProfile(1, samples, timestamp, timestamp);

        // Assert
        Assert.Equal(0, profile.FPS);
    }

    [Fact]
    public void GetSamplesByLabel_FiltersSamples()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, timestamp),
            new("Update", 3.0, timestamp),
            new("Render", 4.0, timestamp)
        };
        var profile = new FrameProfile(1, samples, timestamp, timestamp);

        // Act
        var renderSamples = profile.GetSamplesByLabel("Render").ToList();

        // Assert
        Assert.Equal(2, renderSamples.Count);
        Assert.All(renderSamples, s => Assert.Equal("Render", s.Label));
    }

    [Fact]
    public void GetTotalTimeForLabel_SumsElapsedTime()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, timestamp),
            new("Update", 3.0, timestamp),
            new("Render", 4.0, timestamp)
        };
        var profile = new FrameProfile(1, samples, timestamp, timestamp);

        // Act
        double totalRenderTime = profile.GetTotalTimeForLabel("Render");

        // Assert
        Assert.Equal(9.0, totalRenderTime);
    }

    [Fact]
    public void GetUniqueLabels_ReturnsDistinctLabels()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, timestamp),
            new("Update", 3.0, timestamp),
            new("Render", 4.0, timestamp),
            new("Input", 1.0, timestamp)
        };
        var profile = new FrameProfile(1, samples, timestamp, timestamp);

        // Act
        var labels = profile.GetUniqueLabels().ToList();

        // Assert
        Assert.Equal(3, labels.Count);
        Assert.Contains("Render", labels);
        Assert.Contains("Update", labels);
        Assert.Contains("Input", labels);
    }

    [Fact]
    public void GetTimeBreakdown_ReturnsLabelToTimeMapping()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, timestamp),
            new("Update", 3.0, timestamp),
            new("Render", 4.0, timestamp),
            new("Input", 1.0, timestamp)
        };
        var profile = new FrameProfile(1, samples, timestamp, timestamp);

        // Act
        var breakdown = profile.GetTimeBreakdown();

        // Assert
        Assert.Equal(3, breakdown.Count);
        Assert.Equal(9.0, breakdown["Render"]);
        Assert.Equal(3.0, breakdown["Update"]);
        Assert.Equal(1.0, breakdown["Input"]);
    }

    [Fact]
    public void ToString_FormatsProfileInfo()
    {
        // Arrange
        long timestamp = Stopwatch.GetTimestamp();
        var samples = new List<PerformanceSample>
        {
            new("Render", 5.0, timestamp),
            new("Update", 3.0, timestamp)
        };
        var profile = new FrameProfile(42, samples, timestamp, timestamp);

        // Act
        string result = profile.ToString();

        // Assert
        Assert.Contains("Frame 42", result);
        Assert.Contains("2 samples", result);
    }
}
