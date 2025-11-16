using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Descriptors;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Contract for HorizontalLayout component after alignment refactor.
/// This represents the expected public API surface.
/// 
/// IMPLEMENTATION NOTE: Actual class is partial with source-generated properties.
/// This contract shows the effective public API after source generation.
/// </summary>
public partial class HorizontalLayout : Container
{
    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    /// <summary>
    /// Creates a new HorizontalLayout instance.
    /// </summary>
    /// <param name="descriptorManager">Vulkan descriptor manager for UI rendering</param>
    public HorizontalLayout(IDescriptorManager descriptorManager);

    // ==========================================
    // PROPERTIES (SOURCE-GENERATED)
    // ==========================================

    /// <summary>
    /// Gets the current alignment vector for child positioning.
    /// Only the Y component is used (vertical alignment of children).
    /// X component is ignored but typically set to 0 by convention.
    /// 
    /// Standard Y range: -1 (top), 0 (center/middle), 1 (bottom)
    /// Values outside this range are permitted for advanced use cases.
    /// </summary>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _alignment.
    /// This property returns the current runtime value (after deferred updates are applied).
    /// </remarks>
    public Vector2D<float> Alignment { get; }

    /// <summary>
    /// Gets the target alignment value (including pending deferred updates).
    /// </summary>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _alignment.
    /// This property returns the value that will be active after ApplyUpdates() is called.
    /// </remarks>
    public Vector2D<float> TargetAlignment { get; }

    /// <summary>
    /// Gets whether children should be stretched vertically to fill content height.
    /// When true, all children will have height = content area height,
    /// and vertical alignment (Y component) is ignored.
    /// </summary>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _stretchChildren.
    /// Inherited behavior from previous implementation.
    /// </remarks>
    public bool StretchChildren { get; }

    // ==========================================
    // METHODS (SOURCE-GENERATED)
    // ==========================================

    /// <summary>
    /// Sets the alignment vector for child positioning.
    /// Only the Y component affects child positioning (vertical alignment).
    /// </summary>
    /// <param name="value">Alignment vector. Y: -1 (top) to 1 (bottom)</param>
    /// <param name="duration">Optional animation duration in seconds (default: 0 = immediate)</param>
    /// <param name="mode">Optional interpolation mode (default: Linear)</param>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _alignment.
    /// Update is deferred until ApplyUpdates() is called (typically next frame).
    /// </remarks>
    public void SetAlignment(Vector2D<float> value, float duration = 0, InterpolationMode mode = InterpolationMode.Linear);

    /// <summary>
    /// Sets whether children should be stretched vertically.
    /// </summary>
    /// <param name="value">True to stretch children to content height, false to use measured height</param>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _stretchChildren.
    /// Update is deferred until ApplyUpdates() is called.
    /// </remarks>
    public void SetStretchChildren(bool value);

    // ==========================================
    // INHERITED FROM CONTAINER
    // ==========================================

    // Properties and methods inherited from Container base class:
    // - Padding (get/set)
    // - Spacing (get/set)
    // - SafeArea (get/set)
    // - GetContentArea() - protected method
    // - Invalidate() - marks layout for recalculation
    // - IsLayoutInvalid (get)

    // ==========================================
    // LAYOUT ALGORITHM (PROTECTED OVERRIDE)
    // ==========================================

    /// <summary>
    /// Arranges child components horizontally with spacing and vertical alignment.
    /// 
    /// Algorithm:
    /// 1. Get content area (bounds - padding - safeArea)
    /// 2. For each child:
    ///    a. Measure child to get preferred size
    ///    b. Calculate X position (cumulative, left to right)
    ///    c. Calculate Y position based on Alignment.Y:
    ///       - If StretchChildren: Y = contentArea.Origin.Y
    ///       - Else: Y = contentArea.Origin.Y + (contentArea.Height - childHeight) * ((Alignment.Y + 1) / 2)
    ///    d. Calculate size:
    ///       - Width: measured width
    ///       - Height: StretchChildren ? contentArea.Height : measured height
    ///    e. Apply constraints: child.SetSizeConstraints(new Rectangle(x, y, w, h))
    ///    f. Advance X position: x += childWidth + Spacing.X
    /// </summary>
    protected override void UpdateLayout();

    // ==========================================
    // USAGE EXAMPLES
    // ==========================================

    /*
    // Example 1: Create toolbar with top-aligned children
    var toolbar = new HorizontalLayout(descriptorManager);
    toolbar.SetAlignment(new Vector2D<float>(0, Align.Top));
    toolbar.SetPadding(new Padding(10));
    toolbar.SetSpacing(new Vector2D<float>(5, 0));

    // Example 2: Create toolbar with predefined constant
    var toolbar = new HorizontalLayout(descriptorManager);
    toolbar.SetAlignment(Align.TopCenter); // Only Y matters (Top)
    
    // Example 3: Center children vertically (default)
    var toolbar = new HorizontalLayout(descriptorManager);
    toolbar.SetAlignment(new Vector2D<float>(0, 0)); // Center vertical
    // OR
    toolbar.SetAlignment(Align.MiddleCenter); // Same effect
    
    // Example 4: Stretch children to full height
    var toolbar = new HorizontalLayout(descriptorManager);
    toolbar.SetStretchChildren(true);
    // Alignment is ignored when stretching
    */
}
