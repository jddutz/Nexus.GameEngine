using Xunit;
using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Graphics;

namespace Tests.GameEngine.Components.Lookups;

public class ParentLookupTests
{
    [Fact]
    public void Resolve_ShouldReturnParent_WhenParentIsOfTypeT()
    {
        // Arrange
        var parentMock = new Mock<IComponent>();
        var targetMock = new Mock<IComponent>();
        targetMock.Setup(c => c.Parent).Returns(parentMock.Object);

        var strategy = new ParentLookup<IComponent>();

        // Act
        var result = strategy.Resolve(targetMock.Object);

        // Assert
        Assert.Same(parentMock.Object, result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenParentIsNotOfTypeT()
    {
        // Arrange
        // Parent implements IComponent but NOT IDrawable
        var parentMock = new Mock<IComponent>(); 
        
        var targetMock = new Mock<IComponent>();
        targetMock.Setup(c => c.Parent).Returns(parentMock.Object);

        var strategy = new ParentLookup<IDrawable>();

        // Act
        var result = strategy.Resolve(targetMock.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenParentIsNull()
    {
        // Arrange
        var targetMock = new Mock<IComponent>();
        targetMock.Setup(c => c.Parent).Returns((IComponent?)null);

        var strategy = new ParentLookup<IComponent>();

        // Act
        var result = strategy.Resolve(targetMock.Object);

        // Assert
        Assert.Null(result);
    }
}
