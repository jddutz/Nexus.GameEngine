using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Defines a shader program with vertex and fragment shaders.
/// Includes VertexInputDescription describing expected vertex format.
/// </summary>
public interface IShaderDefinition : IResourceDefinition
{
    /// <summary>
    /// Path to compiled vertex shader SPIR-V file.
    /// </summary>
    string VertexShaderPath { get; }
    
    /// <summary>
    /// Path to compiled fragment shader SPIR-V file.
    /// </summary>
    string FragmentShaderPath { get; }
    
    /// <summary>
    /// Description of vertex input format this shader expects.
    /// Defines locations, formats, and offsets of vertex attributes.
    /// </summary>
    VertexInputDescription InputDescription { get; }
    
    /// <summary>
    /// Push constant ranges used by this shader.
    /// Null or empty if shader doesn't use push constants.
    /// </summary>
    PushConstantRange[]? PushConstantRanges { get; }
    
    /// <summary>
    /// Descriptor set layout bindings used by this shader.
    /// Null or empty if shader doesn't use descriptor sets (UBOs, textures, etc.).
    /// </summary>
    /// <remarks>
    /// For shaders that use uniform buffers or other resources, this defines
    /// the descriptor set layout bindings. The descriptor manager will create
    /// descriptor set layouts from these bindings.
    /// </remarks>
    DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings { get; }
    
    /// <summary>
    /// Validates that geometry is compatible with this shader's input requirements.
    /// </summary>
    /// <param name="geometry">Geometry resource to validate</param>
    /// <exception cref="InvalidOperationException">Thrown if geometry is incompatible</exception>
    void ValidateGeometry(GeometryResource geometry);
}
