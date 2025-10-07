using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Extensions;

/// <summary>
/// Extension methods for GL that provide shader-related functionality.
/// </summary>
public static class GLShaderExtensions
{
    /// <summary>
    /// Helper method to set the active shader program
    /// </summary>
    public static void SetShader(this IRenderer renderer, uint programId)
    {
        var gl = renderer.GL;

        gl.UseProgram(programId);
    }

    /// <summary>
    /// Helper method to create shader program from source
    /// </summary>
    public static uint CreateShaderProgram(this IRenderer renderer, string vertexSource, string fragmentSource)
    {
        var gl = renderer.GL;

        var vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexSource);
        gl.CompileShader(vertexShader);

        var fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentSource);
        gl.CompileShader(fragmentShader);

        var program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);

        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        return program;
    }
}