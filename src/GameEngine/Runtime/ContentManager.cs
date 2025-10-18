using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Provides efficient creation, caching, and disposal of component hierarchies.
/// </summary>
public class ContentManager(
    ILoggerFactory loggerFactory,
    IComponentFactory componentFactory,
    IOptions<GraphicsSettings> graphicsSettings)
    : IContentManager
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(ContentManager));
    private readonly IComponentFactory _factory = componentFactory;
    private readonly IOptions<GraphicsSettings> _graphicsSettings = graphicsSettings;
    private readonly Dictionary<string, IRuntimeComponent> content = [];
    private bool _disposed;

    /// <summary>
    /// Gets or sets the main <see cref="IViewport"/> used for rendering.
    /// </summary>
    public IViewport Viewport { get; private set; } = null!;

    /// <inheritdoc/>
    public void Load(IComponentTemplate template)
    {
        _logger.LogDebug("Loading content from template: {TemplateName}", template.Name);

        // Check if template is Viewport.Template
        if (template is Viewport.Template viewportTemplate)
        {
            _logger.LogDebug("Template is Viewport.Template, creating viewport from template");
            // Create viewport from the provided template (may include Content)
            Viewport = CreateViewport(viewportTemplate);
        }
        else
        {
            _logger.LogDebug("Template is not Viewport.Template, creating default viewport and content");
            // Create default viewport from GraphicsSettings
            Viewport = CreateViewport(null);
            
            // Create content from the template
            var createdContent = Create(template);
            
            if (createdContent != null)
            {
                // Assign content to viewport
                Viewport.Content = createdContent;
                _logger.LogDebug("Assigned content '{ContentName}' to viewport", createdContent.Name);
            }
            else
            {
                _logger.LogWarning("Failed to create content from template '{TemplateName}'", template.Name);
            }
        }
    }

    /// <summary>
    /// Creates and activates a viewport from the provided template or GraphicsSettings.
    /// Disposes the existing viewport if it exists.
    /// </summary>
    /// <param name="template">Optional viewport template. If null, creates a default viewport.</param>
    /// <returns>The created and activated viewport.</returns>
    private IViewport CreateViewport(Viewport.Template? template)
    {
        // Dispose existing viewport if it exists
        if (Viewport != null)
        {
            _logger.LogDebug("Disposing existing viewport");
            (Viewport as IRuntimeComponent)?.Dispose();
        }

        IViewport viewport;

        if (template != null)
        {
            _logger.LogDebug("Creating viewport from template");
            // Create from template using ComponentFactory
            var component = _factory.CreateInstance(template);
            viewport = component as IViewport 
                ?? throw new InvalidOperationException("Failed to create Viewport from template");
        }
        else
        {
            _logger.LogDebug("Creating default viewport from GraphicsSettings");
            // Create default viewport
            viewport = _factory.Create<Viewport>() as IViewport
                ?? throw new InvalidOperationException("Failed to create default Viewport");
            
            // Configure with null template to trigger OnConfigure lifecycle
            // This ensures component initializes with GraphicsSettings background color
            (viewport as IRuntimeComponent)?.Configure(null);
        }

        // Activate the viewport
        _logger.LogDebug("Activating viewport");
        (viewport as IRuntimeComponent)?.Activate();

        return viewport;
    }

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

        // Dispose viewport if it was created
        if (Viewport != null)
        {
            (Viewport as IRuntimeComponent)?.Dispose();
        }

        _disposed = true;
        _logger.LogDebug("ContentManager disposed");
    }
}