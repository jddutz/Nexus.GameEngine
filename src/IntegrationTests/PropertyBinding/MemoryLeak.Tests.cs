using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Xunit;
using IntegrationTests.PropertyBinding; // For ManualBindings

namespace IntegrationTests.PropertyBinding;

public class MemoryLeakTests
{
    public class LeakTestComponent : RuntimeComponent
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

    [Fact]
    public void Binding_ShouldNotLeak_AfterDeactivation()
    {
        // Arrange
        var source = new LeakTestComponent();
        WeakReference targetRef = null!;
        
        void RunScope()
        {
            var target = new LeakTestComponent();
            targetRef = new WeakReference(target);
            
            var bindings = new ManualBindings();
            bindings.Add("Value", Binding.FromParent<LeakTestComponent>(p => p.Value));
            
            source.Load(new Template());
            
            // Manually set parent to enable lookup, but DO NOT add to source.Children
            // This ensures source only holds reference via event handler (if leaked)
            target.Parent = source;
            
            target.Load(new Template { Bindings = bindings });
            
            // Act: Cycle activation
            // Note: Activate() usually cascades to children, but since it's not in Children list,
            // we must activate target manually.
            // However, PropertyBinding.Activate is called by RuntimeComponent.OnActivate.
            
            for (int i = 0; i < 1000; i++)
            {
                // We need to simulate the lifecycle
                // Since target is not in source.Children, source.Activate() won't touch target.
                // We must call target.Activate() / target.Deactivate().
                
                // But wait, PropertyBinding subscribes to SOURCE event.
                // So when target activates, it subscribes to source.
                // When target deactivates, it should unsubscribe.
                
                target.Activate();
                target.Deactivate();
            }
        }
        
        RunScope();
        
        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Assert
        Assert.False(targetRef.IsAlive, "Target component should be garbage collected");
    }
}
