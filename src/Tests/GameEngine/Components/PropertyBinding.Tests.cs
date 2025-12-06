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
        
        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health");
            
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
        
        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health");
            
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

        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
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
        
        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
            .AsFormattedString("HP: {0}");
            
        // Act
        binding.Activate(target, "Status");
        
        // Assert
        Assert.Equal("HP: 100", target.Status);
        
        // Trigger change
        source.Health = 50;
        Assert.Equal("HP: 50", target.Status);
    }

    [Fact]
    public void Activate_ShouldSkipUpdate_WhenConverterReturnsNull()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent { CurrentHealth = 10 };
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var converterMock = new Mock<IValueConverter>();
        converterMock.Setup(c => c.Convert(It.IsAny<object>())).Returns((object?)null);

        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
            .WithConverter(converterMock.Object);
            
        // Act
        binding.Activate(target, "CurrentHealth");
        
        // Assert
        Assert.Equal(10, target.CurrentHealth); // Should not have changed
        
        // Trigger change
        source.Health = 200;
        Assert.Equal(10, target.CurrentHealth); // Should still not change
    }

    [Fact]
    public void FromNamedObject_ShouldCreateBinding()
    {
        // Act
        var binding = PropertyBinding.FromNamedObject("SourceName");
        
        // Assert
        Assert.NotNull(binding);
    }

    [Fact]
    public void TwoWay_ShouldUpdateSource_WhenTargetChanges()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent { CurrentHealth = 100 };
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
            .TwoWay();
            
        binding.Activate(target, "CurrentHealth");
        
        // Act
        target.CurrentHealth = 50;
        
        // Assert
        Assert.Equal(50, source.Health);
    }

    [Fact]
    public void TwoWay_ShouldPreventInfiniteLoops()
    {
        // Arrange
        var lookupMock = new Mock<ILookupStrategy>();
        var source = new TestSourceComponent { Health = 100 };
        var target = new TestTargetComponent { CurrentHealth = 100 };
        
        lookupMock.Setup(l => l.Resolve(target)).Returns(source);
        
        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
            .TwoWay();
            
        binding.Activate(target, "CurrentHealth");
        
        // Act
        // This would cause a stack overflow if loop prevention is missing
        // Source -> Target -> Source -> Target ...
        source.Health = 50;
        
        // Assert
        Assert.Equal(50, target.CurrentHealth);
        Assert.Equal(50, source.Health);
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

        var binding = new PropertyBinding(lookupMock.Object)
            .GetPropertyValue("Health")
            .WithConverter(converterMock.Object)
            .TwoWay();
            
        binding.Activate(target, "CurrentHealth");
        
        // Act
        target.CurrentHealth = 25.0f;
        
        // Assert
        Assert.Equal(50.0f, source.Health);
        converterMock.Verify(c => c.ConvertBack(25.0f), Times.Once);
    }

    private class TestSourceComponent : RuntimeComponent
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
    
    private class TestTargetComponent : RuntimeComponent
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
