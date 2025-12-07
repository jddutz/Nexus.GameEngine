using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class NamedBindingTests
{
    [Fact]
    public void ShouldBindToSiblingByName()
    {
        // Arrange
        var root = new ContainerComponent();
        var source = new SourceComponent { Value = 10 };
        var target = new TargetComponent();
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                CurrentValue = Binding.FromNamedObject<SourceComponent>("Source")
                                      .GetPropertyValue(s => s.Value)
            }
        };
        
        // Build hierarchy
        root.AddChild(source);
        root.AddChild(target);
        
        // Act - Load & Activate
        root.Load(new ComponentTemplate { Name = "Root" });
        source.Load(new ComponentTemplate { Name = "Source" });
        target.Load(template);
        
        root.Activate();
        
        // Assert - Initial Sync
        Assert.Equal(10, target.CurrentValue);
        
        // Act - Update
        source.Value = 20;
        
        // Assert - Update Sync
        Assert.Equal(20, target.CurrentValue);
    }

    public class ContainerComponent : Component { }

    public class SourceComponent : Component
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

    public class TargetComponent : Component
    {
        public int CurrentValue { get; set; }
    }

    public class ManualBindings : PropertyBindings
    {
        public IPropertyBinding? CurrentValue { get; init; }
        
        public override IEnumerator<(string propertyName, IPropertyBinding binding)> GetEnumerator()
        {
            if (CurrentValue != null) yield return ("CurrentValue", CurrentValue);
        }
    }
}
