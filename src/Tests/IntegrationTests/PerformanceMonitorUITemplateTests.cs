using Xunit;
using Nexus.GameEngine.Templates;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Performance;
using System.Linq;
using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Tests.IntegrationTests;

public class PerformanceMonitorUITemplateTests
{
    [Fact]
    public void Template_HasCorrectStructure()
    {
        // Arrange
        var template = new PerformanceMonitorUITemplate();

        // Assert
        Assert.NotNull(template.Subcomponents);
        
        // Check for PerformanceMonitor component
        var perfMonitor = template.Subcomponents.FirstOrDefault(s => s.GetType().Name == "PerformanceMonitorTemplate");
        Assert.NotNull(perfMonitor);
        
        // Check for VerticalLayoutController
        var layout = template.Subcomponents.FirstOrDefault(s => s.GetType().Name == "VerticalLayoutControllerTemplate");
        Assert.NotNull(layout);
        
        // Check for TextRenderers
        var textRenderers = template.Subcomponents.Where(s => s.GetType().Name == "TextRendererTemplate").ToList();
        Assert.True(textRenderers.Count >= 5);
    }

    [Fact]
    public void Template_AllowsCustomization()
    {
        // Arrange
        var template = new PerformanceMonitorUITemplate
        {
            Position = new Vector2D<float>(100, 100),
            Alignment = new Vector2D<float>(1.0f, 1.0f) // BottomRight
        };

        // Assert
        Assert.Equal(100, template.Position!.Value.X);
        Assert.Equal(1.0f, template.Alignment!.Value.X);
    }
}
