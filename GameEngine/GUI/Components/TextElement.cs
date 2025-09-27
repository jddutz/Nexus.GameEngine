using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Component Template
/// </summary>
public record TextElementTemplate
{
}

/// <summary>
/// A UI component that displays text.
/// </summary>
public class TextElement : RuntimeComponent, IRenderable
{
    public string? Text { get; set; }

    public bool IsVisible { get; set; } = true;
    public bool ShouldRender => IsVisible;
    public int RenderPriority => 450; // UI text layer

    /// <summary>
    /// Bounding box for text elements. Returns minimal box since these are UI elements.
    /// </summary>
    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    /// <summary>
    /// Text elements participate in UI render pass (pass 1).
    /// </summary>
    public uint RenderPassFlags => 1u << 1; // UI pass

    /// <summary>
    /// Text elements are leaf components and don't render children.
    /// </summary>
    public bool ShouldRenderChildren => false;

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        // TODO: Implement text rendering using direct GL calls
    }
}