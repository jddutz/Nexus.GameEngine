using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Graphics.Rendering;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// UserInterface components represent different game modes, menu screens, scenes, etc.
/// They provide a set of renderable components and user interactions available in a given context.
/// 
/// The main purpose of UserInterface is to be the root node for a game mode or screen.
/// When its OnRender method is called, it subsequently calls OnRender on each of its IRenderable
/// subcomponents in insertion order (3D rendering uses depth-based ordering).
/// </summary>
public class UserInterface(IRenderer renderer)
    : RuntimeComponent, IUserInterface
{

    /// <summary>
    /// Template for configuring UserInterface components.
    /// Defines the composition of UI elements for different game modes, menu screens, scenes, etc.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
    }

    private readonly IRenderer _renderer = renderer;

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template) { }
    }

    public void Render()
    {
        Render(0); // Call the deltaTime version with 0 for backward compatibility
    }

    public void Render(double deltaTime)
    {
        foreach (var child in Children)
        {
            if (child is IRenderable r)
            {
                r.OnRender(_renderer, deltaTime);
            }
        }

        // Note: IRenderer.RenderFrame() should handle the actual GL frame rendering
        // This method just calls OnRender on child components
    }
}