using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.GUI.Components;

public class BackgroundLayer(IResourceManager resourceManager)
    : RuntimeComponent, IRenderable
{
    public new record Template : RuntimeComponent.Template
    {
        public bool IsVisible { get; set; }
        public Vector4D<float> BackgroundColor { get; set; }
    }

    public bool IsVisible { get; set; } = true;

    public uint RenderPriority => 0;

    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    public uint RenderPassFlags => 1;

    public Vector4D<float> BackgroundColor { get; set; } = Colors.CornflowerBlue;

    public IEnumerable<RenderState> OnRender(double deltaTime)
    {
        // BackgroundLayer no longer directly calls GL methods
        // Background clearing is now handled by RenderPassConfiguration.FillColor
        // This component can be removed or repurposed for other background rendering needs
        
        // For now, return empty to not interfere with the new render pass clearing
        return System.Linq.Enumerable.Empty<RenderState>();
    }
}