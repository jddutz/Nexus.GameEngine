using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Extensions;

/// <summary>
/// Extension methods for GL that provide error checking functionality.
/// </summary>
public static class GLErrorExtensions
{
    /// <summary>
    /// Helper method to check for GL errors and report them
    /// </summary>
    public static void CheckGLError(this IRenderer renderer, string operation)
    {
        var gl = renderer.GL;

        var error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            throw new InvalidOperationException($"OpenGL error during {operation}: {error}");
        }
    }
}