using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Provides efficient creation, caching, and disposal of component hierarchies.
/// </summary>
public class ContentManager(
    ILoggerFactory loggerFactory,
    IComponentFactory componentFactory)
    : IContentManager
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(ContentManager));
    private readonly IComponentFactory _factory = componentFactory;
    private readonly Dictionary<string, IRuntimeComponent> _content = [];
    private bool _disposed;

    /// <inheritdoc/>
    public IRuntimeComponent? TryGet(string name)
    {
        return _content.TryGetValue(name, out var component) ? component : null;
    }

    /// <inheritdoc/>
    public IRuntimeComponent? GetOrCreate(IComponentTemplate template, bool activate = true)
    {
        if (string.IsNullOrEmpty(template.Name))
        {
            _logger.LogWarning("Cannot get or create content with empty name");
            return null;
        }

        // Try to get existing content first
        if (_content.TryGetValue(template.Name, out var existingComponent))
        {
            return existingComponent;
        }

        // Content doesn't exist, create it
        try
        {
            var created = _factory.Instantiate(template);
            if (created != null)
            {
                _content[template.Name] = created;

                if (activate) created.Activate();

                return created;
            }
            else
            {
                _logger.LogError("Failed to create content from template '{Name}'", template.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create content from template '{Name}'", template.Name);
            return null;
        }
    }

    /// <inheritdoc/>
    public bool Remove(string name)
    {
        if (_content.TryGetValue(name, out var component))
        {
            _content.Remove(name);
            component.Dispose();
            _logger.LogDebug("Removed content '{Name}'", name);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<IRuntimeComponent> GetContent()
    {
        if (_disposed) yield break;

        foreach (var component in _content.Values)
        {
            yield return component;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Dispose all managed content instances
        foreach (var component in _content.Values)
        {
            component.Dispose();
        }

        _content.Clear();
        _disposed = true;
        _logger.LogDebug("ContentManager disposed");
    }
}