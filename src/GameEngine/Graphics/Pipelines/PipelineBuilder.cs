using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Fluent builder for creating graphics pipelines.
/// Provides a readable API for configuring pipeline state.
/// Use extension methods to configure the builder.
/// </summary>
public class PipelineBuilder(
    IPipelineManager manager, 
    ISwapChain swapChain, 
    IResourceManager resources,
    IDescriptorManager descriptorManager) : IPipelineBuilder
{    
    // Internal state accessible by extension methods
    internal ShaderResource? Shader { get; set; }
    internal IShaderDefinition? ShaderDefinition { get; set; }
    internal IResourceManager Resources { get; } = resources;
    internal ISwapChain SwapChain { get; } = swapChain;
    internal IDescriptorManager DescriptorManager { get; } = descriptorManager;
    internal RenderPass? RenderPass { get; set; }
    internal PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;
    internal CullModeFlags CullMode { get; set; } = CullModeFlags.BackBit;
    internal FrontFace FrontFace { get; set; } = FrontFace.CounterClockwise;
    internal bool EnableDepthTest { get; set; } = true;
    internal bool EnableDepthWrite { get; set; } = true;
    internal bool EnableBlending { get; set; } = false;
    internal uint Subpass { get; set; } = 0;
    
    /// <inheritdoc/>
    public PipelineHandle Build(string? name)
    {
        // Get shader resource from definition if needed
        if (Shader == null && ShaderDefinition != null)
        {
            Shader = Resources.Shaders.GetOrCreate(ShaderDefinition);
        }
        
        // Validate required fields
        if (Shader == null)
            throw new InvalidOperationException("Shader is required. Call WithShader() before Build().");
        if (RenderPass == null)
            throw new InvalidOperationException("RenderPass is required. Call WithRenderPass() before Build().");
        
        // Create descriptor set layouts if shader uses descriptor sets
        DescriptorSetLayout[]? descriptorSetLayouts = null;
        if (Shader.Definition.DescriptorSetLayoutBindings != null && 
            Shader.Definition.DescriptorSetLayoutBindings.Length > 0)
        {
            var layout = DescriptorManager.CreateDescriptorSetLayout(Shader.Definition.DescriptorSetLayoutBindings);
            descriptorSetLayouts = [layout];
        }
        
        // Create descriptor from builder configuration
        var descriptor = new PipelineDescriptor
        {
            Name = name ?? GenerateAutomaticName(),
            VertexShaderPath = Shader.Definition.VertexShaderPath,
            FragmentShaderPath = Shader.Definition.FragmentShaderPath,
            VertexInputDescription = Shader.Definition.InputDescription,
            PushConstantRanges = Shader.Definition.PushConstantRanges,
            DescriptorSetLayouts = descriptorSetLayouts,
            RenderPass = RenderPass.Value,
            Topology = Topology,
            CullMode = CullMode,
            FrontFace = FrontFace,
            EnableDepthTest = EnableDepthTest,
            EnableDepthWrite = EnableDepthWrite,
            EnableBlending = EnableBlending,
            Subpass = Subpass,
        };
        
        // Let the manager create and cache the pipeline
        return manager.GetOrCreatePipeline(descriptor);
    }
    
    private string GenerateAutomaticName()
    {
        // Generate a deterministic name from configuration
        var hash = ComputeConfigurationHash();
        return $"Pipeline_{Shader!.Name}_{hash:X8}";
    }
    
    private int ComputeConfigurationHash()
    {
        var hashCode = new HashCode();
        hashCode.Add(Shader?.Name);
        hashCode.Add(RenderPass);
        hashCode.Add(Topology);
        hashCode.Add(CullMode);
        hashCode.Add(FrontFace);
        hashCode.Add(EnableDepthTest);
        hashCode.Add(EnableDepthWrite);
        hashCode.Add(EnableBlending);
        hashCode.Add(Subpass);
        return hashCode.ToHashCode();
    }
}
