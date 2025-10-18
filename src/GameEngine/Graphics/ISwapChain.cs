using Silk.NET.Vulkan;

using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages the Vulkan swap chain and the complete presentation pipeline.
/// Owns swapchain resources, render passes, framebuffers, and orchestrates their lifecycle.
/// </summary>
/// <remarks>
/// <para><strong>Ownership:</strong></para>
/// SwapChain owns Swapchain → Images → ImageViews → RenderPasses → Framebuffers.
/// All resources are created from VulkanSettings configuration during initialization.
/// 
/// <para><strong>Lifecycle:</strong></para>
/// - Init: Creates all resources
/// - Resize: Recreates swapchain, image views, and framebuffers (render passes survive)
/// - Dispose: Destroys all resources including render passes
/// 
/// <para><strong>Usage:</strong></para>
/// Renderer injects ISwapChain to access RenderPasses and Framebuffers for rendering.
/// Clear values are built dynamically in Renderer from Viewport.BackgroundColor.
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
    /// Gets all render passes indexed by bit position (0-10 corresponding to RenderPasses constants).
    /// These survive window resize since format doesn't change.
    /// </summary>
    /// <remarks>
    /// Array index corresponds to bit position in RenderPasses constants.
    /// Example: swapChain.Passes[0] = Shadow pass, swapChain.Passes[3] = Main pass
    /// </remarks>
    RenderPass[] Passes { get; }
    
    /// <summary>
    /// Gets framebuffers for each render pass, indexed by bit position.
    /// Each render pass has N framebuffers (one per swapchain image).
    /// Recreated on window resize.
    /// </summary>
    /// <remarks>
    /// <para><strong>Usage:</strong></para>
    /// <code>
    /// var framebuffer = swapChain.Framebuffers[bitPos][imageIndex];
    /// </code>
    /// 
    /// <para>Framebuffers bind specific ImageViews to RenderPass attachments.</para>
    /// </remarks>
    Framebuffer[][] Framebuffers { get; }

    /// <summary>
    /// Gets the depth image if any render pass uses depth attachment, otherwise default.
    /// Used for layout transitions in the renderer.
    /// </summary>
    Image DepthImage { get; }

    /// <summary>
    /// Gets whether any render pass uses a depth attachment.
    /// </summary>
    bool HasDepthAttachment { get; }

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
    
    /// <summary>
    /// Event raised before presenting an image to the screen.
    /// At this point, the image is in PresentSrcKhr layout and ready to be sampled for testing.
    /// </summary>
    /// <remarks>
    /// This event is primarily used by testing infrastructure (IPixelSampler) to sample
    /// the final rendered output before it's presented to the screen.
    /// The image is guaranteed to be in the correct layout (PresentSrcKhr) for readback.
    /// </remarks>
    event EventHandler<PresentEventArgs>? BeforePresent;
}