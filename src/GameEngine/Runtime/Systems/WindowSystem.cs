namespace Nexus.GameEngine.Runtime.Systems;

internal sealed class WindowSystem : IWindowSystem
{
    internal IWindow Window { get; }

    public WindowSystem(IWindowService windowService)
    {
        Window = windowService.GetWindow();
    }
}
