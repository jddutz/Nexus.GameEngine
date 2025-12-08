using System;
using System.Reflection;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Events;
using Xunit;

namespace Tests.GameEngine.Components;

public class PropertyBindingTests
{
    private class TestSourceComponent : Component
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
    
    private class TestTargetComponent : Component
    {
        public float CurrentHealth { get; set; }
        public string? Status { get; set; }
        
        public void SetHealth(float value)
        {
            CurrentHealth = value;
        }
    }

    [Fact]
    public void Activate_ResolvesSource_AndSyncsValue()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 100f };
        var target = new TestTargetComponent();
        
        // Setup hierarchy
        source.AddChild(target);
        
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.CurrentHealth = src.Health; // Initial sync
                src.HealthChanged += (s, e) => tgt.CurrentHealth = e.NewValue;
            },
            unsubscribe: (src, tgt) => src.HealthChanged -= (s, e) => tgt.CurrentHealth = e.NewValue,
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        // Act
        binding.Activate(target);
        
        // Assert
        Assert.Equal(100f, target.CurrentHealth);
    }

    [Fact]
    public void Activate_SubscribesToEvent_AndUpdatesValue()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 100f };
        var target = new TestTargetComponent();
        source.AddChild(target);
        
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.CurrentHealth = src.Health;
                src.HealthChanged += (s, e) => tgt.CurrentHealth = e.NewValue;
            },
            unsubscribe: (src, tgt) => src.HealthChanged -= (s, e) => tgt.CurrentHealth = e.NewValue,
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        binding.Activate(target);
        
        // Act
        source.Health = 50f;
        
        // Assert
        Assert.Equal(50f, target.CurrentHealth);
    }

    [Fact]
    public void Deactivate_Unsubscribes()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 100f };
        var target = new TestTargetComponent();
        source.AddChild(target);
        
        EventHandler<PropertyChangedEventArgs<float>>? handler = null;
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.CurrentHealth = src.Health;
                handler = (s, e) => tgt.CurrentHealth = e.NewValue;
                src.HealthChanged += handler;
            },
            unsubscribe: (src, tgt) => 
            {
                if (handler != null) src.HealthChanged -= handler;
            },
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        binding.Activate(target);
        binding.Deactivate();
        
        // Act
        source.Health = 50f;
        
        // Assert
        Assert.Equal(100f, target.CurrentHealth); // Should remain at old value
    }

    [Fact]
    public void Set_WithTargetDelegate_Works()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 75f };
        var target = new TestTargetComponent();
        source.AddChild(target);
        
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.SetHealth(src.Health);
                src.HealthChanged += (s, e) => tgt.SetHealth(e.NewValue);
            },
            unsubscribe: (src, tgt) => src.HealthChanged -= (s, e) => tgt.SetHealth(e.NewValue),
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        // Act
        binding.Activate(target);
        
        // Assert
        Assert.Equal(75f, target.CurrentHealth);
        
        // Update
        source.Health = 25f;
        Assert.Equal(25f, target.CurrentHealth);
    }

    [Fact]
    public void AsFormattedString_FormatsValue()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 100f };
        var target = new TestTargetComponent();
        source.AddChild(target);
        
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.Status = $"Health: {src.Health:F0}";
                src.HealthChanged += (s, e) => tgt.Status = $"Health: {e.NewValue:F0}";
            },
            unsubscribe: (src, tgt) => src.HealthChanged -= (s, e) => tgt.Status = $"Health: {e.NewValue:F0}",
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        // Act
        binding.Activate(target);
        
        // Assert
        Assert.Equal("Health: 100", target.Status);
        
        // Update
        source.Health = 50.5f;
        Assert.Equal("Health: 50", target.Status); // F0 rounds
    }

    private class TestConverter : IValueConverter
    {
        public object? Convert(object? value)
        {
            if (value is float f) return f * 2f;
            return null;
        }
    }

    [Fact]
    public void WithConverter_TransformsValue()
    {
        // Arrange
        var source = new TestSourceComponent { Health = 50f };
        var target = new TestTargetComponent();
        source.AddChild(target);
        
        var converter = new TestConverter();
        var binding = new PropertyBinding<TestSourceComponent, TestTargetComponent>(
            subscribe: (src, tgt) => 
            {
                tgt.CurrentHealth = (float)(converter.Convert(src.Health) ?? 0f);
                src.HealthChanged += (s, e) => tgt.CurrentHealth = (float)(converter.Convert(e.NewValue) ?? 0f);
            },
            unsubscribe: (src, tgt) => src.HealthChanged -= (s, e) => tgt.CurrentHealth = (float)(converter.Convert(e.NewValue) ?? 0f),
            lookup: Binding.ParentLookup<TestSourceComponent>());
            
        // Act
        binding.Activate(target);
        
        // Assert
        Assert.Equal(100f, target.CurrentHealth); // 50 * 2
    }
}
