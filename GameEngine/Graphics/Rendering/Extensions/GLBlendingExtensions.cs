using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering.Extensions;

/// <summary>
/// Extension methods for GL that provide blending-related functionality.
/// </summary>
public static class GLBlendingExtensions
{
    /// <summary>
    /// Helper method to configure blending
    /// </summary>
    public static void SetBlending(this IRenderer renderer, BlendingFactor src, BlendingFactor dst)
    {
        var gl = renderer.GL;

        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(src, dst);
    }

    /// <summary>
    /// Helper method to configure blending using BlendingMode enum
    /// </summary>
    public static void SetBlending(this IRenderer renderer, BlendingMode mode)
    {
        var gl = renderer.GL;

        switch (mode)
        {
            case BlendingMode.None:
                gl.Disable(EnableCap.Blend);
                break;
            case BlendingMode.Alpha:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Additive:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                break;
            case BlendingMode.Multiply:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
            case BlendingMode.PremultipliedAlpha:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Subtract:
                gl.Enable(EnableCap.Blend);
                gl.BlendEquation(BlendEquationModeEXT.FuncReverseSubtract);
                gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                break;
            default:
                gl.Disable(EnableCap.Blend);
                break;
        }
    }
}