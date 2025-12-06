using Nexus.GameEngine.Data;
using Xunit;

namespace Tests.GameEngine.Data;

public class MultiplyConverterTests
{
    [Fact]
    public void Convert_MultipliesValue()
    {
        // Arrange
        var converter = new MultiplyConverter(2.0f);

        // Act
        var result = converter.Convert(10.0f);

        // Assert
        Assert.Equal(20.0f, result);
    }

    [Fact]
    public void Convert_HandlesIntegers()
    {
        // Arrange
        var converter = new MultiplyConverter(0.5f);

        // Act
        var result = converter.Convert(100);

        // Assert
        Assert.Equal(50.0f, result);
    }

    [Fact]
    public void Convert_HandlesNull()
    {
        // Arrange
        var converter = new MultiplyConverter(2.0f);

        // Act
        var result = converter.Convert(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_DividesValue()
    {
        // Arrange
        var converter = new MultiplyConverter(2.0f);

        // Act
        var result = converter.ConvertBack(20.0f);

        // Assert
        Assert.Equal(10.0f, result);
    }

    [Fact]
    public void ConvertBack_HandlesZeroFactor()
    {
        // Arrange
        var converter = new MultiplyConverter(0.0f);

        // Act
        var result = converter.ConvertBack(10.0f);

        // Assert
        Assert.Equal(0.0f, result); // Or handle division by zero gracefully? 
        // If factor is 0, Convert(x) = 0. ConvertBack(0) -> undefined/any. 
        // Let's assume it returns 0 or handles it safely.
    }
}
