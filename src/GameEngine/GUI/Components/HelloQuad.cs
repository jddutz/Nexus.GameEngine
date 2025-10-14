using Microsoft.Extensions.Options;
using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

public partial class HelloQuad(IOptions<VulkanSettings> vulkanSettings)
    : RenderableBase(vulkanSettings), IRenderable
{
    public new record Template : RuntimeComponent.Template
    {
        public bool IsVisible { get; set; }
        public Vector4D<float> BackgroundColor { get; set; }
    }

    // ComponentProperty fields - generator creates public properties with deferred updates
    [ComponentProperty]
    private bool _isVisible = true;

    [ComponentProperty(Duration = AnimationDuration.Normal, Interpolation = InterpolationMode.CubicEaseInOut)]
    private Vector4D<float> _backgroundColor = new(0.39f, 0.58f, 0.93f, 1.0f); // CornflowerBlue

    public uint RenderPriority => 0;

    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    public uint RenderPassFlags => 1;

    public IEnumerable<DrawCommand> GetElements()
    {
        return [];
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            // Direct field assignment during configuration - no deferred updates needed
            _isVisible = template.IsVisible;
            _backgroundColor = template.BackgroundColor;
        }
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    public void SetBackgroundColor(Vector4D<float> color)
    {
        BackgroundColor = color;
    }
}