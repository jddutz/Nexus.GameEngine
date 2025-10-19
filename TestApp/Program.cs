using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Settings;
using Nexus.GameEngine.Testing;
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
        try
        {
            // Create configuration with application settings
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Application:General:ApplicationName"] = APPLICATION_NAME,
                    ["Application:General:ApplicationVersion"] = "1.0.0",
                    ["Application:General:EngineName"] = "Nexus Game Engine",
                    ["Application:General:EngineVersion"] = "1.0.0",
                    ["Graphics:Fullscreen"] = "false",
                    ["Graphics:Vulkan:EnableValidationLayers"] = "true",
                    ["Graphics:Vulkan:EnableSwapchainTransfer"] = "true"  // Enable for pixel sampling tests
                })
                .Build();

            // Configure logging
            var loggerConfig = new TestLoggerConfiguration();
            var testLoggerFactory = new TestLoggerFactory(loggerConfig);

            // Create a wrapper that implements ILoggerFactory and delegates to our TestLoggerFactory
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(testLoggerFactory);
            });

            // Build service container
            var services = new ServiceCollection()
                .AddSingleton(loggerFactory)
                .AddSingleton(testLoggerFactory)
                .Configure<ApplicationSettings>(configuration.GetSection("Application"))
                .Configure<GraphicsSettings>(configuration.GetSection("Graphics"))
                .Configure<VulkanSettings>(configuration.GetSection("Graphics:Vulkan"))
                .AddSingleton<IWindowService, WindowService>()
                .AddSingleton<IValidation, Validation>()
                .AddSingleton<IGraphicsContext, Context>()
                .AddSingleton<ISwapChain, SwapChain>()
                .AddSingleton<ISyncManager, SyncManager>()
                .AddSingleton<ICommandPoolManager, CommandPoolManager>()
                .AddSingleton<IDescriptorManager, DescriptorManager>()
                .AddSingleton<IPipelineManager, PipelineManager>()
                .AddSingleton<IBatchStrategy, DefaultBatchStrategy>()
                .AddSingleton<IRenderer, Renderer>()
                .AddSingleton<IEventBus, EventBus>()
                .AddSingleton<IAssetService, AssetService>()
                .AddSingleton<IBufferManager, BufferManager>()
                .AddSingleton<IGeometryResourceManager, GeometryResourceManager>()
                .AddSingleton<IShaderResourceManager, ShaderResourceManager>()
                .AddSingleton<IResourceManager, ResourceManager>()
                .AddSingleton<IContentManager, ContentManager>()
                .AddSingleton<IComponentFactory, ComponentFactory>()
                .AddSingleton<IActionFactory, ActionFactory>()
                .AddPixelSampling()
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
        catch (Exception ex)
        {
            Debug.WriteLine("Program Error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}