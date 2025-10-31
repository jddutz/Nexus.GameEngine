namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines standard placement strategies for background images.
/// Combines fill mode (stretch vs uniform) with alignment (which part to show when cropping).
/// </summary>
/// <remarks>
/// <para><strong>Design Philosophy:</strong></para>
/// <list type="bullet">
/// <item>BackgroundLayer always fills the entire viewport - no gaps, no letterboxing</item>
/// <item>Stretch mode distorts aspect ratio to fit viewport</item>
/// <item>Fill modes maintain aspect ratio and crop excess on one axis</item>
/// <item>Alignment determines which edge is anchored when cropping occurs</item>
/// </list>
/// 
/// <para><strong>Cropping Behavior:</strong></para>
/// <list type="bullet">
/// <item>When image aspect ratio &lt; viewport aspect ratio: image is narrow/tall, crops top and/or bottom</item>
/// <item>When image aspect ratio &gt; viewport aspect ratio: image is wide/short, crops left and/or right</item>
/// <item>Edge alignments (Top/Bottom/Left/Right) anchor to that edge, useful for off-center compositions</item>
/// </list>
/// 
/// <para><strong>Usage Examples:</strong></para>
/// <code>
/// // Stretch to fill (may distort)
/// ImagePlacement = BackgroundImagePlacement.Stretch
/// 
/// // Uniform scale, center and crop (DEFAULT)
/// ImagePlacement = BackgroundImagePlacement.FillCenter
/// 
/// // Uniform scale, anchor to top (e.g., hero images)
/// ImagePlacement = BackgroundImagePlacement.FillTop
/// 
/// // Uniform scale, anchor to left (e.g., menu on right, artwork on left)
/// ImagePlacement = BackgroundImagePlacement.FillLeft
/// </code>
/// </remarks>
public static class BackgroundImagePlacement
{
    // ==========================================
    // PLACEMENT DEFINITIONS
    // ==========================================
    
    /// <summary>
    /// Stretch image non-uniformly to fill viewport.
    /// May distort aspect ratio. No cropping occurs.
    /// </summary>
    public const int Stretch = 0;
    
    /// <summary>
    /// Scale uniformly to fill viewport, crop excess, center image.
    /// Maintains aspect ratio. Shows center portion when cropping.
    /// DEFAULT - handles most use cases.
    /// </summary>
    public const int FillCenter = 1;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to top edge.
    /// When image is narrow/tall: shows top, crops bottom.
    /// When image is wide/short: centers horizontally, shows top.
    /// Useful for hero images or headers with important content at top.
    /// </summary>
    public const int FillTop = 2;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to bottom edge.
    /// When image is narrow/tall: shows bottom, crops top.
    /// When image is wide/short: centers horizontally, shows bottom.
    /// Useful for footer images or ground-level scenes.
    /// </summary>
    public const int FillBottom = 3;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to left edge.
    /// When image is wide/short: shows left, crops right.
    /// When image is narrow/tall: centers vertically, shows left.
    /// Useful for off-center compositions with focus on left (e.g., menu on right, artwork on left).
    /// </summary>
    public const int FillLeft = 4;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to right edge.
    /// When image is wide/short: shows right, crops left.
    /// When image is narrow/tall: centers vertically, shows right.
    /// Useful for off-center compositions with focus on right (e.g., menu on left, artwork on right).
    /// </summary>
    public const int FillRight = 5;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to top-left corner.
    /// Shows top-left portion of image, crops bottom and/or right.
    /// Useful for images with important content in top-left.
    /// </summary>
    public const int FillTopLeft = 6;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to top-right corner.
    /// Shows top-right portion of image, crops bottom and/or left.
    /// Useful for images with important content in top-right.
    /// </summary>
    public const int FillTopRight = 7;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to bottom-left corner.
    /// Shows bottom-left portion of image, crops top and/or right.
    /// Useful for images with important content in bottom-left.
    /// </summary>
    public const int FillBottomLeft = 8;
    
    /// <summary>
    /// Scale uniformly to fill viewport, anchor to bottom-right corner.
    /// Shows bottom-right portion of image, crops top and/or left.
    /// Useful for images with important content in bottom-right.
    /// </summary>
    public const int FillBottomRight = 9;
    
    // ==========================================
    // METADATA
    // ==========================================
    
    /// <summary>
    /// Total number of defined placement modes.
    /// </summary>
    public const int Count = 10;
    
    /// <summary>
    /// Default placement mode (uniform fill, centered).
    /// </summary>
    public const int Default = FillCenter;
    
    // ==========================================
    // NAME MAPPINGS
    // ==========================================
    
    /// <summary>
    /// Human-readable names for each placement mode.
    /// Array is indexed by placement constant.
    /// </summary>
    public static readonly string[] Names =
    [
        nameof(Stretch),          // 0
        nameof(FillCenter),       // 1
        nameof(FillTop),          // 2
        nameof(FillBottom),       // 3
        nameof(FillLeft),         // 4
        nameof(FillRight),        // 5
        nameof(FillTopLeft),      // 6
        nameof(FillTopRight),     // 7
        nameof(FillBottomLeft),   // 8
        nameof(FillBottomRight)   // 9
    ];
    
    // ==========================================
    // ALIGNMENT VECTORS
    // ==========================================
    
    /// <summary>
    /// Alignment vectors for uniform fill modes.
    /// Defines which part of the image to show when cropping.
    /// X: 0=left, 0.5=center, 1=right
    /// Y: 0=top, 0.5=center, 1=bottom
    /// Array is indexed by placement constant.
    /// </summary>
    public static readonly Vector2D<float>[] Alignments =
    [
        new(0.5f, 0.5f),  // 0: Stretch (alignment not used but defined for consistency)
        new(0.5f, 0.5f),  // 1: FillCenter
        new(0.5f, 0.0f),  // 2: FillTop
        new(0.5f, 1.0f),  // 3: FillBottom
        new(0.0f, 0.5f),  // 4: FillLeft
        new(1.0f, 0.5f),  // 5: FillRight
        new(0.0f, 0.0f),  // 6: FillTopLeft
        new(1.0f, 0.0f),  // 7: FillTopRight
        new(0.0f, 1.0f),  // 8: FillBottomLeft
        new(1.0f, 1.0f)   // 9: FillBottomRight
    ];
    
    // ==========================================
    // FILL MODE FLAGS
    // ==========================================
    
    /// <summary>
    /// Indicates whether each placement mode uses uniform scaling.
    /// True = maintains aspect ratio (Fill modes)
    /// False = stretches non-uniformly (Stretch mode)
    /// Array is indexed by placement constant.
    /// </summary>
    public static readonly bool[] IsUniformScale =
    [
        false,  // 0: Stretch
        true,   // 1: FillCenter
        true,   // 2: FillTop
        true,   // 3: FillBottom
        true,   // 4: FillLeft
        true,   // 5: FillRight
        true,   // 6: FillTopLeft
        true,   // 7: FillTopRight
        true,   // 8: FillBottomLeft
        true    // 9: FillBottomRight
    ];
    
    // ==========================================
    // UTILITY METHODS
    // ==========================================
    
    /// <summary>
    /// Gets the human-readable name of a placement mode.
    /// </summary>
    /// <param name="placement">Placement mode constant</param>
    /// <returns>Placement name or "Unknown" if invalid</returns>
    public static string GetName(int placement)
    {
        if (placement < 0 || placement >= Count)
            return $"Unknown({placement})";
        return Names[placement];
    }
    
    /// <summary>
    /// Gets the alignment vector for a placement mode.
    /// Only relevant for uniform fill modes (not Stretch).
    /// </summary>
    /// <param name="placement">Placement mode constant</param>
    /// <returns>Alignment vector (X: 0-1 horizontal, Y: 0-1 vertical)</returns>
    public static Vector2D<float> GetAlignment(int placement)
    {
        if (placement < 0 || placement >= Count)
            return new(0.5f, 0.5f);  // Default to center
        return Alignments[placement];
    }
    
    /// <summary>
    /// Checks if a placement mode uses uniform scaling.
    /// </summary>
    /// <param name="placement">Placement mode constant</param>
    /// <returns>True if maintains aspect ratio (Fill modes), false if stretches (Stretch mode)</returns>
    public static bool UsesUniformScale(int placement)
    {
        if (placement < 0 || placement >= Count)
            return true;  // Default to uniform scaling
        return IsUniformScale[placement];
    }
    
    /// <summary>
    /// Validates that a placement mode is within defined range.
    /// </summary>
    /// <param name="placement">Placement mode to validate</param>
    /// <returns>True if placement is valid (0-5)</returns>
    public static bool IsValid(int placement)
    {
        return placement >= 0 && placement < Count;
    }
    
    /// <summary>
    /// Calculates UV bounds for rendering based on placement mode and aspect ratios.
    /// </summary>
    /// <param name="placement">Placement mode constant</param>
    /// <param name="imageWidth">Image width in pixels</param>
    /// <param name="imageHeight">Image height in pixels</param>
    /// <param name="viewportWidth">Viewport width in pixels</param>
    /// <param name="viewportHeight">Viewport height in pixels</param>
    /// <returns>UV bounds (min, max) for texture sampling</returns>
    /// <remarks>
    /// For Stretch mode, returns ((0,0), (1,1)) - use entire texture.
    /// For Fill modes, calculates UV bounds to crop image while maintaining aspect ratio.
    /// </remarks>
    public static (Vector2D<float> min, Vector2D<float> max) CalculateUVBounds(
        int placement,
        float imageWidth,
        float imageHeight,
        float viewportWidth,
        float viewportHeight)
    {
        // Stretch mode - use entire texture
        if (placement == Stretch)
            return (new(0f, 0f), new(1f, 1f));
        
        // Fill modes - calculate crop based on aspect ratio
        var imageAspect = imageWidth / imageHeight;
        var viewportAspect = viewportWidth / viewportHeight;
        
        var uvMin = new Vector2D<float>(0f, 0f);
        var uvMax = new Vector2D<float>(1f, 1f);
        var alignment = GetAlignment(placement);
        
        if (imageAspect > viewportAspect)
        {
            // Image is wider than viewport - crop horizontally
            var scale = viewportHeight / imageHeight;
            var scaledWidth = imageWidth * scale;
            var cropAmount = (scaledWidth - viewportWidth) / scaledWidth;
            
            // Apply horizontal alignment
            var uOffset = cropAmount * alignment.X;
            uvMin.X = uOffset;
            uvMax.X = 1f - (cropAmount - uOffset);
        }
        else if (imageAspect < viewportAspect)
        {
            // Image is taller than viewport - crop vertically
            var scale = viewportWidth / imageWidth;
            var scaledHeight = imageHeight * scale;
            var cropAmount = (scaledHeight - viewportHeight) / scaledHeight;
            
            // Apply vertical alignment
            var vOffset = cropAmount * alignment.Y;
            uvMin.Y = vOffset;
            uvMax.Y = 1f - (cropAmount - vOffset);
        }
        // else: Perfect aspect ratio match - no cropping needed
        
        return (uvMin, uvMax);
    }
}
