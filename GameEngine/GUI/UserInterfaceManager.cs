using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
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
    private IRuntimeComponent? _active;
    private readonly Dictionary<string, IRuntimeComponent> _userInterfaces = [];

    /// <inheritdoc/>
    public IRuntimeComponent? Active => _active;

    /// <inheritdoc/>
    public void Create(IComponentTemplate template)
    {
        if (string.IsNullOrEmpty(template.Name))
        {
            _logger.LogWarning("Cannot create user interface with empty name");
            return;
        }

        if (_userInterfaces.TryGetValue(template.Name, out var _)) return;

        var created = _factory.Instantiate(template);

        if (created != null)
        {
            _userInterfaces[template.Name] = created;
            _logger.LogDebug("Created user interface component '{Name}'", template.Name);
        }
        else
        {
            _logger.LogWarning("Failed to create user interface component from template '{Name}'", template.Name);
        }
    }

    /// <inheritdoc/>
    public bool Activate(string name)
    {
        if (!_userInterfaces.TryGetValue(name, out var component))
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
        _active = component;
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
            _logger.LogDebug("No active user interface available for update - skipping UI update");
            return;
        }

        _active.Update(deltaTime);
    }

    /// <inheritdoc/>
    public bool TryGet(string name, out IRuntimeComponent? component)
    {
        bool found = _userInterfaces.TryGetValue(name, out var result);
        component = found ? result : null;
        return found;
    }

    /// <inheritdoc/>
    public IRuntimeComponent? GetOrCreate(IComponentTemplate template, bool activate = true)
    {
        if (string.IsNullOrEmpty(template.Name))
        {
            _logger.LogWarning("Cannot get or create user interface with empty name");
            return null;
        }

        // Try to get existing component first
        if (_userInterfaces.TryGetValue(template.Name, out var existingComponent))
        {
            if (activate)
            {
                try
                {
                    // Deactivate current UI
                    if (_active != null)
                    {
                        _active.Deactivate();
                        _logger.LogDebug("Deactivated previous UI component");
                    }

                    // Activate the existing component
                    _active = existingComponent;
                    _active.Activate();

                    // Set as renderer root component
                    _renderer.RootComponent = _active;

                    _logger.LogInformation("Activated UI component '{Name}'", template.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate UI component '{Name}'", template.Name);
                }
            }

            return existingComponent;
        }

        // Component doesn't exist, create it
        try
        {
            var created = _factory.Instantiate(template);

            if (created != null)
            {
                _userInterfaces[template.Name] = created;
                _logger.LogDebug("Created user interface component '{Name}'", template.Name);

                if (activate)
                {
                    try
                    {
                        // Deactivate current UI
                        if (_active != null)
                        {
                            _active.Deactivate();
                            _logger.LogDebug("Deactivated previous UI component");
                        }

                        // Activate the new component
                        _active = created;
                        _active.Activate();

                        // Set as renderer root component
                        _renderer.RootComponent = _active;

                        _logger.LogInformation("Activated UI component '{Name}'", template.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to activate UI component '{Name}'", template.Name);
                    }
                }

                return created;
            }
            else
            {
                _logger.LogError("Failed to create UI component from template '{Name}'", template.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create UI component from template '{Name}'", template.Name);
            return null;
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        // Dispose all managed UI instances
        foreach (var component in _userInterfaces.Values)
        {
            component.Dispose();
        }

        _userInterfaces.Clear();
        _active = null;
        _disposed = true;
    }
}