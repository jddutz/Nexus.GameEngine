using System.Diagnostics;
using Xunit;
using Nexus.GameEngine.Performance;

namespace Tests.GameEngine.Performance;

public class ProfilerTests
{
    [Fact]
    public void Constructor_InitializesWithDisabledState()
    {
        // Act
        var profiler = new Profiler();

        // Assert
        Assert.False(profiler.IsEnabled);
    }

    [Fact]
    public void Enable_SetsIsEnabledToTrue()
    {
        // Arrange
        var profiler = new Profiler();

        // Act
        profiler.Enable();

        // Assert
        Assert.True(profiler.IsEnabled);
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();

        // Act
        profiler.Disable();

        // Assert
        Assert.False(profiler.IsEnabled);
    }

    [Fact]
    public void RecordSample_DoesNothingWhenDisabled()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.BeginFrame();

        // Act
        profiler.RecordSample("Test", 5.0);
        var profile = profiler.EndFrame();

        // Assert
        Assert.Empty(profile.Samples);
    }

    [Fact]
    public void RecordSample_AddsSampleWhenEnabled()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();

        // Act
        profiler.RecordSample("Test", 5.0);
        var profile = profiler.EndFrame();

        // Assert
        Assert.Single(profile.Samples);
        Assert.Equal("Test", profile.Samples[0].Label);
        Assert.Equal(5.0, profile.Samples[0].ElapsedMs);
    }

    [Fact]
    public void BeginFrame_ClearsPreviousSamples()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();
        profiler.RecordSample("Sample1", 5.0);
        profiler.EndFrame();

        // Act
        profiler.BeginFrame();
        profiler.RecordSample("Sample2", 3.0);
        var profile = profiler.EndFrame();

        // Assert
        Assert.Single(profile.Samples);
        Assert.Equal("Sample2", profile.Samples[0].Label);
    }

    [Fact]
    public void EndFrame_CreatesFrameProfileWithCorrectData()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();
        profiler.RecordSample("Render", 10.0);
        profiler.RecordSample("Update", 5.0);

        // Act
        var profile = profiler.EndFrame();

        // Assert
        Assert.Equal(2, profile.Samples.Count);
        Assert.Contains(profile.Samples, s => s.Label == "Render" && s.ElapsedMs == 10.0);
        Assert.Contains(profile.Samples, s => s.Label == "Update" && s.ElapsedMs == 5.0);
    }

    [Fact]
    public void EndFrame_IncrementsFrameNumber()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();

        // Act
        profiler.BeginFrame();
        var profile1 = profiler.EndFrame();

        profiler.BeginFrame();
        var profile2 = profiler.EndFrame();

        profiler.BeginFrame();
        var profile3 = profiler.EndFrame();

        // Assert
        Assert.Equal(0, profile1.FrameNumber);
        Assert.Equal(1, profile2.FrameNumber);
        Assert.Equal(2, profile3.FrameNumber);
    }

    [Fact]
    public void GenerateReport_IncludesRequestedFrameCount()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();

        for (int i = 0; i < 10; i++)
        {
            profiler.BeginFrame();
            profiler.RecordSample("Test", 5.0);
            profiler.EndFrame();
        }

        // Act
        var report = profiler.GenerateReport(frameCount: 5);

        // Assert
        Assert.Equal(5, report.FrameCount);
    }

    [Fact]
    public void GenerateReport_HandlesRequestForMoreFramesThanAvailable()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();

        for (int i = 0; i < 3; i++)
        {
            profiler.BeginFrame();
            profiler.RecordSample("Test", 5.0);
            profiler.EndFrame();
        }

        // Act
        var report = profiler.GenerateReport(frameCount: 10);

        // Assert
        Assert.Equal(3, report.FrameCount);
    }

    [Fact]
    public void Clear_RemovesAllFrameHistory()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();

        for (int i = 0; i < 5; i++)
        {
            profiler.BeginFrame();
            profiler.RecordSample("Test", 5.0);
            profiler.EndFrame();
        }

        // Act
        profiler.Clear();
        var report = profiler.GenerateReport(frameCount: 100);

        // Assert
        Assert.Equal(0, report.FrameCount);
    }

    [Fact]
    public void Clear_ResetsFrameNumber()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();
        profiler.EndFrame();
        profiler.BeginFrame();
        profiler.EndFrame();

        // Act
        profiler.Clear();
        profiler.BeginFrame();
        var profile = profiler.EndFrame();

        // Assert
        Assert.Equal(0, profile.FrameNumber);
    }

    [Fact]
    public void Profiler_LimitsFrameHistorySize()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        const int excessiveFrameCount = 1500; // More than MaxFrameHistory (1000)

        // Act
        for (int i = 0; i < excessiveFrameCount; i++)
        {
            profiler.BeginFrame();
            profiler.RecordSample("Test", 1.0);
            profiler.EndFrame();
        }

        var report = profiler.GenerateReport(frameCount: excessiveFrameCount);

        // Assert - should be capped at MaxFrameHistory
        Assert.True(report.FrameCount <= 1000,
            $"Expected frame count <= 1000, got {report.FrameCount}");
    }

    [Fact]
    public void RecordSample_HandlesMultipleSamplesInSameFrame()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();

        // Act
        for (int i = 0; i < 10; i++)
        {
            profiler.RecordSample($"Sample{i}", i * 1.0);
        }
        var profile = profiler.EndFrame();

        // Assert
        Assert.Equal(10, profile.Samples.Count);
    }

    [Fact]
    public void EndFrame_MeasuresActualFrameTime()
    {
        // Arrange
        var profiler = new Profiler();
        profiler.Enable();
        profiler.BeginFrame();

        // Act
        Thread.Sleep(10); // Simulate at least 10ms frame
        var profile = profiler.EndFrame();

        // Assert - Sleep guarantees at least 10ms, but system load may cause longer duration
        Assert.True(profile.TotalFrameTimeMs >= 10.0,
            $"Expected at least 10ms frame time, got {profile.TotalFrameTimeMs}ms");
    }
}
