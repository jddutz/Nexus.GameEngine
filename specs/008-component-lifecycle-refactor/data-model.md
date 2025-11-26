# Data Model: Component Lifecycle Refactor

## Interfaces

### `Nexus.GameEngine.GUI.IUserInterfaceElement`

```csharp
namespace Nexus.GameEngine.GUI;

/// <summary>
/// Represents a UI element that requires layout updates.
/// </summary>
public interface IUserInterfaceElement
{
    /// <summary>
    /// Updates the layout of this element and its children.
    /// Called by ContentManager before Validation and Activation.
    /// </summary>
    void UpdateLayout();
}
```

## Classes

### `Nexus.GameEngine.Components.Configurable`

```csharp
public abstract partial class Configurable : Entity, IConfigurable
{
    // ... existing events ...

    /// <summary>
    /// Orchestrates the loading lifecycle.
    /// Non-virtual to enforce order.
    /// </summary>
    public void Load(Template template)
    {
        Loading?.Invoke(this, new(template));
        
        // 1. Apply properties (Root -> Leaf)
        Configure(template);
        
        // 2. Run hooks (Root -> Leaf, usually)
        OnLoad(template);
        
        // 3. Finalize
        IsLoaded = true;
        Loaded?.Invoke(this, new(template));
    }

    /// <summary>
    /// Applies template properties to the component.
    /// Generated overrides MUST call base.Configure(template) first.
    /// </summary>
    protected virtual void Configure(Template template) { }
    
    // ... existing OnLoad ...
}
```

### `Nexus.GameEngine.Runtime.ContentManager`

```csharp
namespace Nexus.GameEngine.Runtime;

public class ContentManager : IContentManager
{
    // ...
    
    public IComponent? Load(Template template, bool activate = true)
    {
        // ... creation ...
        var component = CreateInstance(template);
        
        if (component != null && activate)
        {
            // 1. Layout
            if (component is IUserInterfaceElement ui)
            {
                ui.UpdateLayout();
            }
            // Recursive layout update might be needed if not handled by root
            
            // 2. Validate
            if (component is IConfigurable configurable)
            {
                configurable.Validate(); 
                // Note: Validate() is recursive or needs to be called recursively?
                // Current implementation of Validate() is not recursive but Validating event might be.
                // Need to check if we need explicit recursive validation here.
            }
            
            // 3. Activate
            ActivateComponentTree(component);
        }
        
        return component;
    }
}
```
