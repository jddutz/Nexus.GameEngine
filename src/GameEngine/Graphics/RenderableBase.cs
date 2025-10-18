using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Base class for renderable components. Provides render pass management and configuration.
/// Handles render mask computation from template configuration.
/// </summary>
public partial class RenderableBase : RuntimeComponent, IRenderable
{
    /// <summary>
    /// Template for configuring renderable components.
    /// Provides render pass configuration via named passes or explicit bit mask.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Explicit render mask (bit field). If set, overrides RenderPasses.
        /// Use this for direct bit mask control.
        /// Example: 0b0011 = passes 0 and 1
        /// </summary>
        public uint? RenderMask { get; init; }
        
        /// <summary>
        /// Array of render pass names this component participates in.
        /// Examples: ["Main"], ["Shadow", "Main"], ["UI"]
        /// If RenderMask is set, this is ignored.
        /// </summary>
        public string[]? RenderPasses { get; init; }
    }
    
    private uint? _cachedRenderMask;
    
    /// <summary>
    /// Gets the render mask for this component. Computed from template on first access.
    /// </summary>
    protected uint RenderMask
    {
        get
        {
            if (_cachedRenderMask.HasValue)
                return _cachedRenderMask.Value;
            
            // Not yet configured, use default
            return GetDefaultRenderMask();
        }
    }
    
    /// <summary>
    /// Override this to provide a default render mask if template doesn't specify one.
    /// Default implementation: All passes (0xFFFFFFFF).
    /// Common overrides: Main pass only, UI pass only, etc.
    /// </summary>
    /// <returns>Default render mask bit field</returns>
    protected virtual uint GetDefaultRenderMask()
    {
        // Default: Render in all passes
        return 0xFFFFFFFF;
    }
    
    /// <summary>
    /// Configures the component from template, computing render mask from template data.
    /// </summary>
    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            // Priority 1: Explicit bit mask provided
            if (template.RenderMask.HasValue)
            {
                _cachedRenderMask = template.RenderMask.Value;
                Logger?.LogDebug("Using explicit RenderMask: 0b{Mask:B}", _cachedRenderMask.Value);
            }
            // Priority 2: Named passes provided
            else if (template.RenderPasses is { Length: > 0 })
            {
                _cachedRenderMask = ComputeRenderMaskFromNames(template.RenderPasses);
                Logger?.LogDebug("Computed RenderMask from passes {Passes}: 0b{Mask:B}", 
                    string.Join(", ", template.RenderPasses), _cachedRenderMask.Value);
            }
            // Priority 3: Use component's default
            else
            {
                _cachedRenderMask = GetDefaultRenderMask();
                Logger?.LogDebug("Using default RenderMask: 0b{Mask:B}", _cachedRenderMask.Value);
            }
        }
    }
    
    /// <summary>
    /// Computes render mask from array of pass names by looking up in RenderPasses static class.
    /// Supports both individual pass names (e.g., "Main", "Shadow") and combined masks (e.g., "All", "Opaque").
    /// </summary>
    private uint ComputeRenderMaskFromNames(string[] passNames)
    {
        uint mask = 0;
        
        foreach (var passName in passNames)
        {
            // Try to get the constant value using reflection on the static RenderPasses class
            var field = typeof(RenderPasses).GetField(passName, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (field?.GetValue(null) is uint passValue)
            {
                mask |= passValue;
                Logger?.LogTrace("Found render pass constant '{PassName}': 0x{Value:X}", passName, passValue);
            }
            else
            {
                Logger?.LogWarning("Render pass '{PassName}' not found in RenderPasses constants. " +
                    "Available: Shadow, Depth, Background, Main, Lighting, Transparent, PostProcess, Sky, Particles, UI, Debug, " +
                    "All, Opaque, AlphaBlended, Scene", passName);
            }
        }
        
        if (mask == 0)
        {
            Logger?.LogWarning("No valid render passes found, component may not render");
        }
        
        return mask;
    }

    /// <summary>
    /// Override this to provide draw commands for rendering.
    /// Use the RenderMask property to populate DrawCommand.RenderMask.
    /// Use context.Camera for depth calculations, frustum culling, etc.
    /// </summary>
    /// <param name="context">Rendering context containing camera, viewport, and pass information</param>
    /// <returns>Collection of draw commands for this component</returns>
    public virtual IEnumerable<DrawCommand> GetDrawCommands(RenderContext context) => [];
}
