using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class SiblingBindingTests
{
    [Fact]
    public void ShouldBindToSiblingByType()
    {
        // Arrange
        var root = new ContainerComponent();
        var source = new SourceComponent { Value = 100 };
        var target = new TargetComponent();
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                CurrentValue = Binding.FromSibling<SourceComponent>()
                                      .GetPropertyValue(s => s.Value)
            }
        };
        
        // Build hierarchy
        root.AddChild(source);
        root.AddChild(target);
        
        // Act - Load & Activate
        root.Load(new Template());
        source.Load(new Template());
        target.Load(template);
        
        root.Activate();
        
        // Assert - Initial Sync
        Assert.Equal(100, target.CurrentValue);
        
        // Act - Update
        source.Value = 200;
        
        // Assert - Update Sync
        Assert.Equal(200, target.CurrentValue);
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
