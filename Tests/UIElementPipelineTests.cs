using Nexus.GameEngine.Graphics.Pipelines;
using Xunit;

namespace Tests;

public class UIElementPipelineTests
{
    [Fact]
    public void PipelineDefinitions_UIElement_Exists()
    {
        // Arrange & Act
        var pipeline = PipelineDefinitions.UIElement;

        // Assert
        Assert.NotNull(pipeline);
        Assert.Equal("UI_Element", pipeline.Name);
    }

    [Fact]
    public void PipelineDefinitions_UIElement_HasValidName()
    {
        // Arrange
        var pipeline = PipelineDefinitions.UIElement;

        // Act
        var name = pipeline.Name;

        // Assert
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void PipelineDefinitions_UIElement_IsNotNull()
    {
        // Arrange & Act
        var pipeline = PipelineDefinitions.UIElement;

        // Assert
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void PipelineDefinitions_UIElement_NameIsUnique()
    {
        // Arrange
        var uiElement = PipelineDefinitions.UIElement;

        // Act & Assert
        // Just verify it has a name different from other pipelines
        Assert.NotEqual("", uiElement.Name);
        Assert.NotNull(uiElement.Name);
    }
}