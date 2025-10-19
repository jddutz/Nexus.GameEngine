using Microsoft.Extensions.DependencyInjection;

namespace Nexus.GameEngine.Testing;

/// <summary>
/// Extension methods for registering testing services.
/// </summary>
public static class TestingServiceExtensions
{
    /// <summary>
    /// Adds pixel sampling service for testing.
    /// WARNING: This service impacts performance and should only be used in test/debug builds.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPixelSampling(this IServiceCollection services)
    {
        services.AddTransient<IPixelSampler, VulkanPixelSampler>();
        return services;
    }
}
