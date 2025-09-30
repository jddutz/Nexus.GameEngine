using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Resources;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime.Settings;
using Nexus.GameEngine.GUI.Abstractions;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Builds and manages the DI service container with Windows-specific services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the service collection with Windows-specific services.
    /// </summary>
    /// <param name="configuration">The configuration to use for service setup</param>
    /// <param name="loggingConfiguration">The logging configuration to use for creating loggers</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddGameEngineServices(
        this IServiceCollection services,
        IConfiguration configuration,
        LoggingConfiguration loggingConfiguration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton(loggingConfiguration);

        // Add Microsoft.Extensions.Logging services
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(loggingConfiguration.MinimumLevel);
            // Add our custom console provider that creates context-specific loggers
            builder.Services.AddSingleton<ILoggerProvider>(provider =>
                new ConsoleLoggerFactory(loggingConfiguration));
        });

        // Register application settings with defaults
        services.AddSingleton(provider =>
        {
            var settings = new ApplicationSettings();
            return Options.Create(settings);
        });

        // Register core runtime services
        services.AddSingleton<IComponentFactory, ComponentFactory>();
        services.AddSingleton<IWindowService, WindowService>();

        // Register renderer with window service dependency
        services.AddSingleton<IRenderer, Renderer>();
        services.AddSingleton<IContentManager, ContentManager>();
        services.AddSingleton<IResourceManager, ResourceManager>();

        // Temporary: Provide stub IAssetService until real implementation
        services.AddSingleton<IAssetService, StubAssetService>();

        services.AddSingleton<IGameStateManager, GameStateManager>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IApplication, Application>();

        // Register action system with automatic discovery
        services.AddActionSystem();
        services.AddServicesOfType<IRuntimeComponent>();

        return services;
    }

    /// <summary>
    /// Register all IAction implementations from the specified assembly
    /// </summary>
    public static IServiceCollection AddServicesOfType<T>(
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
    public static IServiceCollection AddServicesOfType<T>(
        this IServiceCollection services)
    {
        // Register actions from the assembly that defines the type
        services.AddServicesOfType<T>(typeof(T).Assembly);

        // Register actions from the calling assembly
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != typeof(T).Assembly)
        {
            services.AddServicesOfType<T>(callingAssembly);
        }

        return services;
    }
}
