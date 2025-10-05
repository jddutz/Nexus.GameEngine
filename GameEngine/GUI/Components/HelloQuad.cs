using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.GUI.Components;

public class HelloQuad(IRenderer renderer)
    : RuntimeComponent, IRenderable
{
    public new record Template : RuntimeComponent.Template
    {
        public bool IsVisible { get; set; }
        public Vector4D<float> BackgroundColor { get; set; }
    }

    private RenderData? renderData;

    public bool IsVisible => true;

    public uint RenderPriority => 0;

    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    public uint RenderPassFlags => 1;

    public static unsafe RenderData GetRenderData(GL gl)
    {
        //Creating a vertex array.
        uint vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        //Initializing a vertex buffer that holds the vertex data.
        uint vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (void* v = &GeometryDefinitions.BasicQuad.Vertices[0])
        {
            gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(GeometryDefinitions.BasicQuad.Vertices.Length * sizeof(uint)),
                v,
                BufferUsageARB.StaticDraw);
        }

        //Initializing a element buffer that holds the index data.
        uint ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (void* i = &GeometryDefinitions.BasicQuad.Indices[0])
        {
            //Set buffer data
            gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(GeometryDefinitions.BasicQuad.Indices.Length * sizeof(uint)),
                i,
                BufferUsageARB.StaticDraw);
        }

        //Creating a vertex shader.
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        var vertexShaderSource = ShaderDefinitions.BasicQuad.VertexShader.Load();
        gl.ShaderSource(vertexShader, vertexShaderSource);
        gl.CompileShader(vertexShader);

        //Checking the shader for compilation errors.
        string infoLog = gl.GetShaderInfoLog(vertexShader);

        //Creating a fragment shader.
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        var fragmentShaderSource = ShaderDefinitions.BasicQuad.FragmentShader.Load();
        gl.ShaderSource(fragmentShader, fragmentShaderSource);
        gl.CompileShader(fragmentShader);

        //Checking the shader for compilation errors.
        infoLog = gl.GetShaderInfoLog(fragmentShader);

        //Combining the shaders under one shader program.
        uint shader = gl.CreateProgram();
        gl.AttachShader(shader, vertexShader);
        gl.AttachShader(shader, fragmentShader);
        gl.LinkProgram(shader);

        //Checking the linking for errors.
        gl.GetProgram(shader, GLEnum.LinkStatus, out var status);

        //Delete the no longer useful individual shaders;
        gl.DetachShader(shader, vertexShader);
        gl.DetachShader(shader, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        //Tell opengl how to give the data to the shaders.
        gl.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            null);

        gl.EnableVertexAttribArray(0);

        return new()
        {
            Vao = vao,
            Vbo = vbo,
            Ebo = ebo,
            Shader = shader
        };
    }

    protected override void OnActivate()
    {
        renderData = GetRenderData(renderer.GL);
    }

    public IEnumerable<RenderData> OnRender(IViewport viewport, double deltaTime)
    {
        if (renderData == null) yield break;

        yield return renderData;
    }

    public void SetVisible(bool visible)
    {
        visible = true;
    }
}