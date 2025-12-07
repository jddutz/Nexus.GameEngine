using Nexus.GameEngine.Actions;

namespace Nexus.GameEngine.Runtime.Systems;

internal sealed class InputSystem : IInputSystem
{
    private readonly IWindowService _windowService;
    internal IActionFactory ActionFactory { get; }
    private IInputContext? _inputContext;

    internal IInputContext InputContext
    {
        get
        {
            if (_inputContext == null)
            {
                _inputContext = _windowService.InputContext;
            }
            return _inputContext;
        }
    }

    internal IKeyboard Keyboard => InputContext.Keyboards[0];
    internal IMouse Mouse => InputContext.Mice[0];

    public InputSystem(IWindowService windowService, IActionFactory actionFactory)
    {
        _windowService = windowService;
        ActionFactory = actionFactory;
    }
}
