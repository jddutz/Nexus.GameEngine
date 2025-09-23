using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Interface for components that can be positioned and sized by layout systems.
/// </summary>
public interface ILayoutable
{
    /// <summary>
    /// Sets the bounds (position and size) of this component.
    /// Called by layout components to position their children.
    /// </summary>
    /// <param name="bounds">The rectangle defining position and size</param>
    void SetBounds(Rectangle<float> bounds);

    /// <summary>
    /// Gets the current bounds of this component.
    /// </summary>
    Rectangle<float> GetBounds();
}