using Nexus.GameEngine.Components;

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
