using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Base class containing infrastructure properties for components.
/// This class does NOT implement IRuntimeComponent to prevent source generation
/// from processing infrastructure properties. Only derived classes that implement
/// IRuntimeComponent will have their properties auto-generated.
/// </summary>
public abstract class ComponentBase : INotifyPropertyChanged
{
    /// <summary>
    /// Factory used to create new components.
    /// </summary>
    public IComponentFactory? ComponentFactory { get; set; }

    /// <summary>
    /// Logger, for logging of course
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    private ComponentId _id = ComponentId.None;
    public ComponentId Id
    {
        get => _id;
        set
        {
            if (_id == value) return;

            _id = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Human-readable name for this component instance.
    /// </summary>
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;

            _name = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Whether this component is currently enabled and should participate in updates.
    /// </summary>
    private bool _enabled = true;
    public bool IsEnabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;

            _enabled = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Call this method when a property changes.
    /// </summary>
    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}