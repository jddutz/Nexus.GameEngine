using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using System;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Layout controller that arranges children vertically.
/// </summary>
public partial class VerticalLayoutController : LayoutController
{
    /// <summary>
    /// Gap between adjacent children in pixels.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected float _itemSpacing = 0.0f;

    /// <summary>
    /// Cross-axis alignment from -1.0 (left) to 1.0 (right).
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
            Log.Debug("[VerticalLayoutController] UpdateLayout - container is null");
            return;
        }

        Log.Debug($"[VerticalLayoutController] UpdateLayout - container: {container.Name}, spacing: {Spacing}, alignment: {Alignment}, itemSpacing: {ItemSpacing}");

        var children = new System.Collections.Generic.List<UserInterfaceElement>();
        foreach (var child in container.Children)
        {
            if (child is UserInterfaceElement uiElement)
            {
                // Filter zero-height children
                if (uiElement.Size.Y > 0)
                {
                    children.Add(uiElement);
                }
                else
                {
                    Log.Debug($"[VerticalLayoutController] Skipping zero-height child: {uiElement.Name}");
                }
            }
        }

        if (children.Count == 0)
        {
            Log.Debug("[VerticalLayoutController] No children to layout");
            return;
        }

        Log.Debug($"[VerticalLayoutController] Laying out {children.Count} children");

        // Clamp alignment
        float alignment = Math.Clamp(Alignment, -1.0f, 1.0f);

        if (Spacing == SpacingMode.Stacked)
        {
            Log.Debug("[VerticalLayoutController] Using Stacked spacing mode");
            float currentY = 0f;
            float containerWidth = container.Size.X;

            foreach (var child in children)
            {
                // Calculate X based on alignment
                float childWidth = child.Size.X;
                float t = alignment;
                float childX = t * (containerWidth - childWidth);

                Log.Debug($"[VerticalLayoutController] Child {child.Name}: position=({childX}, {currentY}), size=({childWidth}, {child.Size.Y})");
                child.SetCurrentPosition(new Vector2D<float>(childX, currentY));

                currentY += child.Size.Y + ItemSpacing;
            }
        }
        else if (Spacing == SpacingMode.Justified)
        {
            Log.Debug("[VerticalLayoutController] Using Justified spacing mode");
            float totalChildHeight = 0;
            foreach (var child in children) totalChildHeight += child.Size.Y;

            float availableSpace = container.Size.Y - totalChildHeight;
            float gap = 0;
            if (children.Count > 1)
            {
                gap = availableSpace / (children.Count - 1);
            }

            Log.Debug($"[VerticalLayoutController] totalChildHeight={totalChildHeight}, availableSpace={availableSpace}, gap={gap}");

            float currentY = 0;
            float containerWidth = container.Size.X;

            foreach (var child in children)
            {
                float childWidth = child.Size.X;
                float t = alignment;
                float childX = t * (containerWidth - childWidth);

                Log.Debug($"[VerticalLayoutController] Child {child.Name}: position=({childX}, {currentY})");
                child.SetCurrentPosition(new Vector2D<float>(childX, currentY));

                currentY += child.Size.Y + gap;
            }
        }
        else if (Spacing == SpacingMode.Distributed)
        {
            Log.Debug("[VerticalLayoutController] Using Distributed spacing mode");
            float totalChildHeight = 0;
            foreach (var child in children) totalChildHeight += child.Size.Y;

            float availableSpace = container.Size.Y - totalChildHeight;
            float gap = 0;
            if (children.Count > 0)
            {
                gap = availableSpace / (children.Count + 1);
            }

            Log.Debug($"[VerticalLayoutController] totalChildHeight={totalChildHeight}, availableSpace={availableSpace}, gap={gap}");

            float currentY = gap;
            float containerWidth = container.Size.X;

            foreach (var child in children)
            {
                float childWidth = child.Size.X;
                float t = alignment;
                float childX = t * (containerWidth - childWidth);

                Log.Debug($"[VerticalLayoutController] Child {child.Name}: position=({childX}, {currentY})");
                child.SetCurrentPosition(new Vector2D<float>(childX, currentY));

                currentY += child.Size.Y + gap;
            }
        }
    }
}
