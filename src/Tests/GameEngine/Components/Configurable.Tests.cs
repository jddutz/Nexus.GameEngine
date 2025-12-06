using Xunit;
using Moq;
using Nexus.GameEngine.Components;
using System.Collections.Generic;
using System;

namespace Tests.GameEngine.Components;

public partial record TestConfigurableTemplate : Template
{
    public int TestProperty { get; set; }
}

public partial class TestConfigurable : Configurable
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

public class ConfigurableTests
{
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
        Assert.Equal(42, component.TestProperty);
        Assert.True(component.IsLoaded);
        
        // Verify sequence: Loading -> Configure -> OnLoad -> Loaded
        Assert.Equal("Loading", events[0]);
        Assert.Equal("Configure", component.LifecycleLog[0]);
        Assert.Equal("OnLoad", component.LifecycleLog[1]);
        Assert.Equal("Loaded", events[1]);
    }
}
