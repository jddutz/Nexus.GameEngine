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
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            [0] = 
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
        }
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
        DescriptorSetLayouts = null
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
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            [0] = 
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
        }
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
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            [0] = 
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
        }
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
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            [0] = 
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
        }
    };
    
    /// <summary>
    /// Shader for uniform color rendering using ViewProjection UBO and push constants.
    /// Vertex format: Position (vec2) = 8 bytes per vertex.
    /// ViewProjection matrix from UBO at set=0, binding=0.
    /// Single color for all vertices provided via push constants (vec4 = 16 bytes).
    /// 
    /// OBSOLETE: Replaced by UIElement shader which supports both solid colors and textures.
    /// Use UIElement shader with 1×1 white dummy texture for solid colors.
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
                // Push constants contain model matrix + color (mat4 + vec4 = 80 bytes)
                StageFlags = ShaderStageFlags.VertexBit,
                Offset = 0,
                Size = 80 // mat4 model (64 bytes) + vec4 color (16 bytes)
            }
        ],
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            [0] = 
            [
                new DescriptorSetLayoutBinding
                {
                    // ViewProjection UBO at set=0, binding=0
                    Binding = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    StageFlags = ShaderStageFlags.VertexBit
                }
            ]
        }
    };

    /// <summary>
    /// Unified shader for all UI elements (colored and textured).
    /// Vertex format: Position (vec2) + UV (vec2) = 16 bytes per vertex.
    /// ViewProjection matrix from UBO at set=0, binding=0.
    /// Model matrix and tint color via push constants (mat4 + vec4 = 80 bytes).
    /// Texture sampler at set=1, binding=0.
    /// For solid colors: use 1×1 white dummy texture with tint color.
    /// For textures: use actual texture with white tint color.
    /// </summary>
    public static readonly ShaderDefinition UIElement = new()
    {
        Name = "UITexturedShader",
        Source = new EmbeddedSpvShaderSource(
            "EmbeddedResources/Shaders/ui_element.vert.spv",
            "EmbeddedResources/Shaders/ui_element.frag.spv",
            GameEngineAssembly),
        InputDescription = Position2DTexCoord,
        PushConstantRanges =
        [
            new PushConstantRange
            {
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                Offset = 0,
                Size = 96 // mat4 model (64 bytes) + vec4 tintColor (16 bytes) + vec4 uvRect (16 bytes)
            }
        ],
        DescriptorSetLayouts = new Dictionary<uint, DescriptorSetLayoutBinding[]>
        {
            // Set 0: ViewProjection UBO (bound by camera system)
            [0] = 
            [
                new DescriptorSetLayoutBinding
                {
                    Binding = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    StageFlags = ShaderStageFlags.VertexBit,
                    PImmutableSamplers = null
                }
            ],
            // Set 1: Texture sampler (bound by Element per-draw)
            [1] =
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
        }
    };
}
