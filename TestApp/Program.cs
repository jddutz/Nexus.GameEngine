using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Runtime;

namespace TestApp;

class Program
{
    private static async Task Main(string[] args)
    {
        // Create basic configuration
        var configuration = new ConfigurationBuilder()
            .Build();

        // Create logging configuration
        var loggingConfig = new LoggingConfiguration
        {
            MinimumLevel = LogLevel.Debug
        };

        // Build service container
        var services = new ServiceCollection()
            .AddGameEngineServices(configuration, loggingConfig)
            .BuildServiceProvider();

        // Get and run the application
        var app = services.GetRequiredService<IApplication>();
        app.StartupTemplate = Templates.MainMenu;

        await app.RunAsync();
    }
}