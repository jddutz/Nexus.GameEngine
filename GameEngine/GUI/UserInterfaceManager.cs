using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.GUI.Abstractions;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Implements <see cref="IUserInterfaceManager"/>. Responsible for routing Update events to active UI components.
/// Manages user interface lifecycle, handles UI switching, caching, and disposal of user interfaces.
/// Rendering is handled by setting the active UI as the renderer's root component.
/// </summary>
public class UserInterfaceManager(
    ILogger<UserInterfaceManager> logger,
    IComponentFactory componentFactory,
    IRenderer renderer)
    : IUserInterfaceManager
{
    private readonly ILogger<UserInterfaceManager> _logger = logger;
    private readonly IComponentFactory _factory = componentFactory;
    private readonly IRenderer _renderer = renderer;
    private IUserInterface? _active;
    private readonly Dictionary<string, IUserInterface> _userInterfaces = [];

    /// <inheritdoc/>
    public IUserInterface? Active => _active;

    /// <inheritdoc/>
    public void Create(UserInterface.Template uiTemplate)
    {
        if (_userInterfaces.TryGetValue(uiTemplate.Name, out var _)) return;

        var created = _factory.Create(typeof(UserInterface), uiTemplate);

        if (created is UserInterface ui)
        {
            ui.Configure(uiTemplate);
            _userInterfaces[uiTemplate.Name] = ui;
        }
    }

    /// <inheritdoc/>
    public bool Activate(string name)
    {
        if (!_userInterfaces.TryGetValue(name, out var userInterface))
        {
            _logger.LogWarning("User Interface '{Name}' not found", name);
            return false;
        }

        // Deactivate current UI
        if (_active != null)
        {
            _active.Deactivate();
            _logger.LogDebug("Deactivated User Interface");
        }

        // Activate new UI
        _active = userInterface;
        _active.Activate();

        // Set the active UI as the renderer's root component for rendering
        _renderer.RootComponent = _active;

        _logger.LogDebug("Activated User Interface '{Name}' and set as renderer root component", name);

        return true;
    }

    /// <inheritdoc/>
    public void Update(double deltaTime)
    {
        if (_active == null)
        {
            throw new InvalidOperationException("No active user interface is available for update.");
        }

        _active.Update(deltaTime);
    }

    /// <inheritdoc/>
    public bool TryGet(string name, out IUserInterface? ui)
    {
        bool found = _userInterfaces.TryGetValue(name, out var result);
        ui = found ? result : null;
        return found;
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        // Dispose all managed UI instances
        foreach (var ui in _userInterfaces.Values)
        {
            ui.Dispose();
        }

        _userInterfaces.Clear();
        _active = null;
        _disposed = true;
    }
}