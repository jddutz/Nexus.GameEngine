namespace Nexus.GameEngine.Components;

public class ConfigurationEventArgs(IComponentTemplate? template)
{
    public IComponentTemplate? Template { get; init; } = template;
}