To Render a triangle on the screen using Vulkan:

Create a VkInstance
Select a supported graphics card (VkPhysicalDevice)
Create a VkDevice and VkQueue for drawing and presentation
Create a window, window surface and swap chain
Wrap the swap chain images into VkImageView
Create a render pass that specifies the render targets and usage
Create framebuffers for the render pass
Set up the graphics pipeline
Allocate and record a command buffer with the draw commands for every possible swap chain image
Draw frames by acquiring images, submitting the right draw command buffer and returning the images back to the swap chain

# System Architecture

1. Create `IVkRenderPass` interface and implementation
2. Create `ISwapChain` interface and implementation
3. Create `IVkCommandPool` interface and implementation
4. Create `IVkSyncManager` interface and implementation
5. Create `IVkPipelineManager` interface and implementation
6. Create `IVkRenderer` orchestrator
