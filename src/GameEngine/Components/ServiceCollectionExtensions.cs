using Microsoft.Extensions.DependencyInjection;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Extension methods for registering game engine components in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the component factory and required game engine services.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddComponentFactory(this IServiceCollection services)
    {
        services.AddSingleton<IComponentFactory, ComponentFactory>();
        return services;
    }
}
