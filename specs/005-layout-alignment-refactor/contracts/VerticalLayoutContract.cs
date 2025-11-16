using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Descriptors;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Contract for VerticalLayout component after alignment refactor.
/// This represents the expected public API surface.
/// 
/// IMPLEMENTATION NOTE: Actual class is partial with source-generated properties.
/// This contract shows the effective public API after source generation.
/// </summary>
public partial class VerticalLayout : Container
{
    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    /// <summary>
    /// Creates a new VerticalLayout instance.
    /// </summary>
    /// <param name="descriptorManager">Vulkan descriptor manager for UI rendering</param>
    public VerticalLayout(IDescriptorManager descriptorManager);

    // ==========================================
    // PROPERTIES (SOURCE-GENERATED)
    // ==========================================

    /// <summary>
    /// Gets the current alignment vector for child positioning.
    /// Only the X component is used (horizontal alignment of children).
    /// Y component is ignored but typically set to 0 by convention.
    /// 
    /// Standard X range: -1 (left), 0 (center), 1 (right)
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
    /// Gets whether children should be stretched horizontally to fill content width.
    /// When true, all children will have width = content area width,
    /// and horizontal alignment (X component) is ignored.
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
    /// Only the X component affects child positioning (horizontal alignment).
    /// </summary>
    /// <param name="value">Alignment vector. X: -1 (left) to 1 (right)</param>
    /// <param name="duration">Optional animation duration in seconds (default: 0 = immediate)</param>
    /// <param name="mode">Optional interpolation mode (default: Linear)</param>
    /// <remarks>
    /// Source-generated from [ComponentProperty] private field _alignment.
    /// Update is deferred until ApplyUpdates() is called (typically next frame).
    /// </remarks>
    public void SetAlignment(Vector2D<float> value, float duration = 0, InterpolationMode mode = InterpolationMode.Linear);

    /// <summary>
    /// Sets whether children should be stretched horizontally.
    /// </summary>
    /// <param name="value">True to stretch children to content width, false to use measured width</param>
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
    /// Arranges child components vertically with spacing and horizontal alignment.
    /// 
    /// Algorithm:
    /// 1. Get content area (bounds - padding - safeArea)
    /// 2. For each child:
    ///    a. Measure child to get preferred size
    ///    b. Calculate size first (needed for position calculation):
    ///       - Width: StretchChildren ? contentArea.Width : measured width
    ///       - Height: measured height
    ///    c. Calculate X position based on Alignment.X:
    ///       - If StretchChildren: X = contentArea.Origin.X
    ///       - Else: X = contentArea.Origin.X + (contentArea.Width - childWidth) * ((Alignment.X + 1) / 2)
    ///    d. Calculate Y position (cumulative, top to bottom)
    ///    e. Apply constraints: child.SetSizeConstraints(new Rectangle(x, y, w, h))
    ///    f. Advance Y position: y += childHeight + Spacing.Y
    /// </summary>
    protected override void UpdateLayout();

    // ==========================================
    // USAGE EXAMPLES
    // ==========================================

    /*
    // Example 1: Create menu with left-aligned children
    var menu = new VerticalLayout(descriptorManager);
    menu.SetAlignment(new Vector2D<float>(Align.Left, 0));
    menu.SetPadding(new Padding(20));
    menu.SetSpacing(new Vector2D<float>(0, 10));

    // Example 2: Create menu with predefined constant
    var menu = new VerticalLayout(descriptorManager);
    menu.SetAlignment(Align.TopLeft); // Only X matters (Left)
    
    // Example 3: Center children horizontally (default)
    var menu = new VerticalLayout(descriptorManager);
    menu.SetAlignment(new Vector2D<float>(0, 0)); // Center horizontal
    // OR
    menu.SetAlignment(Align.MiddleCenter); // Same effect
    
    // Example 4: Stretch children to full width
    var menu = new VerticalLayout(descriptorManager);
    menu.SetStretchChildren(true);
    // Alignment is ignored when stretching
    */
}
