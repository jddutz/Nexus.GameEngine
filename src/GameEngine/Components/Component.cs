namespace Nexus.GameEngine.Components;

public partial class Component
    : Configurable, IComponent, IDisposable
{
    /// <summary>
    /// Content manager used to create and manage subcomponents.
    /// </summary>
    public IContentManager? ContentManager { get; set; }

    public event EventHandler<EventArgs>? Unloading;
    public event EventHandler<EventArgs>? Unloaded;

    // Tree Management Events
    public event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    private readonly List<IComponent> _children = [];

    /// <summary>
    /// Parent component in the component tree.
    /// Internal setter prevents source generation while allowing tree management.
    /// </summary>
    public virtual IComponent? Parent { get; internal set; }

    /// <summary>
    /// Child components of this component.
    /// </summary>
    public virtual IEnumerable<IComponent> Children => _children;

    // Component tree management
    public virtual void AddChild(IComponent child)
    {
        if (!_children.Contains(child))
        {
            if (string.IsNullOrEmpty(child.Name)) child.Name = child.GetType().Name;

            _children.Add(child);

            // Set parent using concrete type to access internal setter
            if (child is Component component)
            {
                component.Parent = this;
            }

            ChildCollectionChanged?.Invoke(child, new()
            {
                Added = [child]
            });

        }
        else
        {
        }
    }

    public virtual IComponent? CreateChild(Type componentType)
    {
        var component = ContentManager?.Create(componentType);
        if (component == null) return null;

        AddChild(component);

        return component;
    }

    public virtual IComponent? CreateChild(Template template)
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
            if (child is RuntimeComponent runtimeChild)
            {
                runtimeChild.Parent = null;
            }

            ChildCollectionChanged?.Invoke(child, new()
            {
                Removed = [child]
            });

        }
        else
        {
        }
    }

    // Tree navigation methods (basic implementations)
    public virtual IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool depthFirst = false)
        where T : IComponent
    {
        if (depthFirst)
        {
            foreach (var child in Children)
            {
                foreach (var grandchild in child.GetChildren(filter, depthFirst))
                {
                    yield return grandchild;
                }

                if (child is T result && (filter == null || filter(result)))
                    yield return result;
            }
        }
        else
        {
            foreach (var child in Children)
            {
                if (child is T result && (filter == null || filter(result)))
                    yield return result;

                foreach (var grandchild in child.GetChildren(filter, depthFirst))
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
        return Parent.GetChildren(filter);
    }

    public virtual T? FindParent<T>(Func<T, bool>? filter = null)
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

    protected virtual void OnUnload() { }

    public void Unload()
    {
        if (!IsLoaded) return;

        Unloading?.Invoke(this, EventArgs.Empty);

        // Deactivate leaf to root
        foreach (var child in Children)
        {
            child.Unload();
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
        // ToArray so we can modify the collection while iterating
        foreach (var child in Children.OfType<IDisposable>().ToArray()) 
        {
            child.Dispose();
        }

        // Clear the children collection
        _children.Clear();

        // Call component-specific disposal logic
        OnDispose();
    }

    /// <summary>
    /// Helper method for canceling animations on layout-affecting properties.
    /// Use with [ComponentProperty(BeforeChange = nameof(CancelAnimation))] attribute.
    /// Forces immediate updates by setting duration to 0 and interpolation mode to Step.
    /// </summary>
    protected void CancelAnimation<T>(ref T newValue, ref float duration, ref InterpolationMode mode)
    {
        duration = 0f;
        mode = InterpolationMode.Step;
    }
}