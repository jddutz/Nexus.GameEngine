using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Input.Components;
using Silk.NET.Input;
using TestApp.Tests;

namespace TestApp;

/// <summary>
/// Provides declarative templates for application components used in TestApp.
/// </summary>
public static partial class Templates
{
    /// <summary>
    /// MainMenu template defines the root component tree for the TestApp main menu.
    /// Includes the TestRunner and key bindings for toggling fullscreen and quitting the application.
    /// Uses RuntimeComponentTemplate so it participates in the update lifecycle.
    /// </summary>
    public static readonly RuntimeComponentTemplate Tests = new()
    {
        // Set required properties here
        Name = "Tests",
        Subcomponents =
        [
            new TestRunnerTemplate()
            {
                Name = "Test Runner",
                Subcomponents = [
                    new ComponentLifecycleTestTemplate()
                    {
                        Name = "Component Lifecycle Test"
                    },
                    new RenderableTestTemplate()
                    {
                        Name = "Renderable Test"
                    },
                    new ProfilingActivationTestTemplate()
                    {
                        Name = "Profiling Activation Test",
                        FrameCount = 3
                    },
                    new SubsystemProfilingTestTemplate()
                    {
                        Name = "Subsystem Profiling Test",
                        FrameCount = 10
                    },
                    new BottleneckIdentificationTestTemplate()
                    {
                        Name = "Bottleneck Identification Test",
                        FrameCount = 15
                    }
                ]
            },
            new KeyBindingTemplate()
            {
                Name = "Toggle FullScreen (F12)",
                Key = Key.F12,
                ActionId = ActionId.FromType<ToggleFullscreenAction>()
            },
            new KeyBindingTemplate()
            {
                Name = "Quit (ESC)",
                Key = Key.Escape,
                ActionId = ActionId.FromType<QuitGameAction>()
            }
        ]
    };
}
