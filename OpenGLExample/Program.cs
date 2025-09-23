using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using Silk.NET.Maths;
using Silk.NET.Maths;
using StbImageSharp;

namespace Program
{
    class Program
    {
        private static IWindow _window;
        private static GL _gl;

        private static uint _vbo;
        private static uint _ebo;
        private static uint _vao;
        private static uint _shader;

        private static uint _texture;
        private const string IMAGE_PATH = @"C:\Users\jddut\Source\NexusRealms\Prelude\Shared\GameAssets\GameArt\NexusRealms.Prelude.Splash.png";

        //Vertex shaders are run on each vertex.
        private static readonly string VertexShaderSource = @"
            #version 330 core

            layout (location = 0) in vec3 aPosition;
            // Add a new input attribute for the texture coordinates
            layout (location = 1) in vec2 aTextureCoord;

            // Add an output variable to pass the texture coordinate to the fragment shader
            // This variable stores the data that we want to be received by the fragment
            out vec2 frag_texCoords;

            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
                // Assigin the texture coordinates without any modification to be recived in the fragment
                frag_texCoords = aTextureCoord;
            }
        ";

        //Fragment shaders are run on each fragment/pixel of the geometry.
        private static readonly string FragmentShaderSource = @"
            #version 330 core

            // Receive the input from the vertex shader in an attribute
            in vec2 frag_texCoords;

            out vec4 out_color;
            
            uniform sampler2D uTexture;
            
            void main()
            {
                // out_color = vec4(frag_texCoords.x, frag_texCoords.y, 0.0, 1.0);
                out_color = texture(uTexture, frag_texCoords);
            }   
        ";

        //Vertex data, uploaded to the VBO.
        private static readonly float[] Vertices =
        [
        //       aPosition     | aTexCoords
             0.5f,  0.5f, 0.0f,  1.0f, 1.0f,
             0.5f, -0.5f, 0.0f,  1.0f, 0.0f,
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,
            -0.5f,  0.5f, 0.0f,  0.0f, 1.0f
        ];

        //Index data, uploaded to the EBO.
        private static readonly uint[] Indices =
        [
            0, 1, 3,
            1, 2, 3
        ];


        private static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "LearnOpenGL with Silk.NET";
            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Update += OnUpdate;
            _window.Closing += OnClose;

            _window.Run();

            _window.Dispose();
        }


        private static unsafe void OnLoad()
        {
            IInputContext input = _window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }

            //Getting the opengl api for drawing to the screen.
            _gl = _window.CreateOpenGL();

            //Creating a vertex array.
            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            //Initializing a vertex buffer that holds the vertex data.
            _vbo = _gl.GenBuffer(); //Creating the buffer.
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo); //Binding the buffer.
            fixed (float* v = Vertices)
            {
                _gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(Vertices.Length * sizeof(uint)),
                    v,
                    BufferUsageARB.StaticDraw
                ); //Setting buffer data.
            }

            //Initializing a element buffer that holds the index data.
            _ebo = _gl.GenBuffer(); //Creating the buffer.
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo); //Binding the buffer.
            fixed (uint* i = Indices)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                    (nuint)(Indices.Length * sizeof(uint)),
                    i,
                    BufferUsageARB.StaticDraw
                ); //Setting buffer data.
            }

            //Creating a vertex shader.
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, VertexShaderSource);
            _gl.CompileShader(vertexShader);

            //Checking the shader for compilation errors.
            _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));

            //Creating a fragment shader.
            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, FragmentShaderSource);
            _gl.CompileShader(fragmentShader);

            //Checking the shader for compilation errors.
            _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));

            //Combining the shaders under one shader program.
            _shader = _gl.CreateProgram();
            _gl.AttachShader(_shader, vertexShader);
            _gl.AttachShader(_shader, fragmentShader);
            _gl.LinkProgram(_shader);

            //Checking the linking for errors.
            _gl.GetProgram(_shader, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {_gl.GetProgramInfoLog(_shader)}");
            }

            //Delete the no longer useful individual shaders;
            _gl.DetachShader(_shader, vertexShader);
            _gl.DetachShader(_shader, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            //Tell opengl how to give the data to the shaders.
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(
                0,
                3,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float),
                null);

            const uint texCoordLoc = 1;
            _gl.EnableVertexAttribArray(texCoordLoc);
            _gl.VertexAttribPointer(
                texCoordLoc,
                2,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float), // stride
                (void*)(3 * sizeof(float)) // offset
            );

            _gl.BindVertexArray(_vao);
            _gl.UseProgram(_shader);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);

            _texture = _gl.GenTexture();
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _texture);

            // Flip the image vertically on load
            StbImage.stbi_set_flip_vertically_on_load(1);

            ImageResult result = ImageResult.FromMemory(
                File.ReadAllBytes(IMAGE_PATH),
                ColorComponents.RedGreenBlueAlpha
            );
            // Define a pointer to the image data
            fixed (byte* ptr = result.Data)
                // Here we use "result.Width" and "result.Height" to tell 
                // OpenGL about how big our texture is.
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)result.Width,
                    (uint)result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);

            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            int location = _gl.GetUniformLocation(_shader, "uTexture");
            _gl.Uniform1(location, 0);

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private static unsafe void OnRender(double obj) //Method needs to be unsafe due to draw elements.
        {
            //Clear the color channel.
            _gl.ClearColor(Vector4D.CornflowerBlue);
            _gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            //Bind the geometry and shader.
            _gl.BindVertexArray(_vao);
            _gl.UseProgram(_shader);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _texture);

            //Draw the geometry.
            _gl.DrawElements(
                PrimitiveType.Triangles,
                (uint)Indices.Length,
                DrawElementsType.UnsignedInt,
                null
            );
        }

        private static void OnUpdate(double obj)
        {

        }

        private static void OnFramebufferResize(Vector2D<int> newSize)
        {
            _gl.Viewport(newSize);
        }

        private static void OnClose()
        {
            //Remember to delete the buffers.
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_shader);
        }

        private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                _window.Close();
            }
        }
    }
}