using Xunit;
using Nexus.GameEngine.Components;
using System.Collections.Generic;

namespace Tests.GameEngine.Components;

public class ComponentConfigurationTests
{
    public partial record TestConfigurableTemplate : Template
    {
        public int TestProperty { get; set; }
    }

    public partial class TestConfigurable : Component
    {
        public List<string> LifecycleLog { get; } = new();
        public int TestProperty { get; set; }

        protected override void Configure(Template template)
        {
            LifecycleLog.Add("Configure");
            base.Configure(template);
            if (template is TestConfigurableTemplate t)
            {
                TestProperty = t.TestProperty;
            }
        }

        protected override void OnLoad(Template? template)
        {
            LifecycleLog.Add("OnLoad");
            base.OnLoad(template);
        }
    }

    [Fact]
    public void Load_OrchestratesLifecycleCorrectly()
    {
        // Arrange
        var component = new TestConfigurable();
        var template = new TestConfigurableTemplate { TestProperty = 42 };
        var events = new List<string>();

        component.Loading += (s, e) => events.Add("Loading");
        component.Loaded += (s, e) => events.Add("Loaded");

        // Act
        component.Load(template);

        // Assert
        Assert.True(component.IsLoaded);
        Assert.Equal(42, component.TestProperty);
        
        // Verify event order
        Assert.Equal("Loading", events[0]);
        Assert.Equal("Loaded", events[1]);

        // Verify method call order
        Assert.Equal("Configure", component.LifecycleLog[0]);
        Assert.Equal("OnLoad", component.LifecycleLog[1]);
    }

    [Fact]
    public void IsValid_ReturnsTrue_AfterValidation()
    {
        // Arrange
        var component = new TestConfigurable();
        
        // Act
        var isValid = component.IsValid();

        // Assert
        Assert.True(isValid);
    }
}
