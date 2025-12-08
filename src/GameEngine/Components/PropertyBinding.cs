using System;
using Nexus.GameEngine.Components.Lookups;

namespace Nexus.GameEngine.Components;

public class PropertyBinding<TSource, TTarget> : IPropertyBinding 
    where TSource : class, IComponent
    where TTarget : class, IComponent
{
    private readonly Func<IComponent, TSource?> _lookup;
    private readonly Action<TSource, TTarget> _subscribe;
    private readonly Action<TSource, TTarget>? _unsubscribe;
    
    private TSource? _source;
    private TTarget? _target;
    
    public PropertyBinding(
        Action<TSource, TTarget> subscribe,
        Action<TSource, TTarget>? unsubscribe = null,
        Func<IComponent, TSource?>? lookup = null)
    {
        _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
        _unsubscribe = unsubscribe;
        _lookup = lookup ?? ((Func<IComponent, TSource?>)(c => new ParentLookup<TSource>().Resolve(c) as TSource));
    }
    
    public void Activate(IComponent target)
    {
        _target = target as TTarget;
        if (_target == null)
        {
            // Log warning: target component is not of type TTarget
            return;
        }
        
        _source = _lookup(target);
        if (_source == null)
        {
            // Log warning: source component not found
            return;
        }
        
        _subscribe(_source, _target);
    }
    
    public void Deactivate()
    {
        if (_source != null && _target != null)
        {
            _unsubscribe?.Invoke(_source, _target);
        }
        
        _source = null;
        _target = null;
    }
}