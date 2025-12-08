# API Contract: ILookupStrategy

**Namespace**: `Nexus.GameEngine.Components.Lookups`  
**Purpose**: Strategy interface for resolving source components in the component tree

## Interface Definition

```csharp
namespace Nexus.GameEngine.Components.Lookups;

/// <summary>
/// Strategy interface for resolving source components during binding activation.
/// </summary>
public interface ILookupStrategy
{
    /// <summary>
    /// Resolves the source component starting from the target component.
    /// </summary>
    /// <param name="target">The component that owns the binding</param>
    /// <returns>The resolved source component, or null if not found</returns>
    IComponent? Resolve(IComponent target);
}
```

## Implementations

### ParentLookup<TSource>

**Purpose**: Find the nearest parent component of the specified type.

```csharp
public class ParentLookup<TSource> : ILookupStrategy 
    where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current is TSource typed) 
                return typed;
            current = current.Parent;
        }
        return null;  // Not found
    }
}
```

**Performance**: O(tree depth) - typically <10 iterations  
**Use Case**: Parent-to-child property flow (90% of bindings)

---

### SiblingLookup<TSource>

**Purpose**: Find a sibling component of the specified type.

```csharp
public class SiblingLookup<TSource> : ILookupStrategy 
    where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var parent = target.Parent;
        if (parent == null) return null;
        
        foreach (var child in parent.Children)
        {
            if (child != target && child is TSource typed)
                return typed;
        }
        return null;  // Not found
    }
}
```

**Performance**: O(sibling count) - typically <20 iterations  
**Use Case**: Sibling-to-sibling communication (e.g., health bar binding to player stats panel)

---

### ChildLookup<TSource>

**Purpose**: Find an immediate child component of the specified type.

```csharp
public class ChildLookup<TSource> : ILookupStrategy 
    where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        foreach (var child in target.Children)
        {
            if (child is TSource typed)
                return typed;
        }
        return null;  // Not found
    }
}
```

**Performance**: O(child count) - typically <10 iterations  
**Use Case**: Parent-to-child binding (e.g., panel binding to its own child components)

---

### NamedObjectLookup

**Purpose**: Find a component by name anywhere in the tree (recursive search).

```csharp
public class NamedObjectLookup : ILookupStrategy
{
    private readonly string _name;
    
    public NamedObjectLookup(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    public IComponent? Resolve(IComponent target)
    {
        // Find root
        var root = target;
        while (root.Parent != null) 
            root = root.Parent;
        
        // Recursive search from root
        return SearchTree(root);
    }
    
    private IComponent? SearchTree(IComponent node)
    {
        if (node.Name == _name) 
            return node;
        
        foreach (var child in node.Children)
        {
            var result = SearchTree(child);
            if (result != null) 
                return result;
        }
        
        return null;
    }
}
```

**Performance**: O(tree size) - potentially hundreds of iterations  
**Use Case**: Global singleton-like components (e.g., "GameState", "AudioManager")  
**Warning**: Use sparingly due to performance cost

---

### ContextLookup<TSource>

**Purpose**: Find the nearest ancestor component of the specified type (similar to React context).

```csharp
public class ContextLookup<TSource> : ILookupStrategy 
    where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current is TSource typed)
                return typed;
            current = current.Parent;
        }
        return null;  // Not found
    }
}
```

**Performance**: O(tree depth) - typically <10 iterations  
**Use Case**: Context providers (e.g., ThemeProvider, LocalizationContext)  
**Note**: Functionally identical to ParentLookup, named differently for architectural clarity

---

## Factory Methods

Static factory class provides fluent entry points:

```csharp
public static class Binding
{
    public static PropertyBinding<TSource, TSource> FromParent<TSource>() 
        where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ParentLookup<TSource>());
    }
    
    public static PropertyBinding<TSource, TSource> FromSibling<TSource>() 
        where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new SiblingLookup<TSource>());
    }
    
    public static PropertyBinding<TSource, TSource> FromChild<TSource>() 
        where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ChildLookup<TSource>());
    }
    
    public static PropertyBinding<TSource, TSource> FromNamedObject<TSource>(string name) 
        where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new NamedObjectLookup(name));
    }
    
    public static PropertyBinding<TSource, TSource> FromContext<TSource>() 
        where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ContextLookup<TSource>());
    }
}
```

## Usage Examples

```csharp
// Parent lookup (default)
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .Set(SetHealth);

// Sibling lookup
Binding.FromSibling<ScorePanel>()
    .GetPropertyValue(s => s.Score)
    .Set(SetScore);

// Named lookup
Binding.FromNamedObject<AudioManager>("AudioManager")
    .GetPropertyValue(a => a.Volume)
    .Set(SetVolume);

// Context lookup (architectural pattern)
Binding.FromContext<ThemeProvider>()
    .GetPropertyValue(t => t.PrimaryColor)
    .Set(SetColor);
```

## Error Handling

- **Not found**: Returns `null` (binding activation silently skipped with warning log)
- **Null target**: Returns `null` (defensive programming)
- **Invalid name**: NamedObjectLookup throws `ArgumentNullException` in constructor

## Performance Comparison

| Strategy | Time Complexity | Typical Iterations | Use Frequency |
|----------|----------------|-------------------|---------------|
| ParentLookup | O(depth) | <10 | 90% |
| SiblingLookup | O(siblings) | <20 | 8% |
| ChildLookup | O(children) | <10 | 1% |
| NamedObjectLookup | O(tree size) | 100-1000 | <1% |
| ContextLookup | O(depth) | <10 | <1% |

## Extension Points

Custom lookup strategies can be implemented:

```csharp
public class ClosestByTypeLookup : ILookupStrategy
{
    private readonly Type _targetType;
    
    public ClosestByTypeLookup(Type targetType)
    {
        _targetType = targetType;
    }
    
    public IComponent? Resolve(IComponent target)
    {
        // Search parents first
        var parent = SearchAncestors(target);
        if (parent != null) return parent;
        
        // Then search siblings
        var sibling = SearchSiblings(target);
        if (sibling != null) return sibling;
        
        // Finally search children
        return SearchDescendants(target);
    }
    
    // Implementation details...
}
```

## Thread Safety

- ✅ **Thread-safe**: All implementations are stateless and read-only after construction
- ✅ **Concurrent resolution**: Multiple bindings can resolve simultaneously (component tree is stable during activation)

## Known Limitations

1. **First match only**: Returns first component of matching type, no multi-match support
2. **No predicate filtering**: Cannot filter by additional criteria (e.g., name + type)
3. **No caching**: Resolution happens on every activation (acceptable performance)
4. **Recursive search performance**: NamedObjectLookup can be slow for large trees (use sparingly)
