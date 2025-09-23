using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering.Extensions;

/// <summary>
/// Extension methods for GL that provide error checking functionality.
/// </summary>
public static class GLErrorExtensions
{
    /// <summary>
    /// Helper method to check for GL errors and report them
    /// </summary>
    public static void CheckGLError(this GL gl, string operation)
    {
        var error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            throw new InvalidOperationException($"OpenGL error during {operation}: {error}");
        }
    }
}