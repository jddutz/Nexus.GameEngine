using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine.Runtime.Systems;

namespace Nexus.GameEngine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineSystems(this IServiceCollection services)
    {
        services.AddSingleton<IResourceSystem, ResourceSystem>();
        services.AddSingleton<IGraphicsSystem, GraphicsSystem>();
        services.AddSingleton<IContentSystem, ContentSystem>();
        services.AddSingleton<IWindowSystem, WindowSystem>();
        services.AddSingleton<IInputSystem, InputSystem>();

        return services;
    }
}
