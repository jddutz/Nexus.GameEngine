using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Settings;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Nexus.IDE;

class Program
{
    private const string APPLICATION_NAME = "Nexus IDE";

    private static void Main(string[] args)
    {
        try
        {
            // Determine environment (DOTNET_ENVIRONMENT if set, otherwise Production)
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            // Load configuration from files, environment variables, and command line only.
            // The GameEngine provides runtime defaults; avoid injecting defaults here to prevent surprises.
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

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
                .AddPixelSampling()
                .AddDiscoveredServices<IComponent>()
                .AddDiscoveredServices<IAction>()
                .BuildServiceProvider();

            var application = new Application(services);

            // Allow overriding the window title from configuration
            var windowTitle = configuration["Application:General:ApplicationName"] ?? APPLICATION_NAME;

            var windowOptions = WindowOptions.DefaultVulkan with
            {
                Size = new Vector2D<int>(1280, 720),
                Title = windowTitle,
                WindowBorder = WindowBorder.Resizable,
                WindowState = WindowState.Normal,
                VSync = true
            };

            // Use the NexusIDE template defined in this project
            application.Run(windowOptions, Templates.NexusIDE);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Program Error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
