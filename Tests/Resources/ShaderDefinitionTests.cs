using Xunit;
using Nexus.GameEngine.Resources.Shaders;

namespace Tests.Resources;

public class ShaderDefinitionTests
{
    [Fact]
    public void BackgroundSolid_ShouldHaveValidDefinition()
    {
        // Arrange & Act
        var shader = Shaders.BackgroundSolid;

        // Assert
        Assert.Equal("BackgroundSolid", shader.Name);
        Assert.False(string.IsNullOrWhiteSpace(shader.VertexSource));
        Assert.False(string.IsNullOrWhiteSpace(shader.FragmentSource));
        Assert.Null(shader.GeometrySource);
        Assert.NotEmpty(shader.Uniforms);
        Assert.NotEmpty(shader.AttributeBindings);
    }

    [Fact]
    public void BackgroundSolid_ShouldHaveCorrectUniforms()
    {
        // Arrange & Act
        var shader = Shaders.BackgroundSolid;
        var uniforms = shader.Uniforms.ToList();

        // Assert
        Assert.Equal(2, uniforms.Count);

        // Background color uniform
        var colorUniform = uniforms.FirstOrDefault(u => u.Name == "uBackgroundColor");
        Assert.NotNull(colorUniform);
        Assert.Equal(UniformType.Vec4, colorUniform.Type);
        Assert.True(colorUniform.Required);

        // Fade uniform
        var fadeUniform = uniforms.FirstOrDefault(u => u.Name == "uFade");
        Assert.NotNull(fadeUniform);
        Assert.Equal(UniformType.Float, fadeUniform.Type);
        Assert.False(fadeUniform.Required);
        Assert.Equal(1.0f, fadeUniform.DefaultValue);
    }

    [Fact]
    public void BackgroundSolid_ShouldHaveCorrectAttributeBindings()
    {
        // Arrange & Act
        var shader = Shaders.BackgroundSolid;
        var attributes = shader.AttributeBindings.ToList();

        // Assert
        Assert.Equal(2, attributes.Count);

        // Position attribute
        var posAttr = attributes.FirstOrDefault(a => a.Name == "aPosition");
        Assert.NotNull(posAttr);
        Assert.Equal(0u, posAttr.Location);

        // Texture coordinate attribute
        var texAttr = attributes.FirstOrDefault(a => a.Name == "aTexCoord");
        Assert.NotNull(texAttr);
        Assert.Equal(1u, texAttr.Location);
    }

    [Fact]
    public void BackgroundSolid_VertexShader_ShouldContainRequiredElements()
    {
        // Arrange & Act
        var shader = Shaders.BackgroundSolid;
        var vertexSource = shader.VertexSource;

        // Assert
        Assert.Contains("#version 330 core", vertexSource);
        Assert.Contains("layout (location = 0) in vec2 aPosition", vertexSource);
        Assert.Contains("layout (location = 1) in vec2 aTexCoord", vertexSource);
        Assert.Contains("out vec2 TexCoord", vertexSource);
        Assert.Contains("gl_Position = vec4(aPosition, 0.0, 1.0)", vertexSource);
        Assert.Contains("TexCoord = aTexCoord", vertexSource);
    }

    [Fact]
    public void BackgroundSolid_FragmentShader_ShouldContainRequiredElements()
    {
        // Arrange & Act
        var shader = Shaders.BackgroundSolid;
        var fragmentSource = shader.FragmentSource;

        // Assert
        Assert.Contains("#version 330 core", fragmentSource);
        Assert.Contains("in vec2 TexCoord", fragmentSource);
        Assert.Contains("out vec4 FragColor", fragmentSource);
        Assert.Contains("uniform vec4 uBackgroundColor", fragmentSource);
        Assert.Contains("uniform float uFade", fragmentSource);
        Assert.Contains("FragColor = vec4(uBackgroundColor.rgb, uBackgroundColor.a * uFade)", fragmentSource);
    }

    [Fact]
    public void UniformDefinition_ShouldCreateCorrectly()
    {
        // Arrange & Act
        var uniform = new UniformDefinition
        {
            Name = "testUniform",
            Type = UniformType.Mat4,
            Required = false,
            DefaultValue = "identity"
        };

        // Assert
        Assert.Equal("testUniform", uniform.Name);
        Assert.Equal(UniformType.Mat4, uniform.Type);
        Assert.False(uniform.Required);
        Assert.Equal("identity", uniform.DefaultValue);
    }

    [Fact]
    public void AttributeBinding_ShouldCreateCorrectly()
    {
        // Arrange & Act
        var binding = new AttributeBinding
        {
            Name = "aVertex",
            Location = 3
        };

        // Assert
        Assert.Equal("aVertex", binding.Name);
        Assert.Equal(3u, binding.Location);
    }
}