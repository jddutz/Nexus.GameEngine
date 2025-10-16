using System.Runtime.CompilerServices;

using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines standard render passes as bit flags for efficient render ordering and filtering.
/// Render passes execute in bit-order (lowest bit first).
/// Only passes with draw commands are executed - empty passes are skipped with zero overhead.
/// </summary>
/// <remarks>
/// <para><strong>Design Philosophy:</strong></para>
/// <list type="bullet">
/// <item>Bit position determines execution order (bit 0 first, bit 10 last)</item>
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
    /// Background rendering pass.
    /// Renders skybox, background images, or solid colors at infinite depth.
    /// Executes THIRD (after depth setup, before main scene).
    /// Used by: Skyboxes, procedural skies, background images, solid colors.
    /// Note: Typically rendered at depth=1.0 (far plane) with depth test LEQUAL.
    /// </summary>
    public const uint Background = 1u << 2;  // 0b00000000100
    
    /// <summary>
    /// Main opaque geometry pass.
    /// Renders solid, non-transparent objects with depth testing and writing.
    /// Executes FOURTH (primary scene rendering).
    /// Used by: 3D models, terrain, buildings, characters, static geometry.
    /// Optimization: Render front-to-back for early-Z rejection.
    /// </summary>
    public const uint Main = 1u << 3;  // 0b00000001000
    
    /// <summary>
    /// Lighting pass (for deferred renderers).
    /// Accumulates lighting from all light sources using G-buffer data.
    /// Executes FIFTH (after G-buffer generation).
    /// Used by: Deferred shading pipelines, many-light scenarios.
    /// Note: Forward renderers compute lighting in Main pass instead.
    /// </summary>
    public const uint Lighting = 1u << 4;  // 0b00000010000
    
    /// <summary>
    /// Reflection pass.
    /// Renders reflective surfaces using screen-space reflections (SSR) or planar reflections.
    /// Executes SIXTH (after main scene available for reflection).
    /// Used by: Mirrors, water surfaces, metallic materials, glass.
    /// Techniques: SSR, planar reflections, cube maps, raytraced reflections.
    /// </summary>
    public const uint Reflection = 1u << 5;  // 0b00000100000
    
    /// <summary>
    /// Transparent geometry pass.
    /// Renders alpha-blended surfaces with depth testing but no depth writes.
    /// Executes SEVENTH (after all opaque geometry).
    /// Used by: Glass, water, smoke, foliage, alpha-blended sprites.
    /// Sorting: Back-to-front for correct alpha blending.
    /// Note: Disable depth writes, enable blending (SrcAlpha, OneMinusSrcAlpha).
    /// </summary>
    public const uint Transparent = 1u << 6;  // 0b00001000000
    
    /// <summary>
    /// Particle system pass.
    /// Renders particle effects with additive or alpha blending.
    /// Executes EIGHTH (after transparent geometry).
    /// Used by: Fire, smoke, sparks, magic effects, explosions, weather.
    /// Blending: Additive (One, One) or alpha (SrcAlpha, OneMinusSrcAlpha).
    /// Note: Often uses soft particles (depth fade) to blend with scene.
    /// </summary>
    public const uint Particles = 1u << 7;  // 0b00010000000
    
    /// <summary>
    /// Post-processing pass.
    /// Applies full-screen effects to final rendered image.
    /// Executes NINTH (after all scene rendering).
    /// Used by: Bloom, tone mapping, color grading, anti-aliasing, DOF, motion blur.
    /// Technique: Render scene to texture, apply effects as full-screen quad.
    /// </summary>
    public const uint Post = 1u << 8;  // 0b00100000000
    
    /// <summary>
    /// UI/HUD overlay pass.
    /// Renders screen-space UI elements without depth testing.
    /// Executes TENTH (after post-processing).
    /// Used by: HUD, menus, text, health bars, minimaps, crosshairs.
    /// Note: Orthographic projection, no depth test, alpha blending enabled.
    /// </summary>
    public const uint UI = 1u << 9;  // 0b01000000000
    
    /// <summary>
    /// Debug visualization pass.
    /// Renders development/editor overlays (wireframes, bounds, gizmos).
    /// Executes LAST (overlay all other rendering).
    /// Used by: Collision shapes, normals, light bounds, camera frustums.
    /// Note: Development only - disabled in release builds.
    /// </summary>
    public const uint Debug = 1u << 10;  // 0b10000000000
    
    // ==========================================
    // COMBINED MASKS
    // ==========================================
    
    /// <summary>
    /// All render passes combined.
    /// Use for components that should participate in all rendering.
    /// </summary>
    public const uint All = Shadow | Depth | Background | Main | Lighting | 
                           Reflection | Transparent | Particles | Post | UI | Debug;
    
    /// <summary>
    /// All opaque passes (Shadow, Depth, Main).
    /// Use for solid geometry that casts shadows.
    /// </summary>
    public const uint Opaque = Shadow | Depth | Main;
    
    /// <summary>
    /// All transparent passes (Transparent, Particles).
    /// Use for effects that require alpha blending.
    /// </summary>
    public const uint AlphaBlended = Transparent | Particles;
    
    /// <summary>
    /// Scene rendering passes (excludes UI, Post, Debug).
    /// Use for world-space objects.
    /// </summary>
    public const uint Scene = Shadow | Depth | Background | Main | Lighting | 
                             Reflection | Transparent | Particles;
    
    // ==========================================
    // METADATA
    // ==========================================
    
    /// <summary>
    /// Total number of defined render passes.
    /// </summary>
    public const int Count = 11;
    
    /// <summary>
    /// Maximum valid bit index (0-10).
    /// </summary>
    public const int MaxBitIndex = 10;
    
    // ==========================================
    // RENDER PASS CONFIGURATIONS
    // ==========================================
    
    /// <summary>
    /// Standard configurations for all render passes.
    /// Array is indexed by bit position (0-10) matching the pass constants.
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
            ColorFinalLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilReadOnlyOptimal,
            ClearDepthValue = 1.0f,
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
            ColorFinalLayout = ImageLayout.Undefined,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Front-to-back
        },
        
        // [2] Background - Skybox/background at far plane
        new()
        {
            Name = nameof(Background),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Clear,  // Clear to background
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Load from depth pass
            DepthStoreOp = AttachmentStoreOp.Store,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearColorValue = new(0.1f, 0.1f, 0.1f, 1.0f),  // Dark gray
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [3] Main - Primary opaque geometry
        new()
        {
            Name = nameof(Main),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep background
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Keep depth from prepass
            DepthStoreOp = AttachmentStoreOp.Store,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Front-to-back for early-Z
        },
        
        // [4] Lighting - Deferred lighting accumulation
        new()
        {
            Name = nameof(Lighting),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth needed
            ColorLoadOp = AttachmentLoadOp.Load,  // Load G-buffer
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [5] Reflection - Screen-space reflections
        new()
        {
            Name = nameof(Reflection),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Load scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Need depth for SSR
            DepthStoreOp = AttachmentStoreOp.Store,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [6] Transparent - Alpha-blended geometry
        new()
        {
            Name = nameof(Transparent),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep opaque scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Test against opaque depth
            DepthStoreOp = AttachmentStoreOp.DontCare,  // Don't write depth
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Back-to-front sorting
        },
        
        // [7] Particles - Particle effects
        new()
        {
            Name = nameof(Particles),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep existing scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,  // Soft particles need depth
            DepthStoreOp = AttachmentStoreOp.DontCare,  // Don't write depth
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Back-to-front or additive
        },
        
        // [8] Post - Post-processing effects
        new()
        {
            Name = nameof(Post),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth needed
            ColorLoadOp = AttachmentLoadOp.Load,  // Load final scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorFinalLayout = ImageLayout.ColorAttachmentOptimal,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
        },
        
        // [9] UI - Screen-space UI overlay
        new()
        {
            Name = nameof(UI),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.Undefined,  // No depth testing
            ColorLoadOp = AttachmentLoadOp.Load,  // Keep scene
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.DontCare,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorFinalLayout = ImageLayout.PresentSrcKhr,  // Ready for present
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()  // Painter's algorithm
        },
        
        // [10] Debug - Debug visualization overlay
        new()
        {
            Name = nameof(Debug),
            ColorFormat = Format.Undefined,  // Use swapchain format
            DepthFormat = Format.D32Sfloat,
            ColorLoadOp = AttachmentLoadOp.Load,  // Overlay on everything
            ColorStoreOp = AttachmentStoreOp.Store,
            DepthLoadOp = AttachmentLoadOp.Load,
            DepthStoreOp = AttachmentStoreOp.DontCare,
            ColorFinalLayout = ImageLayout.PresentSrcKhr,  // Final pass
            DepthFinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            ClearDepthValue = 1.0f,
            SampleCount = SampleCountFlags.Count1Bit,
            BatchStrategy = new DefaultBatchStrategy()
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
        Background => nameof(Background),
        Main => nameof(Main),
        Lighting => nameof(Lighting),
        Reflection => nameof(Reflection),
        Transparent => nameof(Transparent),
        Particles => nameof(Particles),
        Post => nameof(Post),
        UI => nameof(UI),
        Debug => nameof(Debug),
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
    /// Gets the bit index of a single render pass (0-10).
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
    /// <returns>Individual pass bits in execution order (bit 0 to bit 10)</returns>
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
    /// <returns>Number of set bits (0-11)</returns>
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
