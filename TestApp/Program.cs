using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Runtime;
using NexusRealms.Prelude.Shared.UI;

namespace Program;

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

        // Get the user interface manager
        var userInterfaceManager = services.GetRequiredService<IUserInterfaceManager>();

        // Create and activate the main menu UI
        userInterfaceManager.Create(Templates.MainMenu);
        userInterfaceManager.Activate(Templates.MainMenu.Name);

        // Get and run the application
        var gameApplication = services.GetRequiredService<IApplication>();
        await gameApplication.RunAsync();
    }
}