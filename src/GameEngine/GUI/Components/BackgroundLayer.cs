using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

public partial class BackgroundLayer()
    : RuntimeComponent, IRenderable, IBackgroundController
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
    private Vector4D<float> _backgroundColor = Colors.CornflowerBlue;

    public uint RenderPriority => 0;

    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    public uint RenderPassFlags => 1;

    public IEnumerable<ElementData> GetElements()
    {
        throw new NotImplementedException();
        /*
        Logger?.LogDebug("BackgroundLayer.OnRender called - IsVisible: {IsVisible}, BackgroundColor: {BackgroundColor}", IsVisible, BackgroundColor);

        if (!IsVisible)
        {
            Logger?.LogDebug("BackgroundLayer not visible, skipping render");
            yield break;
        }

        // Get resources using our new resource system
        Logger?.LogDebug("Creating resources for BackgroundLayer");
        var geometryResource = resourceManager.GetOrCreateResource(
            GeometryDefinitions.FullScreenQuad);
        var shaderResource = 0u;  //resourceManager.GetOrCreateResource(Shaders.BackgroundSolid);

        Logger?.LogDebug("Created geometry resource: {GeometryResource}, shader resource: {ShaderResource}", geometryResource, shaderResource);

        yield return new ElementData()
        {
            // TODO: update these values
            Vao = 0,
            Vbo = 0,
            Ebo = 0,
            Shader = 0,
            ShaderProgram = shaderResource,
            VertexArray = geometryResource,
            Priority = RenderPriority
            // No uniforms needed for basic orange color shader
        };
        */
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

    // IBackgroundController implementation - properties are automatically deferred
    public void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    public void SetBackgroundColor(Vector4D<float> color)
    {
        BackgroundColor = color;
    }

    public void SetBackgroundColor(float r, float g, float b, float a = 1.0f)
    {
        BackgroundColor = new Vector4D<float>(r, g, b, a);
    }

    public void SetBackgroundColor(string colorName)
    {
        // Use reflection to get predefined color from Colors class
        var colorProperty = typeof(Colors).GetProperty(colorName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (colorProperty?.GetValue(null) is Vector4D<float> color)
        {
            BackgroundColor = color;
        }
        else
        {
            // Fallback to default color if name not found
            BackgroundColor = Colors.White;
        }
    }

    public void FadeToColor(Vector4D<float> targetColor, float factor)
    {
        // Linear interpolation between current and target color
        var current = BackgroundColor;
        BackgroundColor = new Vector4D<float>(
            current.X + (targetColor.X - current.X) * factor,
            current.Y + (targetColor.Y - current.Y) * factor,
            current.Z + (targetColor.Z - current.Z) * factor,
            current.W + (targetColor.W - current.W) * factor
        );
    }
}