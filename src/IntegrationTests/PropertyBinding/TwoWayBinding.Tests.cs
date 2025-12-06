using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Nexus.GameEngine.Data;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class TwoWayBindingTests
{
    [Fact]
    public void ShouldSyncBothWays()
    {
        // Arrange
        var parent = new VolumeSource { Volume = 50 };
        var child = new SliderTarget { Value = 0 };
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                Value = Nexus.GameEngine.Components.PropertyBinding.FromParent<VolumeSource>()
                               .GetPropertyValue("Volume")
                               .TwoWay()
            }
        };
        
        // Build hierarchy
        parent.AddChild(child);
        
        // Act - Load & Activate
        parent.Load(new Template());
        child.Load(template);
        
        parent.Activate();
        
        // Assert - Initial Sync (Source -> Target)
        Assert.Equal(50, child.Value);
        
        // Act - Update Source
        parent.Volume = 75;
        
        // Assert - Source -> Target Sync
        Assert.Equal(75, child.Value);
        
        // Act - Update Target
        child.Value = 25;
        
        // Assert - Target -> Source Sync
        Assert.Equal(25, parent.Volume);
    }

    [Fact]
    public void ShouldSyncBothWaysWithConverter()
    {
        // Arrange
        var parent = new VolumeSource { Volume = 50 }; // 0-100
        var child = new SliderTarget { Value = 0 }; // 0-100
        
        // Use MultiplyConverter to map 0-100 to 0-200 (factor 2)
        // Source=50 -> Target=100
        // Target=50 -> Source=25
        
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                Value = Nexus.GameEngine.Components.PropertyBinding.FromParent<VolumeSource>()
                               .GetPropertyValue("Volume")
                               .WithConverter(new MultiplyConverter(2.0f))
                               .TwoWay()
            }
        };
        
        parent.AddChild(child);
        
        parent.Load(new Template());
        child.Load(template);
        
        parent.Activate();
        
        // Assert - Initial Sync (50 * 2 = 100)
        Assert.Equal(100, child.Value);
        
        // Act - Update Target (150 / 2 = 75)
        child.Value = 150;
        
        // Assert - Target -> Source Sync
        Assert.Equal(75, parent.Volume);
        
        // Act - Update Source (25 * 2 = 50)
        parent.Volume = 25;
        
        // Assert - Source -> Target Sync
        Assert.Equal(50, child.Value);
    }

    public class VolumeSource : RuntimeComponent
    {
        private float _volume;
        public float Volume
        {
            get => _volume;
            set
            {
                float old = _volume;
                _volume = value;
                VolumeChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? VolumeChanged;
    }

    public class SliderTarget : RuntimeComponent
    {
        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                float old = _value;
                _value = value;
                ValueChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? ValueChanged;
    }

    public class ManualBindings : PropertyBindings
    {
        public Nexus.GameEngine.Components.PropertyBinding? Value { get; init; }
        
        public override IEnumerator<(string propertyName, Nexus.GameEngine.Components.PropertyBinding binding)> GetEnumerator()
        {
            if (Value != null) yield return ("Value", Value);
        }
    }
}
