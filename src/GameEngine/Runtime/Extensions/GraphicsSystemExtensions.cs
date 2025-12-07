using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nexus.GameEngine.Runtime.Extensions;

/// <summary>
/// Extension methods for the graphics system to provide easy access to common graphics operations.
/// </summary>
public static class GraphicsSystemExtensions
{
    /// <summary>
    /// Gets or creates a graphics pipeline from a descriptor.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="descriptor">The pipeline descriptor.</param>
    /// <returns>A handle to the pipeline.</returns>
    public static PipelineHandle GetPipeline(this IGraphicsSystem system, PipelineDescriptor descriptor)
    {
        return ((GraphicsSystem)system).PipelineManager.GetOrCreatePipeline(descriptor);
    }

    /// <summary>
    /// Gets or creates a graphics pipeline from a definition.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="definition">The pipeline definition.</param>
    /// <returns>A handle to the pipeline.</returns>
    public static PipelineHandle GetPipeline(this IGraphicsSystem system, PipelineDefinition definition)
    {
        return ((GraphicsSystem)system).PipelineManager.GetOrCreate(definition);
    }

    /// <summary>
    /// Creates a descriptor set layout from bindings.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="bindings">The layout bindings.</param>
    /// <returns>The created descriptor set layout.</returns>
    public static DescriptorSetLayout CreateDescriptorSetLayout(this IGraphicsSystem system, DescriptorSetLayoutBinding[] bindings)
    {
        return ((GraphicsSystem)system).DescriptorManager.CreateDescriptorSetLayout(bindings);
    }

    /// <summary>
    /// Allocates a descriptor set from a layout.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="layout">The descriptor set layout.</param>
    /// <returns>The allocated descriptor set.</returns>
    public static DescriptorSet AllocateDescriptorSet(this IGraphicsSystem system, DescriptorSetLayout layout)
    {
        return ((GraphicsSystem)system).DescriptorManager.AllocateDescriptorSet(layout);
    }

    /// <summary>
    /// Updates a descriptor set with a buffer binding.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="descriptorSet">The descriptor set to update.</param>
    /// <param name="buffer">The buffer to bind.</param>
    /// <param name="size">The size of the buffer range.</param>
    /// <param name="binding">The binding index.</param>
    public static void UpdateDescriptorSet(this IGraphicsSystem system, DescriptorSet descriptorSet, Silk.NET.Vulkan.Buffer buffer, ulong size, uint binding = 0)
    {
        ((GraphicsSystem)system).DescriptorManager.UpdateDescriptorSet(descriptorSet, buffer, size, binding);
    }

    /// <summary>
    /// Updates a descriptor set with an image binding.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="descriptorSet">The descriptor set to update.</param>
    /// <param name="imageView">The image view to bind.</param>
    /// <param name="sampler">The sampler to use.</param>
    /// <param name="imageLayout">The layout of the image.</param>
    /// <param name="binding">The binding index.</param>
    public static void UpdateDescriptorSet(this IGraphicsSystem system, DescriptorSet descriptorSet, ImageView imageView, Sampler sampler, ImageLayout imageLayout, uint binding = 0)
    {
        ((GraphicsSystem)system).DescriptorManager.UpdateDescriptorSet(descriptorSet, imageView, sampler, imageLayout, binding);
    }

    /// <summary>
    /// Binds a pipeline to a command buffer.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="commandBuffer">The command buffer.</param>
    /// <param name="pipeline">The pipeline handle.</param>
    public static void BindPipeline(this IGraphicsSystem system, CommandBuffer commandBuffer, PipelineHandle pipeline)
    {
        var vk = ((GraphicsSystem)system).Context.VulkanApi;
        vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline.Pipeline);
    }

    /// <summary>
    /// Acquires the next image from the swapchain.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="imageAvailableSemaphore">Semaphore to signal when the image is available.</param>
    /// <param name="result">The result of the acquisition.</param>
    /// <returns>The index of the acquired image.</returns>
    public static uint BeginFrame(this IGraphicsSystem system, Semaphore imageAvailableSemaphore, out Result result)
    {
        return ((GraphicsSystem)system).SwapChain.AcquireNextImage(imageAvailableSemaphore, out result);
    }

    /// <summary>
    /// Presents the rendered image to the swapchain.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="imageIndex">The index of the image to present.</param>
    /// <param name="renderFinishedSemaphore">Semaphore to wait on before presenting.</param>
    public static void EndFrame(this IGraphicsSystem system, uint imageIndex, Semaphore renderFinishedSemaphore)
    {
        ((GraphicsSystem)system).SwapChain.Present(imageIndex, renderFinishedSemaphore);
    }

    /// <summary>
    /// Sets the viewport for the command buffer.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="commandBuffer">The command buffer.</param>
    /// <param name="viewport">The viewport to set.</param>
    public static void SetViewport(this IGraphicsSystem system, CommandBuffer commandBuffer, Silk.NET.Vulkan.Viewport viewport)
    {
        var vk = ((GraphicsSystem)system).Context.VulkanApi;
        vk.CmdSetViewport(commandBuffer, 0, 1, in viewport);
    }

    /// <summary>
    /// Sets the scissor rectangle for the command buffer.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="commandBuffer">The command buffer.</param>
    /// <param name="scissor">The scissor rectangle to set.</param>
    public static void SetScissor(this IGraphicsSystem system, CommandBuffer commandBuffer, Rect2D scissor)
    {
        var vk = ((GraphicsSystem)system).Context.VulkanApi;
        vk.CmdSetScissor(commandBuffer, 0, 1, in scissor);
    }

    /// <summary>
    /// Draws a full-screen quad. Useful for post-processing or background rendering.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="commandBuffer">The command buffer.</param>
    public static void DrawQuad(this IGraphicsSystem system, CommandBuffer commandBuffer)
    {
        var vk = ((GraphicsSystem)system).Context.VulkanApi;
        vk.CmdDraw(commandBuffer, 6, 1, 0, 0);
    }

    /// <summary>
    /// Draws a triangle.
    /// </summary>
    /// <param name="system">The graphics system.</param>
    /// <param name="commandBuffer">The command buffer.</param>
    public static void DrawTriangle(this IGraphicsSystem system, CommandBuffer commandBuffer)
    {
        var vk = ((GraphicsSystem)system).Context.VulkanApi;
        vk.CmdDraw(commandBuffer, 3, 1, 0, 0);
    }
}
