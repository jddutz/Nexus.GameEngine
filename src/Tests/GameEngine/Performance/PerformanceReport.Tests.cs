using System.Diagnostics;
using Xunit;
using Nexus.GameEngine.Performance;

namespace Tests.GameEngine.Performance;

public class PerformanceReportTests
{
    [Fact]
    public void Constructor_InitializesWithEmptyFrames()
    {
        // Act
        var report = new PerformanceReport(new List<FrameProfile>());

        // Assert
        Assert.Equal(0, report.FrameCount);
        Assert.Equal(0, report.AverageFrameTimeMs);
        Assert.Equal(0, report.MinFrameTimeMs);
        Assert.Equal(0, report.MaxFrameTimeMs);
        Assert.Equal(0, report.AverageFPS);
    }

    [Fact]
    public void Constructor_CalculatesAverageFrameTime()
    {
        // Arrange
        var frames = CreateFrameProfiles([10.0, 20.0, 30.0]);

        // Act
        var report = new PerformanceReport(frames);

        // Assert
        Assert.Equal(20.0, report.AverageFrameTimeMs);
    }

    [Fact]
    public void Constructor_CalculatesMinMaxFrameTime()
    {
        // Arrange
        var frames = CreateFrameProfiles([15.0, 8.0, 23.0, 12.0]);

        // Act
        var report = new PerformanceReport(frames);

        // Assert
        Assert.Equal(8.0, report.MinFrameTimeMs);
        Assert.Equal(23.0, report.MaxFrameTimeMs);
    }

    [Fact]
    public void AverageFPS_CalculatesFromAverageFrameTime()
    {
        // Arrange - 16.67ms = ~60 FPS
        var frames = CreateFrameProfiles([16.67]);

        // Act
        var report = new PerformanceReport(frames);

        // Assert
        Assert.True(report.AverageFPS >= 59.0 && report.AverageFPS <= 61.0,
            $"Expected ~60 FPS, got {report.AverageFPS:F2}");
    }

    [Fact]
    public void FrameTimeVarianceMs_CalculatesCorrectly()
    {
        // Arrange
        var frames = CreateFrameProfiles([10.0, 25.0, 15.0]);

        // Act
        var report = new PerformanceReport(frames);

        // Assert
        Assert.Equal(15.0, report.FrameTimeVarianceMs); // 25.0 - 10.0
    }

    [Fact]
    public void GetTopNSlowest_ReturnsSlowesttOperations()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0), ("Update", 5.0), ("Input", 1.0) },
            [("Render", 12.0), ("Update", 4.0), ("Input", 0.5)],
            [("Render", 11.0), ("Update", 6.0), ("Input", 1.5)]
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var slowest = report.GetTopNSlowest(2);

        // Assert
        Assert.Equal(2, slowest.Count);
        Assert.Equal("Render", slowest[0].Label);
        Assert.Equal("Update", slowest[1].Label);
        Assert.True(slowest[0].AverageMs > slowest[1].AverageMs);
    }

    [Fact]
    public void GetTopNSlowest_HandlesRequestForMoreThanAvailable()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0), ("Update", 5.0) }
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var slowest = report.GetTopNSlowest(10);

        // Assert
        Assert.Equal(2, slowest.Count);
    }

    [Fact]
    public void GetThresholdViolations_ReturnsFramesExceedingThreshold()
    {
        // Arrange
        var frames = CreateFrameProfiles([5.0, 15.0, 8.0, 20.0, 12.0]);
        var report = new PerformanceReport(frames);

        // Act
        var violations = report.GetThresholdViolations(thresholdMs: 10.0);

        // Assert
        Assert.Equal(3, violations.Count); // 15.0, 20.0, 12.0
        Assert.All(violations, v => Assert.True(v.TotalFrameTimeMs > 10.0));
    }

    [Fact]
    public void GetAverageTimePerLabel_CalculatesCorrectly()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0), ("Update", 5.0) },
            [("Render", 12.0), ("Update", 3.0)],
            [("Render", 8.0), ("Update", 4.0)]
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var averages = report.GetAverageTimePerLabel();

        // Assert
        Assert.Equal(2, averages.Count);
        Assert.Equal(10.0, averages["Render"]); // (10 + 12 + 8) / 3
        Assert.Equal(4.0, averages["Update"]); // (5 + 3 + 4) / 3
    }

    [Fact]
    public void GetTotalTimePerLabel_CalculatesCorrectly()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0), ("Update", 5.0) },
            [("Render", 12.0), ("Update", 3.0)]
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var totals = report.GetTotalTimePerLabel();

        // Assert
        Assert.Equal(22.0, totals["Render"]); // 10 + 12
        Assert.Equal(8.0, totals["Update"]); // 5 + 3
    }

    [Fact]
    public void GetStatisticsForLabel_ReturnsCorrectStatistics()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0) },
            [("Render", 15.0)],
            [("Render", 12.0)]
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var stats = report.GetStatisticsForLabel("Render");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("Render", stats.Label);
        Assert.Equal(3, stats.SampleCount);
        Assert.Equal(12.333, stats.AverageMs, precision: 2);
        Assert.Equal(10.0, stats.MinMs);
        Assert.Equal(15.0, stats.MaxMs);
        Assert.Equal(37.0, stats.TotalMs);
    }

    [Fact]
    public void GetStatisticsForLabel_ReturnsNullForNonExistentLabel()
    {
        // Arrange
        var frames = CreateFramesWithSamples(
        [
            new[] { ("Render", 10.0) }
        ]);

        var report = new PerformanceReport(frames);

        // Act
        var stats = report.GetStatisticsForLabel("NonExistent");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public void ToString_FormatsReportInfo()
    {
        // Arrange
        var frames = CreateFrameProfiles([10.0, 20.0, 30.0]);
        var report = new PerformanceReport(frames);

        // Act
        string result = report.ToString();

        // Assert
        Assert.Contains("3 frames", result);
        Assert.Contains("Avg:", result);
        Assert.Contains("FPS", result);
    }

    private List<FrameProfile> CreateFrameProfiles(double[] frameTimes)
    {
        var profiles = new List<FrameProfile>();
        long baseTimestamp = Stopwatch.GetTimestamp();

        for (int i = 0; i < frameTimes.Length; i++)
        {
            long startTime = baseTimestamp + i * Stopwatch.Frequency;
            long endTime = startTime + (long)(frameTimes[i] * Stopwatch.Frequency / 1000.0);

            profiles.Add(new FrameProfile(
                i,
                new List<PerformanceSample>(),
                startTime,
                endTime
            ));
        }

        return profiles;
    }

    private List<FrameProfile> CreateFramesWithSamples((string Label, double ElapsedMs)[][] frameSamples)
    {
        var profiles = new List<FrameProfile>();
        long baseTimestamp = Stopwatch.GetTimestamp();

        for (int i = 0; i < frameSamples.Length; i++)
        {
            var samples = frameSamples[i]
                .Select(s => new PerformanceSample(s.Label, s.ElapsedMs, baseTimestamp))
                .ToList();

            long startTime = baseTimestamp + i * Stopwatch.Frequency;
            long endTime = startTime + Stopwatch.Frequency / 60; // ~16.67ms frames

            profiles.Add(new FrameProfile(i, samples, startTime, endTime));
        }

        return profiles;
    }
}
