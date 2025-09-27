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

The application does not use any logging services. For debugging, we use Debug.WriteLine().

To build, use the following command:

```bash
dotnet build NexusRealms.sln --configuration Debug
dotnet test
```

RuntimeComponent System Design Summary
Core Principle: Everything is an IRuntimeComponent that can contain other components via a Children property. Components implement only the behavior interfaces they need.

Components implement only the behaviors they need:

Declarative UI Syntax for Components

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
                    new TextElement { Text = "Call UserInterfaceManager.SetCurrent() in UserInterfaceManager.OnStartup." },
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
