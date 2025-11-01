namespace Nexus.GameEngine.Components;

public class ConfigurationEventArgs(Template? template)
{
    public Template? Template { get; init; } = template;
}