using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class ParentChildBindingTests
{
    [Fact]
    public void ShouldSyncPropertyFromParentToChild()
    {
        // Arrange
        var parent = new ParentComponent { Health = 100 };
        
        // Manual bindings configuration
        var bindings = new ManualBindings
        {
            CurrentHealth = Nexus.GameEngine.Components.PropertyBinding.FromParent<ParentComponent>()
                .GetPropertyValue(nameof(ParentComponent.Health))
        };

        var childTemplate = new Template
        {
            ComponentType = typeof(ChildComponent),
            Bindings = bindings
        };

        // We need a ContentManager to create the child properly?
        // Component.CreateChild uses ContentManager.
        // If ContentManager is null, CreateChild returns null.
        // So we need to mock ContentManager or set it.
        // Or we can manually instantiate child and add it, then call Load.
        
        // Manual instantiation approach to avoid mocking ContentManager complexity if possible
        var child = new ChildComponent();
        // We need to set the template on the child?
        // Component.Load(template) does this.
        
        parent.Load(new Template()); // Load parent first
        parent.AddChild(child);
        Assert.Same(parent, child.Parent);
        
        child.Load(childTemplate);
        
        // Act
        parent.Activate();
        
        Assert.True(parent.IsLoaded, "Parent should be loaded");
        Assert.True(parent.IsValid(), "Parent should be valid");
        // Assert.True(parent.Active, "Parent.Active should be true"); // Commented out in case it's not visible
        Assert.True(parent.IsActive(), "Parent should be active");
        Assert.True(child.IsActive(), "Child should be active");
        
        // Assert - Initial Sync
        Assert.Equal(100, child.CurrentHealth);
        
        // Act - Update
        parent.Health = 50;
        
        // Assert - Update Sync
        Assert.Equal(50, child.CurrentHealth);
        
        // Act - Deactivate
        parent.Deactivate();
        parent.Health = 25;
        
        // Assert - No Update after Deactivate
        Assert.Equal(50, child.CurrentHealth);
    }

    public class ParentComponent : RuntimeComponent
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

    public class ChildComponent : RuntimeComponent
    {
        public float CurrentHealth { get; set; }
    }

    public class ManualBindings : PropertyBindings
    {
        public Nexus.GameEngine.Components.PropertyBinding? CurrentHealth { get; init; }
        
        public override IEnumerator<(string propertyName, Nexus.GameEngine.Components.PropertyBinding binding)> GetEnumerator()
        {
            if (CurrentHealth != null) yield return ("CurrentHealth", CurrentHealth);
        }
    }
}
