using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Builds and manages the DI service container with Windows-specific services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all IAction implementations from the specified assembly
    /// </summary>
    public static IServiceCollection AddDiscoveredServices<T>(
        this IServiceCollection services,
        Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(T).IsAssignableFrom(t)
            );

        foreach (var type in types)
        {
            services.AddTransient(type);
        }

        return services;
    }

    /// <summary>
    /// Register all services of the specified type (typically an interface)
    /// from the calling assembly and the Engine.Actions assembly
    /// </summary>
    public static IServiceCollection AddDiscoveredServices<T>(
        this IServiceCollection services)
    {
        // Register actions from the assembly that defines the type
        services.AddDiscoveredServices<T>(typeof(T).Assembly);

        // Register actions from the calling assembly
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != typeof(T).Assembly)
        {
            services.AddDiscoveredServices<T>(callingAssembly);
        }

        return services;
    }
}
