using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Default implementation of IComponentFactory. Responsible for creating and configuring runtime components
/// using dependency injection and component templates.
/// </summary>
public class ComponentFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    : IComponentFactory
{
    private readonly ILogger logger = loggerFactory.CreateLogger<ComponentFactory>();

    /// <summary>
    /// Creates a component instance of the specified type using the service provider.
    /// </summary>
    /// <param name="componentType">The type of the component to create.</param>
    /// <returns>The created component instance, or null if not found.</returns>
    public IRuntimeComponent? Create(Type componentType)
    {
        if (componentType == null) return null;

        return serviceProvider.GetService(componentType) as IRuntimeComponent;
    }

    /// <summary>
    /// Creates a component instance of the specified generic type using the service provider.
    /// </summary>
    /// <typeparam name="T">The type of the component to create (must implement IRuntimeComponent).</typeparam>
    /// <returns>The created component instance, or null if not found.</returns>
    public IRuntimeComponent? Create<T>()
        where T : IRuntimeComponent
    {
        return serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Creates and configures a component instance of the specified type using the provided template.
    /// </summary>
    /// <param name="componentType">The type of the component to create.</param>
    /// <param name="template">The template to configure the component.</param>
    /// <returns>The configured component instance, or null if not found.</returns>
    public IRuntimeComponent? Create(Type componentType, IComponentTemplate template)
    {
        var instance = Create(componentType);

        if (instance is IRuntimeComponent component)
        {
            component.ComponentFactory = this;
            component.Logger = loggerFactory.CreateLogger(componentType.Name);

            component.Configure(template);
            return component;
        }

        return null;
    }

    /// <summary>
    /// Creates and configures a component instance of the specified generic type using the provided template.
    /// </summary>
    /// <typeparam name="T">The type of the component to create (must implement IRuntimeComponent).</typeparam>
    /// <param name="template">The template to configure the component.</param>
    /// <returns>The configured component instance, or null if not found.</returns>
    public IRuntimeComponent? Create<T>(IComponentTemplate template)
        where T : IRuntimeComponent
    {
        var component = Create<T>();

        if (component != null)
        {
            component.ComponentFactory = this;
            component.Logger = loggerFactory.CreateLogger(typeof(T).Name);

            component.Configure(template);
        }

        return component;
    }

    /// <summary>
    /// Creates and configures a component instance by inferring the component type from the template's containing class.
    /// </summary>
    /// <param name="template">The template to use for component creation and configuration.</param>
    /// <returns>The configured component instance, or null if the type cannot be determined or does not implement IRuntimeComponent.</returns>
    public IRuntimeComponent? Instantiate(IComponentTemplate template)
    {
        if (template == null)
        {
            logger.LogDebug("[ComponentFactory] Instantiate called with null template");
            return null;
        }

        logger.LogDebug("Instantiate called for template: {TemplateTypeName} with name '{TemplateName}'", template.GetType().Name, template.Name);
        logger.LogDebug("Template has {Count} subcomponents", template.Subcomponents?.Length ?? 0);

        // Try to get the component type from the template's containing class
        var componentType = template.GetType().DeclaringType;

        logger.LogDebug("Inferred component type: {TypeName}", componentType?.Name ?? "null");

        // Type-safety: Ensure the type implements IRuntimeComponent
        if (componentType == null || !typeof(IRuntimeComponent).IsAssignableFrom(componentType))
        {
            logger.LogDebug("Component type is null or doesn't implement IRuntimeComponent");
            return null;
        }

        logger.LogDebug("Creating component of type: {TypeName}", componentType.Name);

        // Create and configure the component
        var result = Create(componentType, template);

        logger.LogDebug("Component creation result: {Result}", result != null ? "SUCCESS" : "FAILED");

        return result;
    }
}