using System;
using System.Collections.Generic;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components capable of parent-child relationship management.
/// Named "Hierarchy" rather than "Tree" because property bindings and other features
/// can create non-tree structures (cycles, multiple parents via bindings).
/// </summary>
public interface IHierarchical
{
    // Management
    IContentManager? ContentManager { get; set; }
    
    // Relationships
    IComponent? Parent { get; set; }
    IEnumerable<IComponent> Children { get; }
    
    // Modification
    void AddChild(IComponent child);
    void RemoveChild(IComponent child);
    IComponent? CreateChild(Type componentType);
    IComponent? CreateChild(Template template);
    
    // Navigation
    IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool recursive = false, bool depthFirst = false) 
        where T : IComponent;
    T? GetParent<T>(Func<T, bool>? filter = null) 
        where T : IComponent;
    IComponent GetRoot();
    
    // Events
    event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;
}
