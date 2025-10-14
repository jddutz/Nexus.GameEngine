using Silk.NET.Vulkan;

using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages the Vulkan swap chain and the complete presentation pipeline.
/// Owns swapchain resources, render passes, framebuffers, and orchestrates their lifecycle.
/// </summary>
/// <remarks>
/// <para><strong>Ownership:</strong></para>
/// SwapChain owns Swapchain → Images → ImageViews → RenderPasses → Framebuffers → ClearValues.
/// All resources are created from VulkanSettings configuration during initialization.
/// 
/// <para><strong>Lifecycle:</strong></para>
/// - Init: Creates all resources
/// - Resize: Recreates swapchain, image views, and framebuffers (render passes survive)
/// - Dispose: Destroys all resources including render passes
/// 
/// <para><strong>Usage:</strong></para>
/// Renderer injects ISwapChain to access RenderPasses, Framebuffers, and ClearValues for rendering.
/// </remarks>
public interface ISwapChain : IDisposable
{
    /// <summary>Gets the Vulkan swapchain handle.</summary>
    SwapchainKHR Swapchain { get; }
    
    /// <summary>Gets the format of swapchain images (e.g., B8G8R8A8Srgb).</summary>
    Format SwapchainFormat { get; }
    
    /// <summary>Gets the dimensions of swapchain images (width x height in pixels).</summary>
    Extent2D SwapchainExtent { get; }
    
    /// <summary>Gets the swapchain images (owned by Vulkan, destroyed with swapchain).</summary>
    Image[] SwapchainImages { get; }
    
    /// <summary>Gets the image views for swapchain images (recreated on resize).</summary>
    ImageView[] SwapchainImageViews { get; }
    
    /// <summary>
    /// Gets all render passes created from VulkanSettings.RenderPasses[] configuration.
    /// These survive window resize since format doesn't change.
    /// </summary>
    /// <remarks>
    /// Array index corresponds to VulkanSettings.RenderPasses[] index.
    /// Typically index 0 is the "Main" render pass for primary rendering.
    /// Use these with Framebuffers and ClearValues dictionaries.
    /// </remarks>
    RenderPass[] RenderPasses { get; }
    
    /// <summary>
    /// Gets framebuffers for each render pass, indexed by RenderPass handle.
    /// Each render pass has N framebuffers (one per swapchain image).
    /// Recreated on window resize.
    /// </summary>
    /// <remarks>
    /// <para><strong>Usage:</strong></para>
    /// <code>
    /// var framebuffer = swapChain.Framebuffers[renderPass][imageIndex];
    /// </code>
    /// 
    /// <para>Framebuffers bind specific ImageViews to RenderPass attachments.</para>
    /// </remarks>
    IReadOnlyDictionary<RenderPass, Framebuffer[]> Framebuffers { get; }
    
    /// <summary>
    /// Gets clear values for each render pass, indexed by RenderPass handle.
    /// Clear values define what to clear attachments to when LoadOp is Clear.
    /// </summary>
    /// <remarks>
    /// <para><strong>Array Layout:</strong></para>
    /// - Index 0: Color attachment clear value (RGBA)
    /// - Index 1: Depth/stencil clear value (if depth enabled)
    /// 
    /// <para><strong>Usage:</strong></para>
    /// <code>
    /// var clearValues = swapChain.ClearValues[renderPass];
    /// renderPassInfo.PClearValues = clearValues;
    /// </code>
    /// </remarks>
    IReadOnlyDictionary<RenderPass, ClearValue[]> ClearValues { get; }

    /// <summary>
    /// Recreates the swapchain on window resize.
    /// Destroys and recreates: swapchain, image views, and framebuffers.
    /// Preserves: render passes (format doesn't change).
    /// </summary>
    void Recreate();
    
    /// <summary>
    /// Acquires the next available swapchain image for rendering.
    /// </summary>
    /// <param name="imageAvailableSemaphore">Semaphore to signal when image is available.</param>
    /// <param name="result">Result code (Success, ErrorOutOfDateKhr, SuboptimalKhr, etc.).</param>
    /// <returns>Index of the acquired image (use with Framebuffers dictionary).</returns>
    uint AcquireNextImage(Semaphore imageAvailableSemaphore, out Result result);
    
    /// <summary>
    /// Presents the rendered image to the screen.
    /// </summary>
    /// <param name="imageIndex">Index of the image to present (from AcquireNextImage).</param>
    /// <param name="renderFinishedSemaphore">Semaphore to wait on before presenting.</param>
    void Present(uint imageIndex, Semaphore renderFinishedSemaphore);
}