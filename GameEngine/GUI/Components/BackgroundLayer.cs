using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

public class BackgroundLayer(IResourceManager resourceManager)
    : RuntimeComponent, IRenderable, IBackgroundController
{
    public new record Template : RuntimeComponent.Template
    {
        public bool IsVisible { get; set; }
        public Vector4D<float> BackgroundColor { get; set; }
    }

    // Private fields for deferred updates
    private bool _isVisible = true;
    private Vector4D<float> _backgroundColor = Colors.CornflowerBlue;

    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                NotifyPropertyChanged();
            }
        }
    }

    public uint RenderPriority => 0;

    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    public uint RenderPassFlags => 1;

    public Vector4D<float> BackgroundColor
    {
        get => _backgroundColor;
        private set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                NotifyPropertyChanged();
            }
        }
    }

    public IEnumerable<GLState> OnRender(IViewport viewport, double deltaTime)
    {
        Logger?.LogDebug("BackgroundLayer.OnRender called - IsVisible: {IsVisible}, BackgroundColor: {BackgroundColor}", IsVisible, BackgroundColor);

        if (!IsVisible)
        {
            Logger?.LogDebug("BackgroundLayer not visible, skipping render");
            yield break;
        }

        // Get resources using our new resource system
        Logger?.LogDebug("Creating resources for BackgroundLayer");
        var geometryResource = resourceManager.GetOrCreateResource(Geometry.FullScreenQuad);
        var shaderResource = resourceManager.GetOrCreateResource(Shaders.BackgroundSolid);

        Logger?.LogDebug("Created geometry resource: {GeometryResource}, shader resource: {ShaderResource}", geometryResource, shaderResource);

        var GLState = new GLState
        {
            ShaderProgram = shaderResource,
            VertexArray = geometryResource,
            Priority = RenderPriority,
            SourceViewport = viewport
            // No uniforms needed for basic orange color shader
        };

        yield return GLState;
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

    // IBackgroundController implementation - all methods use deferred updates
    public void SetVisible(bool visible)
    {
        QueueUpdate(() => IsVisible = visible);
    }

    public void SetBackgroundColor(Vector4D<float> color)
    {
        QueueUpdate(() => BackgroundColor = color);
    }

    public void SetBackgroundColor(float r, float g, float b, float a = 1.0f)
    {
        QueueUpdate(() => BackgroundColor = new Vector4D<float>(r, g, b, a));
    }

    public void SetBackgroundColor(string colorName)
    {
        QueueUpdate(() =>
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
        });
    }

    public void FadeToColor(Vector4D<float> targetColor, float factor)
    {
        QueueUpdate(() =>
        {
            // Linear interpolation between current and target color
            var current = BackgroundColor;
            BackgroundColor = new Vector4D<float>(
                current.X + (targetColor.X - current.X) * factor,
                current.Y + (targetColor.Y - current.Y) * factor,
                current.Z + (targetColor.Z - current.Z) * factor,
                current.W + (targetColor.W - current.W) * factor
            );
        });
    }
}