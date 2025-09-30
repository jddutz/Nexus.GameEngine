DO NOT change any code without explicit instructions to do so.

This project follows a documentation-first test-driven-development approach. See Tests\README.md for comprehensive testing guidelines.

Before updating any code:

1. Build the solution and verify that the build succeeds without any errors
   - If there are any errors, confirm:
     - does the code need to be fixed, or
     - do the tests need to be revised
   - Fix the errors before proceeding
2. Update any documentation related to the change:
   - `.docs\Project Structure.md`
   - `.docs\Silk dotNET Setup.md`
   - `README.md`
3. Generate unit tests to cover all new behaviors
4. (Red Phase) Confirm that the new tests fail as expected
5. Update the code to implement the changes
6. (Green Phase) Confirm that the tests now pass
7. Rebuild the project and address any warnings and errors
8. Provide a summary (via chat) of the changes made and instructions for manual testing required

Use the .github/agent/ folder for temporary files:

- Work instructions or plans
- Checklists
- Temporary scripts
- Progress tracking
- Ouput files

use dotnet command for editing CSPROJ and SLN files.

All classes and interfaces should be defined in separate files.

To build, use the following command:

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test
```

## Architecture Overview

### Content Management System

The engine uses a **ContentManager + Viewport** architecture for UI and content management:

- **ContentManager**: Manages reusable content trees (menus, screens, levels) with caching
- **Viewport**: Manages screen regions with cameras and content assignment
- **Renderer**: Renders viewport content trees via IRenderer.Viewport property

### Key Architectural Principles

**Application Startup Pattern:**

```csharp
// CORRECT: Use StartupTemplate for deferred component creation
var app = services.GetRequiredService<IApplication>();
app.StartupTemplate = Templates.MainMenu;  // Created after window initialization
await app.RunAsync();
```

**IMPORTANT**: Do not create InputContext-dependent components (KeyBinding, MouseBinding, InputMap) before calling `app.RunAsync()`. These components require the window to be initialized first. Use the StartupTemplate property instead of directly calling `contentManager.GetOrCreate()` in startup code.

The Application class automatically defers StartupTemplate instantiation until after the WindowLoaded event fires, ensuring the InputContext is available when input binding components are created.

**Content Creation Pattern (for runtime content changes):**

```csharp
// Use this pattern for content changes after application startup
var contentManager = services.GetRequiredService<IContentManager>();
var newContent = contentManager.GetOrCreate(Templates.SomeContent);

var renderer = services.GetRequiredService<IRenderer>();
renderer.Viewport.Content = newContent;
```

**RuntimeComponent System Design Summary**
Core Principle: Everything is an IRuntimeComponent that can contain other components via a Children property. Components implement only the behavior interfaces they need.

Components implement only the behaviors they need:

**Declarative UI Syntax for Components**

```csharp
public class PlaceholderUI : ComponentTemplate
{
    public override IRuntimeComponent[] Components =>
    [
        new Border
        {
            Style = BorderStyle.Rectangle,
            Layout = new VerticalLayout
            {
                Alignment = Alignment.Center,
                Padding = new Thickness(20),
                Spacing = 5,
                Subcomponents =
                [
                    new TextElement { Text = "No User Interface loaded." },
                    new TextElement { Text = "Set Renderer.Viewport.Content with ContentManager.GetOrCreate() in your startup code." },
                    new TextElement { Text = "Press F12 to toggle full screen mode" },
                    new TextElement { Text = "Press ESC to quit" }
                ]
            }
        },
        new KeyBinding
        {
            Key = Key.F12,
            ActionType = typeof(ToggleFullscreenAction)
        },
        new KeyBinding
        {
            Key = Key.Escape,
            ActionType = typeof(QuitGameAction)
        }
    ];
}
```

**Logging**:

All logging should be structured using ILogger.

For RuntimeComponents, ComponentFactory sets the Logger when a component is created. The logger should not be injected. Logging via RuntimeComponent looks like this:

    Logger?.LogDebug("Input context not available, cannot activate {ComponentType}", GetType().Name);

For non-RuntimeComponent services, inject ILoggerFactory to create a logger with the proper context:

    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Service));
