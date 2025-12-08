using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using System;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Layout controller that arranges children horizontally.
/// </summary>
public partial class HorizontalLayoutController : LayoutController
{
    /// <summary>
    /// Gap between adjacent children in pixels.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected float _itemSpacing = 0.0f;

    /// <summary>
    /// Cross-axis alignment from -1.0 (top) to 1.0 (bottom).
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected float _alignment = 0.0f;

    /// <summary>
    /// Distribution strategy for children.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected SpacingMode _spacing = SpacingMode.Stacked;

    /// <inheritdoc/>
    public override void UpdateLayout(UserInterfaceElement container)
    {
        if (container == null)
        {
            Log.Debug("[HorizontalLayoutController] UpdateLayout - container is null");
            return;
        }

        Log.Debug($"[HorizontalLayoutController] UpdateLayout - container: {container.Name}, spacing: {Spacing}, alignment: {Alignment}, itemSpacing: {ItemSpacing}");

        var children = new System.Collections.Generic.List<UserInterfaceElement>();
        foreach (var child in container.Children)
        {
            if (child is UserInterfaceElement uiElement)
            {
                // Filter zero-width children
                if (uiElement.Size.X > 0)
                {
                    children.Add(uiElement);
                }
                else
                {
                    Log.Debug($"[HorizontalLayoutController] Skipping zero-width child: {uiElement.Name}");
                }
            }
        }

        if (children.Count == 0)
        {
            Log.Debug("[HorizontalLayoutController] No children to layout");
            return;
        }

        Log.Debug($"[HorizontalLayoutController] Laying out {children.Count} children");

        // Clamp alignment
        float alignment = Math.Clamp(Alignment, -1.0f, 1.0f);

        if (Spacing == SpacingMode.Stacked)
        {
            Log.Debug("[HorizontalLayoutController] Using Stacked spacing mode");
            float currentX = 0f;
            float containerHeight = container.Size.Y;

            foreach (var child in children)
            {
                // Calculate Y based on alignment
                float childHeight = child.Size.Y;
                float t = alignment;
                float childY = t * (containerHeight - childHeight);

                Log.Debug($"[HorizontalLayoutController] Child {child.Name}: position=({currentX}, {childY}), size=({child.Size.X}, {childHeight})");
                child.SetCurrentPosition(new Vector2D<float>(currentX, childY));

                currentX += child.Size.X + ItemSpacing;
            }
        }
        else if (Spacing == SpacingMode.Justified)
        {
            Log.Debug("[HorizontalLayoutController] Using Justified spacing mode");
            float totalChildWidth = 0;
            foreach (var child in children) totalChildWidth += child.Size.X;

            float availableSpace = container.Size.X - totalChildWidth;
            float gap = 0;
            if (children.Count > 1)
            {
                gap = availableSpace / (children.Count - 1);
            }

            Log.Debug($"[HorizontalLayoutController] totalChildWidth={totalChildWidth}, availableSpace={availableSpace}, gap={gap}");

            float currentX = 0;
            float containerHeight = container.Size.Y;

            foreach (var child in children)
            {
                float childHeight = child.Size.Y;
                float t = alignment;
                float childY = t * (containerHeight - childHeight);

                Log.Debug($"[HorizontalLayoutController] Child {child.Name}: position=({currentX}, {childY})");
                child.SetCurrentPosition(new Vector2D<float>(currentX, childY));

                currentX += child.Size.X + gap;
            }
        }
        else if (Spacing == SpacingMode.Distributed)
        {
            Log.Debug("[HorizontalLayoutController] Using Distributed spacing mode");
            float totalChildWidth = 0;
            foreach (var child in children) totalChildWidth += child.Size.X;

            float availableSpace = container.Size.X - totalChildWidth;
            float gap = 0;
            if (children.Count > 0)
            {
                gap = availableSpace / (children.Count + 1);
            }

            Log.Debug($"[HorizontalLayoutController] totalChildWidth={totalChildWidth}, availableSpace={availableSpace}, gap={gap}");

            float currentX = gap;
            float containerHeight = container.Size.Y;

            foreach (var child in children)
            {
                float childHeight = child.Size.Y;
                float t = alignment;
                float childY = t * (containerHeight - childHeight);

                Log.Debug($"[HorizontalLayoutController] Child {child.Name}: position=({currentX}, {childY})");
                child.SetCurrentPosition(new Vector2D<float>(currentX, childY));

                currentX += child.Size.X + gap;
            }
        }
    }
}
