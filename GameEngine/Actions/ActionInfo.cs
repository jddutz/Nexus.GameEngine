namespace Nexus.GameEngine.Actions;

/// <summary>
/// Information about a discovered game action.
/// </summary>
public record ActionInfo(
    Type ActionType,
    string ActionName,
    string DefaultInput,
    string Description,
    string Category,
    bool EnabledByDefault,
    int Priority
);