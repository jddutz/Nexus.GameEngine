using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine;
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
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Textures;
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
    /// <param name="args">Command-line arguments. Supports --filter=<pattern> to filter tests by name.</param>
    private static void Main(string[] args)
    {
        // Default to -1 (premature exit) until TestRunner completes successfully
        Environment.ExitCode = -1;
        
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
#if DEBUG
                    ["Graphics:Vulkan:EnableValidationLayers"] = "true",   // Enable in Debug for error detection
#else
                    ["Graphics:Vulkan:EnableValidationLayers"] = "false",  // Disable in Release for performance
#endif
                    ["Graphics:Vulkan:EnableSwapchainTransfer"] = "true"  // Enable for pixel sampling tests
                })
                .Build();

            // Build service container
            var services = new ServiceCollection()
                .Configure<ApplicationSettings>(configuration.GetSection("Application"))
                .Configure<GraphicsSettings>(configuration.GetSection("Graphics"))
                .Configure<VulkanSettings>(configuration.GetSection("Graphics:Vulkan"))
                .AddSingleton<IWindowService, WindowService>()
                .AddSingleton<IVkValidation, VkValidation>()
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
                .AddSingleton<ITextureResourceManager, TextureResourceManager>()
                .AddSingleton<IFontResourceManager, FontResourceManager>()
                .AddSingleton<IResourceManager, ResourceManager>()
                .AddSingleton<IComponentFactory, ComponentFactory>()
                .AddSingleton<IContentManager, ContentManager>()
                .AddSingleton<IActionFactory, ActionFactory>()
                .AddSingleton<IProfiler, Profiler>()
                .AddGameEngineSystems()
                .AddPixelSampling()
                .AddDiscoveredServices<IComponent>()
                .AddDiscoveredServices<IAction>()
                .BuildServiceProvider();

            // Get Application from DI
            var application = new Application(services);

            // Create window options for the application
            var windowOptions = WindowOptions.DefaultVulkan with
            {
                Size = new Vector2D<int>(1280, 720),
                Title = APPLICATION_NAME,
                WindowBorder = WindowBorder.Resizable,
                WindowState = WindowState.Normal,
                VSync = true
            };
            
            application.Run(windowOptions, Templates.Tests);
            
            if (Array.IndexOf(args, "--benchmark") >= 0)
            {
                new Performance.SystemsBenchmark().Run();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Program Error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
