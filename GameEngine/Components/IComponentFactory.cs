namespace Nexus.GameEngine.Components;

public interface IComponentFactory
{
    public IRuntimeComponent? Create(Type componentType);
    public IRuntimeComponent? Create(Type componentType, IComponentTemplate template);
    public IRuntimeComponent? Create<T>()
        where T : IRuntimeComponent;
    public IRuntimeComponent? Create<T>(IComponentTemplate template)
        where T : IRuntimeComponent;
    public IRuntimeComponent? Instantiate(IComponentTemplate template);
}