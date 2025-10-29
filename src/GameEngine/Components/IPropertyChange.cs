using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Components;

public interface IPropertyChange
{
    float Duration { get; set; }
    InterpolationMode Interpolation { get; set; }
}

public interface IPropertyChange<T> : IPropertyChange
{
    T CurrentValue { get; set; }
    T TargetValue { get; set; }
}