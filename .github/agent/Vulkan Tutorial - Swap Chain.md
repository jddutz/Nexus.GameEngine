Vulkan Tutorial - Swap Chain

SwapChain defines a set of images to be presented to the screen. Swap chain defines how exactly the queue works and the conditions for presenting an image from the queue.

1) Direct rendering:
    - Single buffer rendered directly to the screen
2) Double buffering:
    - Addresses flickering due to screen refresh when the image is only partly drawn
    - Front buffer presented to the screen
    - Draw commands operate on a back buffer
3) Triple buffering:
    - Addresses locking issues caused by GPU and CPU trying to access the same resources
    - Front buffer presented to the screen
    - Middle buffer being drawn by the GPU
    - Back buffer receives CPU commands

Looks like we can also define a swap chain that accepts more images as they are rendered and presents them to the screen one at a time, on a first-in first-out basis. This allows us to process frames as fast as possible but we need to handle application behavior when the queue is full.

We need to update physical device selection to test for VK_KHR_swapchain support.

We're already creating the SurfaceKHR in Context.CreateSurface(IWindow), are we checking support for the required extension exists before creating it?
How do we define and get the supported features for SurfaceKHR? Doesn't appear to be part of Context yet.

Vulkan tutorial defines a struct that contains:
Basic surface capabilities (min/max number of images in swap chain, min/max width and height of images)
Surface formats (pixel format, color space)
Available presentation modes

```cs
struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}
```

This information is compared against a set of requirements during device selection to ensure the selected device meets the necessary requirements.

How should we define these as requirements? Does the engine need to be configurable with regard to the minimum number of buffers supported or should we simply choose the preferred architecture and go from there? While SurfaceFormat is relatively straightforward, PresentMode is more complicated. For instance, should we settle on triple buffering for all applications? Would energy requirements limit the ability to deploy a game to mobile (Android or iOS)?

How do we handle situations where a device with the required features is unavailable? Is this really a concern? Is it more likely that we select a device from those available and the necessary adaptations are applied in the background? How specific do we need to be with regard to adaptive degradation?
