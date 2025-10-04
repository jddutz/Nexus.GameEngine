using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private static void Main(string[] args)
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
            .AddRuntimeServices(configuration, loggingConfig)
            .AddDefaultRenderer()
            .AddRuntimeComponents(Assembly.GetExecutingAssembly())
            .BuildServiceProvider();

        // Setup Application
        var application = new Application(services)
        {
            StartupTemplate = Templates.MainMenu
        };

        // Run
        application.Run();
    }
}