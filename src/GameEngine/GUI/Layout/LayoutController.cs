using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Base class for layout controllers that arrange sibling components.
/// Provides alignment-based positioning for children.
/// </summary>
public partial class LayoutController : Component
{
    /// <summary>
    /// Updates the layout of the specified container's children.
    /// Default implementation applies alignment-based positioning to each child.
    /// </summary>
    /// <param name="container">The container whose children should be laid out.</param>
    public virtual void UpdateLayout(UserInterfaceElement container)
    {
        if (container == null)
        {
            return;
        }

        var contentArea = container.GetContentRect();
        var children = new System.Collections.Generic.List<UserInterfaceElement>();
        
        foreach (var child in container.Children)
        {
            // Exclude LayoutController instances - they control layout, they are not laid out
            if (child is UserInterfaceElement uiElement && child is not LayoutController)
            {
                children.Add(uiElement);
            }
        }

        // Default: Let each child use its own Alignment property
        foreach (var child in children)
        {
            child.UpdateLayout(contentArea);
        }
    }
}
