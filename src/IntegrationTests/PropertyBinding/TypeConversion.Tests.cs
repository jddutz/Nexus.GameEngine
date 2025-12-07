using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Nexus.GameEngine.Data;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class TypeConversionTests
{
    [Fact]
    public void FloatToString_WithFormat_ShouldUpdateText()
    {
        // Arrange
        var parent = new HealthSource { Health = 75.5f };
        var child = new TextTarget();
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                Text = Binding.FromParent<HealthSource>()
                              .GetPropertyValue(h => h.Health)
                              .AsFormattedString("HP: {0:F1}")
            }
        };
        
        // Build hierarchy
        parent.AddChild(child);
        
        // Act - Load & Activate
        parent.Load(new Template()); 
        child.Load(template);
        
        parent.Activate(); // Cascades to child
        
        // Assert - Initial Sync
        Assert.Equal("HP: 75.5", child.Text);
        
        // Act - Update
        parent.Health = 20.0f;
        
        // Assert - Update Sync
        Assert.Equal("HP: 20.0", child.Text);
    }

    [Fact]
    public void FloatToFloat_WithPercentageConverter_ShouldScaleValue()
    {
        // Arrange
        var parent = new ProgressSource { Progress = 0.5f }; // 0.0 to 1.0
        var child = new ProgressBarTarget(); // 0 to 100
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                Value = Binding.FromParent<ProgressSource>()
                               .GetPropertyValue(p => p.Progress)
                               .WithConverter(new PercentageConverter())
            }
        };
        
        // Build hierarchy
        parent.AddChild(child);
        
        // Act - Load & Activate
        parent.Load(new Template());
        child.Load(template);
        
        parent.Activate();
        
        // Assert - Initial Sync
        Assert.Equal(50.0f, child.Value);
        
        // Act - Update
        parent.Progress = 0.75f;
        
        // Assert - Update Sync
        Assert.Equal(75.0f, child.Value);
    }

    public class HealthSource : Component
    {
        private float _health;
        public float Health
        {
            get => _health;
            set
            {
                float old = _health;
                _health = value;
                HealthChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? HealthChanged;
    }

    public class TextTarget : Component
    {
        public string? Text { get; set; }
    }

    public class ProgressSource : Component
    {
        private float _progress;
        public float Progress
        {
            get => _progress;
            set
            {
                float old = _progress;
                _progress = value;
                ProgressChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? ProgressChanged;
    }

    public class ProgressBarTarget : Component
    {
        public float Value { get; set; }
    }

    public class ManualBindings : PropertyBindings
    {
        public IPropertyBinding? Text { get; init; }
        public IPropertyBinding? Value { get; init; }
        
        public override IEnumerator<(string propertyName, IPropertyBinding binding)> GetEnumerator()
        {
            if (Text != null) yield return ("Text", Text);
            if (Value != null) yield return ("Value", Value);
        }
    }
}
