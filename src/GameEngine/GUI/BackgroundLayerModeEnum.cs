namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines the rendering mode for the BackgroundLayer component.
/// Each mode uses different shaders and data structures.
/// </summary>
public enum BackgroundLayerModeEnum
{
    /// <summary>
    /// Single solid color across the entire background.
    /// Uses existing ColoredGeometryShader with uniform colors.
    /// </summary>
    UniformColor,
    
    /// <summary>
    /// Four corner colors interpolated across the background.
    /// Uses existing ColoredGeometryShader with per-vertex colors.
    /// </summary>
    PerVertexColor,
    
    /// <summary>
    /// Linear gradient with multiple color stops.
    /// Uses LinearGradientShader with UBO and push constants for angle.
    /// </summary>
    LinearGradient,
    
    /// <summary>
    /// Radial gradient with multiple color stops.
    /// Uses RadialGradientShader with UBO and push constants for center/radius.
    /// </summary>
    RadialGradient,
    
    // Future modes (not yet implemented):
    // ImageTexture,     // Texture-based background
    // Procedural,       // Shader-based procedural patterns
}