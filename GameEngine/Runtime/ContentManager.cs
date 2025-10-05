using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

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
    private readonly Dictionary<string, IRuntimeComponent> content = [];
    private bool _disposed;

    /// <summary>
    /// Gets or sets the main <see cref="IViewport"/> used for rendering.
    /// </summary>
    public IViewport Viewport { get; init; }
        = componentFactory.Create<Viewport>() as IViewport
        ?? throw new InvalidOperationException("Unable to create Viewport");

    /// <inheritdoc/>
    public IRuntimeComponent? TryGet(string name)
    {
        return content.TryGetValue(name, out var component) ? component : null;
    }

    /// <inheritdoc/>
    public IRuntimeComponent? Create(IComponentTemplate template, string id = "", bool activate = true)
    {
        if (string.IsNullOrEmpty(template.Name))
        {
            _logger.LogWarning("Cannot get or create content with empty name");
            return null;
        }

        // Try to get existing content first
        if (content.TryGetValue(template.Name, out var existingComponent))
        {
            return existingComponent;
        }

        // Content doesn't exist, create it
        try
        {
            var created = _factory.CreateInstance(template);
            if (created != null)
            {
                content[template.Name] = created;

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

    public IRuntimeComponent? GetOrCreate(IComponentTemplate template, string id, bool activate = true)
    {
        if (content.TryGetValue(id, out var component))
            return component;

        return Create(template, id);
    }

    public void OnUpdate(double deltaTime)
    {
        Viewport?.Update(deltaTime);

        var activeComponents = content.Values
            .OfType<IRuntimeComponent>()
            .Where(c => c.IsActive);

        foreach (var component in activeComponents)
            component.Update(deltaTime);
    }

    /// <inheritdoc/>
    public bool Remove(string name)
    {
        if (content.TryGetValue(name, out var component))
        {
            content.Remove(name);
            component.Dispose();
            _logger.LogDebug("Removed content '{Name}'", name);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Dispose all managed content instances
        foreach (var component in content.Values)
        {
            component.Dispose();
        }

        content.Clear();

        Viewport.Dispose();

        _disposed = true;
        _logger.LogDebug("ContentManager disposed");
    }
}