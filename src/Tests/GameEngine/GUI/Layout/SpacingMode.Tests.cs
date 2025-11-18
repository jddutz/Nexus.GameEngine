using Nexus.GameEngine.GUI.Layout;

namespace Tests.GameEngine.GUI.Layout;

public class SpacingModeTests
{
    [Fact]
    public void SpacingMode_Stacked_HasValueZero()
    {
        // Arrange & Act
        var mode = SpacingMode.Stacked;

        // Assert
        Assert.Equal(0, (int)mode);
    }

    [Fact]
    public void SpacingMode_Justified_HasValueOne()
    {
        // Arrange & Act
        var mode = SpacingMode.Justified;

        // Assert
        Assert.Equal(1, (int)mode);
    }

    [Fact]
    public void SpacingMode_Distributed_HasValueTwo()
    {
        // Arrange & Act
        var mode = SpacingMode.Distributed;

        // Assert
        Assert.Equal(2, (int)mode);
    }

    [Fact]
    public void SpacingMode_Stacked_HasCorrectName()
    {
        // Arrange & Act
        var mode = SpacingMode.Stacked;

        // Assert
        Assert.Equal("Stacked", mode.ToString());
    }

    [Fact]
    public void SpacingMode_Justified_HasCorrectName()
    {
        // Arrange & Act
        var mode = SpacingMode.Justified;

        // Assert
        Assert.Equal("Justified", mode.ToString());
    }

    [Fact]
    public void SpacingMode_Distributed_HasCorrectName()
    {
        // Arrange & Act
        var mode = SpacingMode.Distributed;

        // Assert
        Assert.Equal("Distributed", mode.ToString());
    }

    [Fact]
    public void SpacingMode_Values_AreUnique()
    {
        // Arrange
        var values = Enum.GetValues<SpacingMode>();

        // Act & Assert
        Assert.Equal(3, values.Length);
        Assert.Contains(SpacingMode.Stacked, values);
        Assert.Contains(SpacingMode.Justified, values);
        Assert.Contains(SpacingMode.Distributed, values);
    }
}