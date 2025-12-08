# API Contract: IPropertyBinding

**Namespace**: `Nexus.GameEngine.Components`  
**Purpose**: Non-generic interface for property binding lifecycle management

## Interface Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Non-generic interface for property bindings to enable collection storage and lifecycle management.
/// </summary>
public interface IPropertyBinding
{
    /// <summary>
    /// Activates the binding by resolving source component and subscribing to property changes.
    /// </summary>
    /// <remarks>
    /// Called during Component.OnActivate() lifecycle phase.
    /// - Resolves source component using configured lookup strategy
    /// - Subscribes to source's property change event
    /// - Performs initial synchronization (source → target)
    /// - If TwoWay mode, subscribes to target's property change event
    /// - Silently skips activation if source component not found (logs warning)
    /// </remarks>
    void Activate();
    
    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    /// <remarks>
    /// Called during Component.OnDeactivate() lifecycle phase.
    /// - Unsubscribes from source PropertyChanged event
    /// - Unsubscribes from target PropertyChanged event (TwoWay mode)
    /// - Clears cached component references to prevent memory leaks
    /// - Binding can be reactivated after deactivation
    /// </remarks>
    void Deactivate();
}
```

## Usage Pattern

```csharp
// Component manages bindings collection
public class Component : IComponent
{
    public static List<IPropertyBinding> PropertyBindings { get; } = [];
    
    protected virtual void OnActivate()
    {
        foreach(var binding in PropertyBindings)
        {
            binding.Activate();
        }
    }
    
    protected virtual void OnDeactivate()
    {
        foreach(var binding in PropertyBindings)
        {
            binding.Deactivate();
        }
    }
}
```

## Lifecycle Contract

1. **Creation**: PropertyBinding instances created during Component.OnLoad() from template definitions
2. **Activation**: Called during Component.OnActivate() to establish event subscriptions
3. **Active**: Binding listens to source property changes and updates target
4. **Deactivation**: Called during Component.OnDeactivate() to cleanup subscriptions
5. **Inactive**: Binding can be reactivated or disposed

## Thread Safety

- ❌ **NOT thread-safe**: All operations must occur on the main thread
- Component lifecycle methods (OnActivate, OnDeactivate) are called sequentially on main thread
- Event handlers execute on the thread that raised the event (typically main thread for UI components)

## Error Handling

- **Activation failure**: Silently skips if source component not found (logs warning via ILogger if available)
- **Event not found**: Logs warning, binding won't update but doesn't throw
- **Deactivation**: Always succeeds (no-op if already inactive)

## Performance Guarantees

- **Activation**: <1ms per binding for typical tree depths (<10 ancestors)
- **Deactivation**: <0.1ms per binding (event unsubscription)
- **Memory**: Binding holds strong references during active phase, released on deactivation
