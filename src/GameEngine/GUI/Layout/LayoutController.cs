using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Base class for layout controllers that arrange sibling components.
/// </summary>
public abstract partial class LayoutController : RuntimeComponent
{
    /// <summary>
    /// Updates the layout of the specified container's children.
    /// </summary>
    /// <param name="container">The container whose children should be laid out.</param>
    public abstract void UpdateLayout(UserInterfaceElement container);
}
