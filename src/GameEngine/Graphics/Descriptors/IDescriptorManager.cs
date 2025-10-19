using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Descriptors;

/// <summary>
/// Manages Vulkan descriptor pools, layouts, and sets for binding resources to shaders.
/// Handles descriptor set allocation, updates, and lifecycle management.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// Descriptor sets are Vulkan's mechanism for binding resources (UBOs, textures, samplers, etc.) to shaders.
/// This service manages:
/// <list type="bullet">
/// <item>Descriptor pool allocation and management</item>
/// <item>Descriptor set layout creation and caching</item>
/// <item>Descriptor set allocation from pools</item>
/// <item>Descriptor set updates (binding buffers/images to sets)</item>
/// </list>
/// 
/// <para><strong>Usage Pattern:</strong></para>
/// <code>
/// // 1. Create descriptor set layout (cached by descriptor manager)
/// var layoutBindings = new DescriptorSetLayoutBinding[]
/// {
///     new() {
///         Binding = 0,
///         DescriptorType = DescriptorType.UniformBuffer,
///         DescriptorCount = 1,
///         StageFlags = ShaderStageFlags.FragmentBit
///     }
/// };
/// var layout = descriptorManager.CreateDescriptorSetLayout(layoutBindings);
/// 
/// // 2. Allocate descriptor set from pool
/// var descriptorSet = descriptorManager.AllocateDescriptorSet(layout);
/// 
/// // 3. Update descriptor set to bind uniform buffer
/// descriptorManager.UpdateDescriptorSet(descriptorSet, uniformBuffer, bufferSize);
/// 
/// // 4. Use descriptor set in DrawCommand
/// var drawCommand = new DrawCommand
/// {
///     ...,
///     DescriptorSet = descriptorSet
/// };
/// 
/// // 5. Cleanup handled automatically on disposal
/// </code>
/// </remarks>
public interface IDescriptorManager : IDisposable
{
    /// <summary>
    /// Creates a descriptor set layout from bindings.
    /// Layouts are cached - multiple calls with same bindings return the same layout.
    /// </summary>
    /// <param name="bindings">Array of descriptor bindings defining the layout</param>
    /// <returns>Descriptor set layout handle</returns>
    /// <remarks>
    /// Descriptor set layouts define the "blueprint" for descriptor sets - what resources
    /// can be bound and at which binding points. They are used when creating pipeline layouts.
    /// </remarks>
    DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutBinding[] bindings);
    
    /// <summary>
    /// Allocates a descriptor set from the pool using the specified layout.
    /// </summary>
    /// <param name="layout">Descriptor set layout to use for allocation</param>
    /// <returns>Allocated descriptor set handle</returns>
    /// <remarks>
    /// Allocates from an internal descriptor pool. If pool is exhausted, a new pool is created.
    /// Descriptor sets cannot be individually freed - entire pools are reset together.
    /// </remarks>
    DescriptorSet AllocateDescriptorSet(DescriptorSetLayout layout);
    
    /// <summary>
    /// Updates a descriptor set to bind a uniform buffer.
    /// </summary>
    /// <param name="descriptorSet">Descriptor set to update</param>
    /// <param name="buffer">Uniform buffer to bind</param>
    /// <param name="size">Size of the uniform buffer data in bytes</param>
    /// <param name="binding">Binding point (default 0)</param>
    /// <remarks>
    /// This is a convenience method for the common case of binding a single UBO.
    /// For more complex updates, a lower-level method accepting WriteDescriptorSet[] may be needed.
    /// </remarks>
    void UpdateDescriptorSet(DescriptorSet descriptorSet, Silk.NET.Vulkan.Buffer buffer, ulong size, uint binding = 0);
    
    /// <summary>
    /// Updates a descriptor set to bind a combined image sampler (texture).
    /// </summary>
    /// <param name="descriptorSet">Descriptor set to update</param>
    /// <param name="imageView">Image view to bind</param>
    /// <param name="sampler">Sampler to bind</param>
    /// <param name="imageLayout">Image layout (typically ShaderReadOnlyOptimal)</param>
    /// <param name="binding">Binding point (default 0)</param>
    void UpdateDescriptorSet(
        DescriptorSet descriptorSet,
        ImageView imageView,
        Sampler sampler,
        ImageLayout imageLayout,
        uint binding = 0);
    
    /// <summary>
    /// Resets all descriptor pools, freeing all allocated descriptor sets.
    /// Call this during resource cleanup or when recreating swapchain.
    /// </summary>
    /// <remarks>
    /// WARNING: After calling this, all previously allocated descriptor sets are invalid.
    /// Components must re-allocate descriptor sets after a pool reset.
    /// </remarks>
    void ResetPools();
}
