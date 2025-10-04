using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Runtime;

namespace TestApp;

/// <summary>
/// Entry point for the TestApp integration test runner.
/// Configures services, logging, and executes integration tests using the Nexus Game Engine.
/// </summary>
class Program
{
    /// <summary>
    /// Main entry point for the application. Configures services and runs integration tests.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static async Task Main(string[] args)
    {
        // Create basic configuration
        var configuration = new ConfigurationBuilder()
            .Build();

        // Create logging configuration
        var loggingConfig = new LoggingConfiguration
        {
            MinimumLevel = LogLevel.Debug // Enable debug logging for integration tests
        };

        // Build service container
        var services = new ServiceCollection()
            .AddGameEngineServices(configuration, loggingConfig)
            .AddTransient<IBatchStrategy, DefaultBatchStrategy>()
            .AddTransient<TestRunner>()
            .AddServicesOfType<IRuntimeComponent>(Assembly.GetExecutingAssembly())
            .BuildServiceProvider();

        // Register testing middleware with renderer
        var renderer = services.GetRequiredService<IRenderer>();

        // TestApp's purpose is to run integration tests
        await RunIntegrationTestsAsync(services);
    }

    /// <summary>
    /// Runs integration tests by initializing the application and executing the test runner.
    /// </summary>
    /// <param name="services">The service provider containing application dependencies.</param>
    private static async Task RunIntegrationTestsAsync(IServiceProvider services)
    {
        Console.WriteLine("=== NEXUS GAME ENGINE INTEGRATION TEST RUNNER ===");
        Console.WriteLine();

        // Initialize the application with MainMenu template (which includes TestRunner)
        var app = services.GetRequiredService<IApplication>();
        app.StartupTemplate = Templates.MainMenu;

        // Run the application - TestRunner will handle all test execution and quit when done
        await app.RunAsync();
    }
}