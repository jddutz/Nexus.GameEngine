using Xunit;
using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Events;
using System;

namespace Tests.GameEngine.Components;

public class PropertyBindingTests
{
    [Fact]
    public void Activate_ShouldResolveSourceAndSubscribe()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(100, target.CurrentHealth); // Initial sync
        
        // Trigger change
        source.Health = 50;
        Assert.Equal(50, target.CurrentHealth); // Update
    }

    [Fact]
    public void Deactivate_ShouldUnsubscribe()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health);
            
        binding.Activate(target, "CurrentHealth");
        
        // Act
        binding.Deactivate();
        
        // Assert
        source.Health = 50;
        Assert.Equal(100, target.CurrentHealth); // Should NOT update
    }

    [Fact]
    public void WithConverter_ShouldApplyConverter()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var converterMock = new Mock<IValueConverter>();
        converterMock.Setup(c => c.Convert(100.0f)).Returns(50.0f);
        converterMock.Setup(c => c.Convert(200.0f)).Returns(100.0f);

        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health)
            .WithConverter(converterMock.Object);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(50, target.CurrentHealth); // Initial sync with conversion
        
        // Trigger change
        source.Health = 200;
        Assert.Equal(100, target.CurrentHealth); // Update with conversion
    }

    [Fact]
    public void AsFormattedString_ShouldApplyStringFormatConverter()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health)
            .AsFormattedString("HP: {0:F0}");
            
        // Act
        binding.Activate(target, "Status");
        
        // Assert
        Assert.Equal("HP: 100", target.Status);
        
        // Trigger change
        source.Health = 50;
        Assert.Equal("HP: 50", target.Status);
    }

    [Fact]
    public void TwoWay_ShouldSyncBothDirections()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health)
            .TwoWay();
            
        binding.Activate(target, "CurrentHealth");
        
        // Assert initial sync
        Assert.Equal(100, target.CurrentHealth);
        
        // Act: Change source
        source.Health = 50;
        Assert.Equal(50, target.CurrentHealth);
        
        // Act: Change target
        target.CurrentHealth = 75;
        Assert.Equal(75, source.Health);
    }

    [Fact]
    public void BindingFromParent_ShouldUseParentLookupStrategy()
    {
        // Arrange
        var parent = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        parent.AddChild(target);
        
        var binding = Binding.FromParent<TestSourceComponent>()
            .GetPropertyValue(p => p.Health);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(100, target.CurrentHealth);
        
        parent.Health = 50;
        Assert.Equal(50, target.CurrentHealth);
    }

    [Fact]
    public void BindingFromSibling_ShouldUseSiblingLookupStrategy()
    {
        // Arrange
        var parent = new TestTargetComponent();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        parent.AddChild(source);
        parent.AddChild(target);
        
        var binding = Binding.FromSibling<TestSourceComponent>()
            .GetPropertyValue(s => s.Health);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(100, target.CurrentHealth);
        
        source.Health = 75;
        Assert.Equal(75, target.CurrentHealth);
    }

    [Fact]
    public void BindingFromNamedObject_ShouldUseNamedLookupStrategy()
    {
        // Arrange
        var root = new TestTargetComponent();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent();
        
        source.SetName("HealthSource");
        root.AddChild(source);
        root.AddChild(target);
        
        var binding = Binding.FromNamedObject<TestSourceComponent>("HealthSource")
            .GetPropertyValue(s => s.Health);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(100, target.CurrentHealth);
        
        source.Health = 50;
        Assert.Equal(50, target.CurrentHealth);
    }

    [Fact]
    public void TwoWay_WithConverter_ShouldUseConvertBack()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent { CurrentHealth = 50 }; // 100 * 0.5
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var converterMock = new Mock<IBidirectionalConverter>();
        converterMock.Setup(c => c.Convert(100.0f)).Returns(50.0f);
        converterMock.Setup(c => c.ConvertBack(25.0f)).Returns(50.0f);

        var binding = new PropertyBinding<TestSourceComponent, float>(lookupMock.Object)
            .GetPropertyValue(s => s.Health)
            .WithConverter(converterMock.Object)
            .TwoWay();
            
        binding.Activate(target, "CurrentHealth");
        
        // Act
        target.CurrentHealth = 25.0f;
        
        // Assert
        Assert.Equal(50.0f, source.Health);
        converterMock.Verify(c => c.ConvertBack(25.0f), Times.Once);
    }

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
        private float _currentHealth;
        public float CurrentHealth 
        { 
            get => _currentHealth; 
            set 
            {
                float old = _currentHealth;
                _currentHealth = value;
                CurrentHealthChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<float>>? CurrentHealthChanged;
        
        public string? Status { get; set; }
    }
}
