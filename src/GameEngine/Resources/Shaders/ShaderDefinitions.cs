using System.Reflection;
using System.Runtime.CompilerServices;
using static Nexus.GameEngine.Graphics.Pipelines.VertexInputDescriptions;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Static definitions for all built-in shaders in the game engine.
/// Each definition contains the shader source, input description, and descriptor/push constant metadata.
/// </summary>
public static class ShaderDefinitions
{
    private static readonly Assembly GameEngineAssembly = typeof(ShaderDefinitions).Assembly;
    
    /// <summary>
    /// Shader for biaxial (4-corner) gradient backgrounds.
    /// Vertex format: Position (vec2) only.
    /// Uses UBO at binding 0 for 4 corner colors with bilinear interpolation in fragment shader.
    /// </summary>
    public static readonly ShaderDefinition BiaxialGradient = new()
    {
        Name = "BiaxialGradientShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/biaxial_gradient.vert.spv",
            "EmbeddedResources/Shaders/biaxial_gradient.frag.spv",
            GameEngineAssembly),
        InputDescription = VertexInputDescriptions.Position2D,
        PushConstantRanges = null,
        DescriptorSetLayoutBindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            }
        ]
    };
    
    /// <summary>
    /// Shader for colored geometry with per-vertex colors.
    /// Vertex format: Position (vec2) + Color (vec4) = 24 bytes per vertex.
    /// Colors provided as vertex attributes, no descriptor sets or push constants needed.
    /// </summary>
    public static readonly ShaderDefinition ColoredGeometry = new()
    {
        Name = "ColoredGeometryShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/shader.vert.spv",
            "EmbeddedResources/Shaders/shader.frag.spv",
            GameEngineAssembly),
        InputDescription = Position2DColor,
        PushConstantRanges = null,
        DescriptorSetLayoutBindings = null
    };
    
    /// <summary>
    /// Shader for rendering textured backgrounds with image sampling.
    /// Vertex format: Position (vec2) + UV (vec2) = 16 bytes per vertex.
    /// Supports UV bounds via push constants for Fill placement modes.
    /// Uses combined image sampler at binding 0.
    /// </summary>
    public static readonly ShaderDefinition ImageTexture = new()
    {
        Name = "ImageTextureShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/image_texture.vert.spv",
            "EmbeddedResources/Shaders/image_texture.frag.spv",
            GameEngineAssembly),
        InputDescription = Position2DTexCoord,
        PushConstantRanges =
        [
            new PushConstantRange
            {
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                Offset = 0,
                Size = (uint)Unsafe.SizeOf<ImageTexturePushConstants>() // 32 bytes
            }
        ],
        DescriptorSetLayoutBindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            }
        ]
    };
    
    /// <summary>
    /// Shader for linear gradient backgrounds.
    /// Vertex format: Position (vec2) only.
    /// Uses UBO at binding 0 for gradient definition and push constants for angle animation.
    /// </summary>
    public static readonly ShaderDefinition LinearGradient = new()
    {
        Name = "LinearGradientShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/linear_gradient.vert.spv",
            "EmbeddedResources/Shaders/linear_gradient.frag.spv",
            GameEngineAssembly),
        InputDescription = VertexInputDescriptions.Position2D,
        PushConstantRanges =
        [
            new PushConstantRange
            {
                StageFlags = ShaderStageFlags.FragmentBit,
                Offset = 0,
                Size = (uint)Unsafe.SizeOf<LinearGradientPushConstants>() // 16 bytes
            }
        ],
        DescriptorSetLayoutBindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            }
        ]
    };
    
    /// <summary>
    /// Shader for radial gradient backgrounds.
    /// Vertex format: Position (vec2) only.
    /// Uses UBO at binding 0 for gradient definition and push constants for center/radius animation.
    /// </summary>
    public static readonly ShaderDefinition RadialGradient = new()
    {
        Name = "RadialGradientShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/radial_gradient.vert.spv",
            "EmbeddedResources/Shaders/radial_gradient.frag.spv",
            GameEngineAssembly),
        InputDescription = VertexInputDescriptions.Position2D,
        PushConstantRanges =
        [
            new PushConstantRange
            {
                StageFlags = ShaderStageFlags.FragmentBit,
                Offset = 0,
                Size = (uint)Unsafe.SizeOf<RadialGradientPushConstants>() // 16 bytes
            }
        ],
        DescriptorSetLayoutBindings =
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            }
        ]
    };
    
    /// <summary>
    /// Shader for uniform color rendering using push constants.
    /// Vertex format: Position (vec2) = 8 bytes per vertex.
    /// Single color for all vertices provided via push constants.
    /// </summary>
    public static readonly ShaderDefinition UniformColorQuad = new()
    {
        Name = "UniformColorQuadShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/uniform_color.vert.spv",
            "EmbeddedResources/Shaders/uniform_color.frag.spv",
            GameEngineAssembly),
        InputDescription = VertexInputDescriptions.Position2D,
        PushConstantRanges =
        [
            new PushConstantRange
            {
                // Vertex shader uses a mat4 (64 bytes) + vec4 (16 bytes) = 80 bytes
                StageFlags = ShaderStageFlags.VertexBit,
                Offset = 0,
                Size = (uint)Unsafe.SizeOf<TransformedColorPushConstants>() // 80 bytes
            }
        ],
        DescriptorSetLayoutBindings = null
    };
}
