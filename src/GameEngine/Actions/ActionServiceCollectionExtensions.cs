using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nexus.GameEngine.Actions;

/// <summary>
/// Extension methods for service collection to register action system services.
/// </summary>
public static class ActionServiceCollectionExtensions
{
    /// <summary>
    /// Adds the complete action system to the service collection.
    /// This includes action factory, action discovery, and automatic registration of all IAction implementations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddActionSystem(this IServiceCollection services)
    {
        // Register core action services
        services.TryAddSingleton<IActionFactory, ActionFactory>();

        // Auto-discover and register all action implementations from current assembly
        services.AddAllActions();

        return services;
    }

    /// <summary>
    /// Adds the complete action system with custom assemblies.
    /// This includes action factory, action discovery, and automatic registration of all IAction implementations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Additional assemblies to scan for actions</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddActionSystem(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register core action services
        services.TryAddSingleton<IActionFactory, ActionFactory>();

        // Auto-discover and register all action implementations
        services.AddAllActions(assemblies);

        return services;
    }

    /// <summary>
    /// Registers a specific action type in the service collection.
    /// </summary>
    /// <typeparam name="TAction">The action type that implements IAction</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAction<TAction>(this IServiceCollection services)
        where TAction : class, IAction
    {
        services.AddTransient<TAction>();
        return services;
    }

    /// <summary>
    /// Automatically discovers and registers all IAction implementations from the specified assemblies.
    /// If no assemblies are provided, scans the calling assembly and the GameEngine assembly.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for action implementations</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAllActions(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies?.Length > 0
            ? assemblies
            : [Assembly.GetCallingAssembly(), typeof(IAction).Assembly];

        foreach (var assembly in assembliesToScan)
        {
            services.AddActionsFromAssembly(assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers all IAction implementations from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">Assembly to scan for action implementations</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddActionsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var actionTypes = assembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(IAction).IsAssignableFrom(t))
            .ToList();

        foreach (var actionType in actionTypes)
        {
            // Register as transient - each action execution gets a fresh instance
            services.AddTransient(actionType);
        }

        return services;
    }
}