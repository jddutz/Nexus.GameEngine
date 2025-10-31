namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Configuration for a single render pass using Vulkan native types.
/// </summary>
public class RenderPassConfiguration
{
    /// <summary>
    /// Unique name for this render pass (e.g., "Main", "Shadow", "PostProcess").
    /// </summary>
    public string Name { get; set; } = "Main";
    
    /// <summary>
    /// Color attachment format. Set to Format.Undefined to use swapchain format.
    /// </summary>
    public Format ColorFormat { get; set; } = Format.Undefined;
    
    /// <summary>
    /// Depth attachment format. Set to Format.Undefined for no depth.
    /// </summary>
    public Format DepthFormat { get; set; } = Format.D32Sfloat;
    
    /// <summary>
    /// Color attachment load operation.
    /// </summary>
    public AttachmentLoadOp ColorLoadOp { get; set; } = AttachmentLoadOp.Clear;
    
    /// <summary>
    /// Color attachment store operation.
    /// </summary>
    public AttachmentStoreOp ColorStoreOp { get; set; } = AttachmentStoreOp.Store;
    
    /// <summary>
    /// Depth attachment load operation.
    /// </summary>
    public AttachmentLoadOp DepthLoadOp { get; set; } = AttachmentLoadOp.Clear;
    
    /// <summary>
    /// Depth attachment store operation.
    /// </summary>
    public AttachmentStoreOp DepthStoreOp { get; set; } = AttachmentStoreOp.DontCare;
    
    /// <summary>
    /// Initial layout for color attachment.
    /// Must not be Undefined if ColorLoadOp is Load.
    /// </summary>
    public ImageLayout ColorInitialLayout { get; set; } = ImageLayout.Undefined;
    
    /// <summary>
    /// Final layout for color attachment.
    /// </summary>
    public ImageLayout ColorFinalLayout { get; set; } = ImageLayout.PresentSrcKhr;
    
    /// <summary>
    /// Initial layout for depth attachment.
    /// Must not be Undefined if DepthLoadOp is Load.
    /// </summary>
    public ImageLayout DepthInitialLayout { get; set; } = ImageLayout.Undefined;
    
    /// <summary>
    /// Final layout for depth attachment.
    /// </summary>
    public ImageLayout DepthFinalLayout { get; set; } = ImageLayout.DepthStencilAttachmentOptimal;
    
    /// <summary>
    /// Number of MSAA samples.
    /// </summary>
    public SampleCountFlags SampleCount { get; set; } = SampleCountFlags.Count1Bit;
    
    /// <summary>
    /// Batch strategy for sorting draw commands in this render pass.
    /// Controls how draw commands are ordered for performance optimization and correctness.
    /// Default uses standard batching without depth sorting.
    /// </summary>
    public IBatchStrategy BatchStrategy { get; set; } = new DefaultBatchStrategy();
}
