namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines standard render passes as bit flags for efficient render ordering and filtering.
/// Render passes execute in bit-order (lowest bit first).
/// Only passes with draw commands are executed - empty passes are skipped with zero overhead.
/// </summary>
/// <remarks>
/// <para><strong>Design Philosophy:</strong></para>
/// <list type="bullet">
/// <item>Bit position determines execution order (bit 0 first, bit 7 last)</item>
/// <item>Multiple passes can be combined with bitwise OR (|)</item>
/// <item>Zero runtime cost for unused passes - they're simply skipped</item>
/// <item>Comprehensive set covers 95% of game rendering scenarios</item>
/// </list>
/// 
/// <para><strong>Usage Examples:</strong></para>
/// <code>
/// // Single pass
/// RenderMask = RenderPasses.Main
/// 
/// // Multiple passes
/// RenderMask = RenderPasses.Main | RenderPasses.Shadow
/// 
/// // Check if pass is active
/// if ((mask &amp; RenderPasses.Transparent) != 0) { ... }
/// </code>
/// </remarks>
public static class RenderPasses
{
    // ==========================================
    // PASS DEFINITIONS (Bit Order = Execution Order)
    // ==========================================
    
    /// <summary>
    /// Shadow map generation pass.
    /// Renders scene from light's perspective to generate shadow maps.
    /// Executes FIRST (depth-only rendering).
    /// Used by: Shadow casters (world geometry, characters, props).
    /// </summary>
    public const uint Shadow = 1u << 0;  // 0b00000000001
    
    /// <summary>
    /// Depth prepass (optional optimization).
    /// Renders opaque geometry to depth buffer only, enabling early-Z rejection.
    /// Executes SECOND (before main pass).
    /// Used by: Complex scenes with heavy overdraw, deferred renderers.
    /// Performance: Reduces pixel shader invocations by 50-70% in dense scenes.
    /// </summary>
    public const uint Depth = 1u << 1;  // 0b00000000010
    
    /// <summary>
    /// Main opaque geometry pass.
    /// Renders solid, non-transparent objects with depth testing and writing.
    /// Clears color and depth buffers if it's the first active pass.
    /// Executes THIRD (primary scene rendering).
    /// Used by: 3D models, terrain, buildings, characters, static geometry, backgrounds.
    /// Optimization: Render front-to-back for early-Z rejection.
    /// </summary>
    public const uint Main = 1u << 2;  // 0b00000000100
    
    /// <summary>
    /// Lighting pass (for deferred renderers).
    /// Accumulates lighting from all light sources using G-buffer data.
    /// Executes FOURTH (after G-buffer generation).
    /// Used by: Deferred shading pipelines, many-light scenarios.
    /// Note: Forward renderers compute lighting in Main pass instead.
    /// </summary>
    public const uint Lighting = 1u << 3;  // 0b00000001000
    
    /// <summary>
    /// Reflection pass.
    /// Renders reflective surfaces using screen-space reflections (SSR) or planar reflections.
    /// Executes FIFTH (after main scene available for reflection).
    /// Used by: Mirrors, water surfaces, metallic materials, glass.
    /// Techniques: SSR, planar reflections, cube maps, raytraced reflections.
    /// </summary>
    public const uint Reflection = 1u << 4;  // 0b00000010000
    
    /// <summary>
    /// Transparent geometry pass.
    /// Renders alpha-blended surfaces with depth testing but no depth writes.
    /// Executes SIXTH (after all opaque geometry).
    /// Used by: Glass, water, smoke, foliage, alpha-blended sprites, particle effects.
    /// Sorting: Back-to-front for correct alpha blending.
    /// Note: Disable depth writes, enable blending (SrcAlpha, OneMinusSrcAlpha or additive).
    /// </summary>
    public const uint Transparent = 1u << 5;  // 0b00000100000
    
    /// <summary>
    /// Post-processing pass.
    /// Applies full-screen effects to final rendered image.
    /// Executes SEVENTH (after all scene rendering).
    /// Used by: Bloom, tone mapping, color grading, anti-aliasing, DOF, motion blur.
    /// Technique: Render scene to texture, apply effects as full-screen quad.
    /// </summary>
    public const uint Post = 1u << 6;  // 0b00001000000
    
    /// <summary>
    /// UI/HUD overlay pass.
    /// Renders screen-space UI elements and debug overlays without depth testing.
    /// Executes LAST (after post-processing).
    /// Used by: HUD, menus, text, health bars, minimaps, crosshairs, debug visualizations.
    /// Note: Orthographic projection, no depth test, alpha blending enabled.
    /// </summary>
    public const uint UI = 1u << 7;  // 0b00010000000
    
    // ==========================================
    // COMBINED MASKS
    // ==========================================
    
    /// <summary>
    /// All render passes combined.
    /// Use for components that should participate in all rendering.
    /// </summary>
    public const uint All = Shadow | Depth | Main | Lighting | 
                           Reflection | Transparent | Post | UI;
    
    /// <summary>
    /// All opaque passes (Shadow, Depth, Main).
    /// Use for solid geometry that casts shadows.
    /// </summary>
    public const uint Opaque = Shadow | Depth | Main;
    
    /// <summary>
    /// Alpha-blended pass (Transparent only - includes particles).
    /// Use for effects that require alpha blending.
    /// </summary>
    public const uint AlphaBlended = Transparent;
    
    /// <summary>
    /// Scene rendering passes (excludes UI, Post).
    /// Use for world-space objects.
    /// </summary>
    public const uint Scene = Shadow | Depth | Main | Lighting | 
                             Reflection | Transparent;
    
    // ==========================================
    // METADATA
    // ==========================================
    
    /// <summary>
    /// Total number of defined render passes.
    /// </summary>
    public const int Count = 8;
    
    /// <summary>
    /// Maximum valid bit index (0-7).
    /// </summary>
    public const int MaxBitIndex = 7;
    
    // ==========================================
    // RENDER PASS CONFIGURATIONS
    // ==========================================
    
    /// <summary>
    /// Standard configurations for all render passes.
    /// Array is indexed by bit position (0-7) matching the pass constants.
    /// Used by SwapChain to create Vulkan render pass objects.
    /// </summary>
    /// <remarks>
    /// Each configuration defines how a render pass should be created:
    /// - Attachment formats (color, depth)
    /// - Load/store operations
    /// - Clear values
    /// - Batch strategy for command sorting
    /// </remarks>
    public static RenderPassConfiguration[] Configurations => 
    [
        // [0] Shadow - Depth-only shadow map generation
        new()
        {
            Name = nameof(Shadow),
            ColorFormat = Format.Undefined,  // No color attachment
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.DontCare,
            ColorStoreOp = AttachmentStoreOp.DontCare,
            DepthLoadOp = AttachmentLoadOp.Clear,
            DepthStoreOp = AttachmentStoreOp.Store,  // Save shadow map
            ColorInitialLayout = ImageLayout.Undefined,  // DontCare + no color attachment
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilReadOnlyOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [1] Depth - Depth prepass for early-Z
        new()
        {
            Name = nameof(Depth),
            ColorFormat = Format.Undefined,  // No color writes
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.DontCare,
            ColorStoreOp = AttachmentStoreOp.DontCare,
            DepthLoadOp = AttachmentLoadOp.Clear,
            DepthStoreOp = AttachmentStoreOp.Store,  // Save for main pass
            ColorInitialLayout = ImageLayout.Undefined,  // DontCare + no color attachment
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Front-to-back
        },
        
        // [2] Main - Primary opaque geometry (also handles background clearing)
        new()
        {
            Name = nameof(Main),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Clear,  // Clear color buffer
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Clear,  // Clear depth buffer
            DepthStoreOp = AttachmentStoreOp.Store,
            ColorInitialLayout = ImageLayout.Undefined,  // Clear discards previous contents
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.Undefined,  // First pass with depth
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Front-to-back for early-Z
        },
        
        // [3] Lighting - Deferred lighting accumulation
        new()
        {
            Name = nameof(Lighting),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth needed
            ColorLoadOp = AttachmentLoadOp.Load,  // Load G-buffer
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorInitialLayout = ImageLayout.ColorAttachmentOptimal,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [4] Reflection - Screen-space reflections
        new()
        {
            Name = nameof(Reflection),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Load scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Need depth for SSR
            DepthStoreOp = AttachmentStoreOp.Store,
            ColorInitialLayout = ImageLayout.ColorAttachmentOptimal,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [5] Transparent - Alpha-blended geometry and particles
        new()
        {
            Name = nameof(Transparent),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep opaque scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Test against opaque depth
            DepthStoreOp = AttachmentStoreOp.DontCare,  // Don't write depth
            ColorInitialLayout = ImageLayout.ColorAttachmentOptimal,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Back-to-front sorting
        },
        
        // [6] Post - Post-processing effects
        new()
        {
            Name = nameof(Post),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth needed
            ColorLoadOp = AttachmentLoadOp.Load,  // Load final scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorInitialLayout = ImageLayout.ColorAttachmentOptimal,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthInitialLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [7] UI - Screen-space UI overlay and debug visualizations
        new()
        {
            Name = nameof(UI),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth testing
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorInitialLayout = ImageLayout.ColorAttachmentOptimal,
            ColorFinalLayout = ImageLayout.PresentSrcKhr,  // Ready for present
            DepthInitialLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Painter's algorithm
        }
    ];
    
    // ==========================================
    // UTILITY METHODS
    // ==========================================
    
    /// <summary>
    /// Gets the human-readable name of a single render pass.
    /// </summary>
    /// <param name="pass">Single render pass bit (must be exactly one bit set)</param>
    /// <returns>Pass name or "Unknown" if invalid</returns>
    public static string GetName(uint pass) => pass switch
    {
        Shadow => nameof(Shadow),
        Depth => nameof(Depth),
        Main => nameof(Main),
        Lighting => nameof(Lighting),
        Reflection => nameof(Reflection),
        Transparent => nameof(Transparent),
        Post => nameof(Post),
        UI => nameof(UI),
        _ => $"Unknown(0x{pass:X})"
    };
    
    /// <summary>
    /// Gets all individual pass names from a combined mask.
    /// </summary>
    /// <param name="mask">Bit mask with multiple passes</param>
    /// <returns>Comma-separated list of pass names</returns>
    public static string GetNames(uint mask)
    {
        if (mask == 0) return "None";
        if (mask == All) return "All";
        
        var names = new List<string>();
        for (int i = 0; i <= MaxBitIndex; i++)
        {
            uint bit = 1u << i;
            if ((mask & bit) != 0)
                names.Add(GetName(bit));
        }
        
        return string.Join(", ", names);
    }
    
    /// <summary>
    /// Gets the bit index of a single render pass (0-9).
    /// Returns -1 if mask doesn't represent exactly one pass.
    /// </summary>
    /// <param name="pass">Single render pass bit</param>
    /// <returns>Bit index or -1 if invalid</returns>
    public static int GetIndex(uint pass)
    {
        // Check if exactly one bit is set
        if (pass == 0 || (pass & (pass - 1)) != 0)
            return -1;
        
        // Manual trailing zero count
        int index = 0;
        while ((pass & 1) == 0)
        {
            pass >>= 1;
            index++;
        }
        return index;
    }
    
    /// <summary>
    /// Enumerates all active passes in a mask in execution order.
    /// Use this to iterate passes for rendering.
    /// </summary>
    /// <param name="mask">Bit mask with one or more passes</param>
    /// <returns>Individual pass bits in execution order (bit 0 to bit 9)</returns>
    public static IEnumerable<uint> GetActivePasses(uint mask)
    {
        for (int i = 0; i <= MaxBitIndex; i++)
        {
            uint pass = 1u << i;
            if ((mask & pass) != 0)
                yield return pass;
        }
    }
    
    /// <summary>
    /// Checks if a specific pass is enabled in the mask.
    /// Convenience method for validation and debugging - NOT for hot paths.
    /// </summary>
    /// <param name="mask">Bit mask to check</param>
    /// <param name="pass">Pass to check for</param>
    /// <returns>True if pass is present in mask</returns>
    /// <remarks>
    /// <para><strong>Performance Note:</strong></para>
    /// This is a helper method for readability in non-critical code.
    /// In performance-critical paths (like Renderer), use direct bitwise operations instead:
    /// <code>if ((mask &amp; pass) != 0) { ... }</code>
    /// </remarks>
    public static bool HasPass(uint mask, uint pass)
    {
        return (mask & pass) != 0;
    }
    
    /// <summary>
    /// Counts the number of active passes in a mask.
    /// </summary>
    /// <param name="mask">Bit mask to count</param>
    /// <returns>Number of set bits (0-10)</returns>
    public static int CountPasses(uint mask)
    {
        // Manual popcount (count set bits)
        int count = 0;
        while (mask != 0)
        {
            count += (int)(mask & 1);
            mask >>= 1;
        }
        return count;
    }
    
    /// <summary>
    /// Validates that a mask only contains defined render passes.
    /// </summary>
    /// <param name="mask">Bit mask to validate</param>
    /// <returns>True if all bits are valid render passes</returns>
    public static bool IsValid(uint mask)
    {
        return (mask & ~All) == 0;
    }
}
