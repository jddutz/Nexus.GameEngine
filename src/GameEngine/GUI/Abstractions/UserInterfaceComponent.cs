using Nexus.GameEngine.Graphics;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Base class for all user interface components.
/// Extends Transformable to add 2D UI-specific positioning and bounds management.
/// Provides layout system integration through SetBounds/GetBounds.
/// </summary>
public abstract partial class UserInterfaceComponent : DrawableComponent
{
    /// <summary>
    /// The 2D bounds of this UI component (position and size in screen space).
    /// Used by layout systems to position and size UI elements.
    /// </summary>
    private Rectangle<float> _bounds = new(0, 0, 0, 0);

    /// <summary>
    /// Sets the bounds (position and size) of this UI component.
    /// Called by layout components to position their children.
    /// Updates the 3D Position to match the 2D bounds position.
    /// </summary>
    /// <param name="bounds">The rectangle defining position and size in screen space</param>
    public virtual void SetBounds(Rectangle<float> bounds)
    {
        _bounds = bounds;
        
        // Sync 3D position with 2D bounds (Z = 0 for UI elements)
        SetPosition(new Vector3D<float>(bounds.Origin.X, bounds.Origin.Y, 0f), duration: 0f);
    }

    /// <summary>
    /// Gets the current bounds of this UI component.
    /// </summary>
    /// <returns>The rectangle defining current position and size in screen space</returns>
    public virtual Rectangle<float> GetBounds()
    {
        return _bounds;
    }
}
