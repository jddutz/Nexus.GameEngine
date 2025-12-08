using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.GameEngine.Components;

public partial class Component
{
    /// <summary>
    /// Content manager used to create and manage subcomponents.
    /// </summary>
    public IContentManager? ContentManager { get; set; }

    public event EventHandler? Unloading;
    public event EventHandler? Unloaded;

    // Tree Management Events
    public event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    private readonly List<IComponent> _children = [];

    /// <summary>
    /// Parent component in the component tree.
    /// </summary>
    public virtual IComponent? Parent { get; set; }

    /// <summary>
    /// Child components of this component.
    /// </summary>
    public virtual IEnumerable<IComponent> Children => _children;

    // Component tree management
    public virtual void AddChild(IComponent child)
    {
        if (!_children.Contains(child))
        {
            // If child already has a parent, remove it from there first
            if (child.Parent != null && child.Parent != this)
            {
                // Cast to IHierarchical to access RemoveChild if IComponent doesn't have it directly
                // But Component implements IComponent which implements IHierarchical
                if (child.Parent is IHierarchical parent)
                {
                    parent.RemoveChild(child);
                }
            }

            _children.Add(child);

            child.Parent = this;

            ChildCollectionChanged?.Invoke(child, new()
            {
                Added = [child]
            });

        }
    }

    public virtual IComponent? CreateChild(Type componentType)
    {
        var component = ContentManager?.Create(componentType);
        if (component == null) return null;

        AddChild(component);

        return component;
    }

    public virtual IComponent? CreateChild(ComponentTemplate template)
    {
        if (template == null)
        {
            return null;
        }

        var component = ContentManager?.CreateInstance(template);

        if (component == null) return null;

        AddChild(component);

        return component;
    }

    public virtual void RemoveChild(IComponent child)
    {
        if (_children.Remove(child))
        {

            // Clear parent using concrete type to access internal setter
            if (child is Component component)
            {
                component.Parent = null;
            }

            ChildCollectionChanged?.Invoke(child, new()
            {
                Removed = [child]
            });

        }
    }

    // Tree navigation methods
    public virtual IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool recursive = false, bool depthFirst = false)
        where T : IComponent
    {
        if (!recursive)
        {
            // Non-recursive: only immediate children
            foreach (var child in Children)
            {
                if (child is T result && (filter == null || filter(result)))
                    yield return result;
            }
        }
        else if (depthFirst)
        {
            // Recursive depth-first
            foreach (var child in Children)
            {
                foreach (var grandchild in child.GetChildren(filter, recursive: true, depthFirst))
                {
                    yield return grandchild;
                }

                if (child is T result && (filter == null || filter(result)))
                    yield return result;
            }
        }
        else
        {
            // Recursive breadth-first
            foreach (var child in Children)
            {
                if (child is T result && (filter == null || filter(result)))
                    yield return result;

                foreach (var grandchild in child.GetChildren(filter, recursive: true, depthFirst))
                {
                    yield return grandchild;
                }
            }
        }
    }

    public virtual IEnumerable<T> GetSiblings<T>(Func<T, bool>? filter = null)
        where T : IComponent
    {
        if (Parent == null) return [];
        return Parent.GetChildren(filter, recursive: false);
    }

    public virtual T? GetParent<T>(Func<T, bool>? filter = null)
        where T : IComponent
    {
        var current = Parent;

        while (current != null)
        {
            if (current is T typed && (filter == null || filter(typed)))
                return typed;

            current = current.Parent;
        }

        return default;
    }
    
    public IComponent GetRoot()
    {
        var current = (IComponent)this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    protected virtual void OnUnload() { }

    public void Unload()
    {
        if (!IsLoaded) return;

        Unloading?.Invoke(this, EventArgs.Empty);

        // Deactivate leaf to root
        foreach (var child in Children)
        {
            if (child is ILoadable loadable)
            {
                 loadable.Unload();
            }
        }

        OnUnload();

        IsLoaded = false;

        Unloaded?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void OnDispose() { }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Dispose children first (leaf to root)
        foreach (var child in Children.OfType<IDisposable>().ToArray()) 
        {
            child.Dispose();
        }

        // Clear the children collection
        _children.Clear();

        // Call component-specific disposal logic
        OnDispose();
    }
}
