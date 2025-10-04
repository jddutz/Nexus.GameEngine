namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Predefined shader resources
/// </summary>
public static partial class Shaders
{
    /// <summary>
    /// Background solid color shader for rendering full-screen backgrounds
    /// </summary>
    public static readonly ShaderDefinition BackgroundSolid = new()
    {
        Name = "BackgroundSolid",

        VertexSource = """
            #version 330 core
            layout (location = 0) in vec3 vPos;
            
            void main()
            {
                gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
            }
            """,

        FragmentSource = """
            #version 330 core
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
            }
            """,

        Uniforms = new UniformDefinition[0], // No uniforms in basic version

        AttributeBindings = new[]
        {
            new AttributeBinding { Name = "vPos", Location = 0 }
        }
    };
}