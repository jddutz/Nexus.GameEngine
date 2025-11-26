namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Interface for layout containers that arrange child components.
/// Extends IUserInterfaceElement to participate in parent layouts while also managing child layouts.
/// </summary>
public interface ILayout : IUserInterfaceElement
{
    /// <summary>
    /// Called when a child's size changes (for intrinsic sizing support).
    /// </summary>
    /// <param name="child">The child component whose size changed.</param>
    /// <param name="oldValue">The previous size value.</param>
    void OnChildSizeChanged(IComponent child, Vector2D<int> oldValue);
}