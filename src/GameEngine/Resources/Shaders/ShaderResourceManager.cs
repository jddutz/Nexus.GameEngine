namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Implements shader resource management with caching and reference counting.
/// </summary>
public class ShaderResourceManager(IGraphicsContext context)
    : VulkanResourceManager<ShaderDefinition, ShaderResource>, IShaderResourceManager
{
    /// <summary>
    /// Gets a string key for logging purposes using the shader definition's name.
    /// </summary>
    protected override string GetResourceKey(ShaderDefinition definition)
    {
        return definition.Name;
    }

    /// <summary>
    /// Creates a new shader resource from a definition by loading SPIR-V and creating shader modules.
    /// </summary>
    public override ShaderResource CreateResource(ShaderDefinition definition)
    {
        // Load SPIR-V from source
        var sourceData = definition.Source.Load();

        // Validate SPIR-V data
        if (sourceData.VertexSpirV == null || sourceData.VertexSpirV.Length == 0)
        {
            throw new InvalidOperationException(
                $"Vertex shader SPIR-V is null or empty for shader '{definition.Name}'");
        }

        if (sourceData.FragmentSpirV == null || sourceData.FragmentSpirV.Length == 0)
        {
            throw new InvalidOperationException(
                $"Fragment shader SPIR-V is null or empty for shader '{definition.Name}'");
        }

        // Create shader modules
        var vertShaderModule = CreateShaderModule(sourceData.VertexSpirV);
        var fragShaderModule = CreateShaderModule(sourceData.FragmentSpirV);

        if (vertShaderModule.Handle == 0 || fragShaderModule.Handle == 0)
        {
            throw new InvalidOperationException(
                $"Failed to create shader modules for '{definition.Name}'");
        }

        return new ShaderResource(vertShaderModule, fragShaderModule, definition);
    }

    /// <summary>
    /// Creates a Vulkan shader module from SPIR-V bytecode.
    /// </summary>
    private unsafe ShaderModule CreateShaderModule(byte[] spirvCode)
    {
        if (spirvCode == null || spirvCode.Length == 0) return default;

        fixed (byte* codePtr = spirvCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirvCode.Length,
                PCode = (uint*)codePtr
            };

            ShaderModule shaderModule;
            var result = context.VulkanApi.CreateShaderModule(context.Device, &createInfo, null, &shaderModule);

            if (result != Result.Success) return default;

            return shaderModule;
        }
    }

    /// <summary>
    /// Destroys a shader resource by destroying both vertex and fragment shader modules.
    /// </summary>
    protected override unsafe void DestroyResource(ShaderResource resource)
    {
        if (resource.VertexShader.Handle != 0)
        {
            context.VulkanApi.DestroyShaderModule(context.Device, resource.VertexShader, null);
        }

        if (resource.FragmentShader.Handle != 0)
        {
            context.VulkanApi.DestroyShaderModule(context.Device, resource.FragmentShader, null);
        }
    }
}
