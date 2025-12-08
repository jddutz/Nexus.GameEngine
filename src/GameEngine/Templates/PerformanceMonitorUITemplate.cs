using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Performance;
using Silk.NET.Maths;
using System.Collections.Generic;

namespace Nexus.GameEngine.Templates;

/// <summary>
/// A drop-in UI overlay that displays real-time performance metrics including FPS, frame time, and subsystem timings.
/// </summary>
public record PerformanceMonitorUITemplate : UserInterfaceElementTemplate
{
    /// <summary>
    /// Creates a new instance of the PerformanceMonitorUITemplate.
    /// </summary>
    public PerformanceMonitorUITemplate()
    {
        // Configure root element
        Position = new Vector2D<float>(10, 10);
        Alignment = new Vector2D<float>(-1.0f, -1.0f); // TopLeft
        
        // Add PerformanceMonitor component (data source)
        var perfMonitor = new PerformanceMonitorTemplate
        {
            Enabled = true,
            UpdateIntervalSeconds = 0.5,
            WarningThresholdMs = 16.67
        };

        // Add VerticalLayoutController
        var layout = new VerticalLayoutControllerTemplate
        {
            ItemSpacing = 2.0f,
            Alignment = -1.0f,
            Spacing = SpacingMode.Stacked
        };

        // Add TextRenderers
        var fpsText = new TextRendererTemplate
        {
            Bindings = new IPropertyBinding[]
            {
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.CurrentFpsChanged += (s, e) => tr.SetText($"FPS: {e.NewValue:F1}", null),
                    unsubscribe: (pm, tr) => pm.CurrentFpsChanged -= (s, e) => tr.SetText($"FPS: {e.NewValue:F1}", null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            }
        };

        var avgFpsText = new TextRendererTemplate
        {
            Bindings = new IPropertyBinding[]
            {
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.AverageFpsChanged += (s, e) => tr.SetText($"Avg: {e.NewValue:F1}", null),
                    unsubscribe: (pm, tr) => pm.AverageFpsChanged -= (s, e) => tr.SetText($"Avg: {e.NewValue:F1}", null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            }
        };

        var frameTimeText = new TextRendererTemplate
        {
            Bindings = new IPropertyBinding[]
            {
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.CurrentFrameTimeMsChanged += (s, e) => tr.SetText($"Frame: {e.NewValue:F2}ms", null),
                    unsubscribe: (pm, tr) => pm.CurrentFrameTimeMsChanged -= (s, e) => tr.SetText($"Frame: {e.NewValue:F2}ms", null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            }
        };

        var updateTimeText = new TextRendererTemplate
        {
            Bindings = new IPropertyBinding[]
            {
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.UpdateTimeMsChanged += (s, e) => tr.SetText($"Update: {e.NewValue:F2}ms", null),
                    unsubscribe: (pm, tr) => pm.UpdateTimeMsChanged -= (s, e) => tr.SetText($"Update: {e.NewValue:F2}ms", null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            }
        };

        var renderTimeText = new TextRendererTemplate
        {
            Bindings = new IPropertyBinding[]
            {
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.RenderTimeMsChanged += (s, e) => tr.SetText($"Render: {e.NewValue:F2}ms", null),
                    unsubscribe: (pm, tr) => pm.RenderTimeMsChanged -= (s, e) => tr.SetText($"Render: {e.NewValue:F2}ms", null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            }
        };

        var warningText = new TextRendererTemplate
        {
            Text = "PERFORMANCE WARNING",
            Color = new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f), // Red
            Bindings =
            [
                new PropertyBinding<PerformanceMonitor, TextRenderer>(
                    subscribe: (pm, tr) => pm.PerformanceWarningChanged += (s, e) => tr.SetVisible(e.NewValue, null),
                    unsubscribe: (pm, tr) => pm.PerformanceWarningChanged -= (s, e) => tr.SetVisible(e.NewValue, null),
                    lookup: Binding.SiblingLookup<PerformanceMonitor>())
            ]
        };

        Subcomponents =
        [
            perfMonitor,
            layout,
            fpsText,
            avgFpsText,
            frameTimeText,
            updateTimeText,
            renderTimeText,
            warningText
        ];
    }
}
