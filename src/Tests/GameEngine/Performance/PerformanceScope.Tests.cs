using System.Diagnostics;
using Moq;
using Xunit;
using Nexus.GameEngine.Performance;

namespace Tests.GameEngine.Performance;

public class PerformanceScopeTests
{
    [Fact]
    public void Dispose_RecordsSampleWhenProfilerEnabled()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: true);
        const string label = "TestOperation";

        // Act
        using (var scope = new PerformanceScope(label, mockProfiler.Object))
        {
            // Simulate some work
            Thread.Sleep(1);
        }

        // Assert
        mockProfiler.Verify(p => p.RecordSample(label, It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotRecordSampleWhenProfilerDisabled()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: false);
        const string label = "TestOperation";

        // Act
        using (var scope = new PerformanceScope(label, mockProfiler.Object))
        {
            Thread.Sleep(1);
        }

        // Assert
        mockProfiler.Verify(p => p.RecordSample(It.IsAny<string>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public void Dispose_DoesNotRecordSampleWhenProfilerIsNull()
    {
        // Act & Assert (should not throw)
        using (var scope = new PerformanceScope("TestOperation", profiler: null))
        {
            Thread.Sleep(1);
        }
    }

    [Fact]
    public void Dispose_MeasuresElapsedTime()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: true);
        double recordedTime = 0;
        mockProfiler.Setup(p => p.RecordSample(It.IsAny<string>(), It.IsAny<double>()))
            .Callback<string, double>((_, elapsed) => recordedTime = elapsed);

        // Act
        using (var scope = new PerformanceScope("TestOperation", mockProfiler.Object))
        {
            Thread.Sleep(10); // Sleep for at least 10ms
        }

        // Assert - Sleep guarantees at least 10ms, but system load may cause longer duration
        Assert.True(recordedTime >= 10.0,
            $"Expected at least 10ms elapsed time, got {recordedTime}ms");
    }

    [Fact]
    public void Dispose_UsesCorrectLabel()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: true);
        const string expectedLabel = "MyCustomLabel";
        string? recordedLabel = null;

        mockProfiler.Setup(p => p.RecordSample(It.IsAny<string>(), It.IsAny<double>()))
            .Callback<string, double>((label, _) => recordedLabel = label);

        // Act
        using (var scope = new PerformanceScope(expectedLabel, mockProfiler.Object))
        {
            Thread.Sleep(1);
        }

        // Assert
        Assert.Equal(expectedLabel, recordedLabel);
    }

    [Fact]
    public void Dispose_HandlesVeryShortOperations()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: true);
        double recordedTime = 0;
        mockProfiler.Setup(p => p.RecordSample(It.IsAny<string>(), It.IsAny<double>()))
            .Callback<string, double>((_, elapsed) => recordedTime = elapsed);

        // Act - no work, should measure near-zero time
        using (var scope = new PerformanceScope("FastOperation", mockProfiler.Object))
        {
            // Intentionally empty - measure overhead only
        }

        // Assert - should be less than 1ms (likely microseconds)
        Assert.True(recordedTime >= 0 && recordedTime < 1.0,
            $"Expected <1ms for empty operation, got {recordedTime}ms");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var mockProfiler = CreateMockProfiler(isEnabled: true);
        var scope = new PerformanceScope("TestOperation", mockProfiler.Object);

        // Act
        scope.Dispose();
        scope.Dispose(); // Second dispose should be safe

        // Assert - Both calls will record, which is acceptable for ref struct
        // The important thing is that it doesn't throw
        mockProfiler.Verify(p => p.RecordSample(It.IsAny<string>(), It.IsAny<double>()), Times.AtLeastOnce);
    }

    private Mock<IProfiler> CreateMockProfiler(bool isEnabled)
    {
        var mock = new Mock<IProfiler>();
        mock.Setup(p => p.IsEnabled).Returns(isEnabled);
        return mock;
    }
}
