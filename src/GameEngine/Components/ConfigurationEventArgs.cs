namespace Nexus.GameEngine.Components;

public class ConfigurationEventArgs(Configurable.Template? template)
{
    public Configurable.Template? Template { get; init; } = template;
}