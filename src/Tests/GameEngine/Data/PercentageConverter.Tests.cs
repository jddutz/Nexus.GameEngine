using Nexus.GameEngine.Data;
using Xunit;

namespace Tests.GameEngine.Data;

public class PercentageConverterTests
{
    [Fact]
    public void Convert_CalculatesPercentage()
    {
        // Arrange
        var converter = new PercentageConverter();

        // Act
        var result = converter.Convert(0.5f);

        // Assert
        // Assuming PercentageConverter converts 0-1 float to 0-100 float
        Assert.Equal(50.0f, result);
    }

    [Fact]
    public void ConvertBack_CalculatesFraction()
    {
        // Arrange
        var converter = new PercentageConverter();

        // Act
        var result = converter.ConvertBack(75.0f);

        // Assert
        Assert.Equal(0.75f, result);
    }

    [Fact]
    public void Convert_HandlesNull()
    {
        // Arrange
        var converter = new PercentageConverter();

        // Act
        var result = converter.Convert(null);

        // Assert
        Assert.Null(result);
    }
}
