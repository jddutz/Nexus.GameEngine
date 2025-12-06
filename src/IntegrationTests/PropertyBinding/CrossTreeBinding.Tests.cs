using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class CrossTreeBindingTests
{
    [Fact]
    public void ShouldBindToDistantComponentByName()
    {
        // Arrange
        // Root
        //   - Branch1
        //     - Source (Name="Source")
        //   - Branch2
        //     - Target (binds to "Source")
        
        var root = new ContainerComponent();
        var branch1 = new ContainerComponent();
        var branch2 = new ContainerComponent();
        var source = new SourceComponent { Value = 42 };
        var target = new TargetComponent();
        
        // Build hierarchy
        root.AddChild(branch1);
        root.AddChild(branch2);
        branch1.AddChild(source);
        branch2.AddChild(target);
        
        // Setup template with binding
        var template = new Template
        {
            Name = "Target",
            Bindings = new ManualBindings
            {
                CurrentValue = Nexus.GameEngine.Components.PropertyBinding.FromNamedObject("Source")
                                      .GetPropertyValue("Value")
            }
        };
        
        // Act - Load & Activate
        root.Load(new Template { Name = "Root" });
        branch1.Load(new Template { Name = "Branch1" });
        branch2.Load(new Template { Name = "Branch2" });
        source.Load(new Template { Name = "Source" });
        target.Load(template);
        
        root.Activate();
        
        // Assert - Initial Sync
        Assert.Equal(42, target.CurrentValue);
        
        // Act - Update
        source.Value = 99;
        
        // Assert - Update Sync
        Assert.Equal(99, target.CurrentValue);
    }

    public class ContainerComponent : RuntimeComponent { }

    public class SourceComponent : RuntimeComponent
    {
        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                int old = _value;
                _value = value;
                ValueChanged?.Invoke(this, new PropertyChangedEventArgs<int>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<int>>? ValueChanged;
    }

    public class TargetComponent : RuntimeComponent
    {
        public int CurrentValue { get; set; }
    }

    public class ManualBindings : PropertyBindings
    {
        public Nexus.GameEngine.Components.PropertyBinding? CurrentValue { get; init; }
        
        public override IEnumerator<(string propertyName, Nexus.GameEngine.Components.PropertyBinding binding)> GetEnumerator()
        {
            if (CurrentValue != null) yield return ("CurrentValue", CurrentValue);
        }
    }
}
