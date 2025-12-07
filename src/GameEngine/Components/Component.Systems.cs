using Nexus.GameEngine.Runtime.Systems;

namespace Nexus.GameEngine.Components;

public partial class Component
{
    /// <summary>
    /// Gets the graphics system for rendering operations.
    /// </summary>
    public IGraphicsSystem Graphics { get; internal set; } = null!;

    /// <summary>
    /// Gets the resource system for managing assets.
    /// </summary>
    public IResourceSystem Resources { get; internal set; } = null!;

    /// <summary>
    /// Gets the content system for loading and managing content.
    /// </summary>
    public IContentSystem Content { get; internal set; } = null!;

    /// <summary>
    /// Gets the window system for window management.
    /// </summary>
    public IWindowSystem Window { get; internal set; } = null!;

    /// <summary>
    /// Gets the input system for handling user input.
    /// </summary>
    public IInputSystem Input { get; internal set; } = null!;
}
