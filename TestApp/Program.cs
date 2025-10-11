using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Settings;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp;

/// <summary>
/// Entry point for the TestApp integration test runner.
/// Configures services, logging, and executes integration tests using the Nexus Game Engine.
/// </summary>
class Program
{
    private const string APPLICATION_NAME = "Nexus Game Engine Test App";

    /// <summary>
    /// Main entry point for the application. Configures services and runs integration tests.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static void Main(string[] args)
    {
        // Create configuration with application settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:General:ApplicationName"] = APPLICATION_NAME,
                ["Application:General:ApplicationVersion"] = "1.0.0",
                ["Application:General:EngineName"] = "Nexus Game Engine",
                ["Application:General:EngineVersion"] = "1.0.0",
                ["Graphics:Vulkan:ValidationEnabled"] = "true"
            })
            .Build();

        // Build service container
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);

                var loggerConfig = new TestLoggerConfiguration();
                var loggerFactory = new TestLoggerFactory(loggerConfig);
                builder.AddProvider(new TestLoggerFactory(loggerConfig));
            })
            .Configure<ApplicationSettings>(configuration.GetSection("Application"))
            .Configure<GraphicsSettings>(configuration.GetSection("Graphics"))
            .Configure<VkSettings>(configuration.GetSection("Graphics:Vulkan"))
            .AddSingleton<IWindowService, WindowService>()
            .AddSingleton<IVkValidationLayers, VkValidationLayers>()
            .AddSingleton<IVkContext, VkContext>()
            .AddSingleton<IRenderer, Renderer>()
            .AddSingleton<IEventBus, EventBus>()
            .AddSingleton<IAssetService, AssetService>()
            .AddSingleton<IResourceManager, ResourceManager>()
            .AddSingleton<IContentManager, ContentManager>()
            .AddSingleton<IComponentFactory, ComponentFactory>()
            .AddSingleton<IActionFactory, ActionFactory>()
            .AddDiscoveredServices<IRuntimeComponent>()
            .AddDiscoveredServices<IAction>()
            .BuildServiceProvider();

        // Get Application from DI
        var application = new Application(services);

        // Create window options for the application
        var windowOptions = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(1920, 1080),
            Title = APPLICATION_NAME,
            WindowBorder = WindowBorder.Hidden,
            WindowState = WindowState.Fullscreen,
            VSync = true
        };

        // Run
        application.Run(windowOptions, Templates.MainMenu);
    }
}