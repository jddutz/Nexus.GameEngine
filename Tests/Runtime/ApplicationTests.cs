using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Runtime;

namespace Tests.Runtime;

/// <summary>
/// Tests for the main Application class to ensure it can start with default configuration
/// </summary>
public class ApplicationTests
{
    /// <summary>
    /// Tests that the application can be created and configured with default settings
    /// without requiring platform-specific overrides from Prelude or Tactics
    /// </summary>
    [Fact]
    public void Application_ShouldStart_UsingDefaultConfig()
    {
        // Arrange: Create a minimal configuration similar to what desktop app uses
        var configuration = CreateDefaultConfiguration();
        var loggingConfig = CreateDefaultLoggingConfiguration();

        // Act & Assert: Verify that the service collection can be built without throwing exceptions
        var exception = Record.Exception(() =>
        {
            var services = new ServiceCollection()
                .AddRuntimeServices(configuration, loggingConfig)
                .BuildServiceProvider();

            // Verify that all critical services can be resolved
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<IApplication>();
            var windowService = scope.ServiceProvider.GetRequiredService<IWindowService>();
            var renderer = scope.ServiceProvider.GetRequiredService<IRenderer>();

            // Verify these are not null
            Assert.NotNull(app);
            Assert.NotNull(windowService);
            Assert.NotNull(renderer);
        });

        // Assert: No exceptions should be thrown during service registration and resolution
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that the logging system is properly configured and ILogger&lt;T&gt; can be resolved
    /// </summary>
    [Fact]
    public void Application_ShouldResolveTypedLoggers_UsingDefaultConfig()
    {
        // Arrange
        var configuration = CreateDefaultConfiguration();
        var loggingConfig = CreateDefaultLoggingConfiguration();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var services = new ServiceCollection()
                .AddRuntimeServices(configuration, loggingConfig)
                .BuildServiceProvider();

            using var scope = services.CreateScope();

            // Verify that typed loggers can be resolved (this was the original DI issue)
            var typedLogger = scope.ServiceProvider.GetRequiredService<ILogger<IApplication>>();
            var typedLoggerRenderer = scope.ServiceProvider.GetRequiredService<ILogger<IRenderer>>();

            Assert.NotNull(typedLogger);
            Assert.NotNull(typedLoggerRenderer);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that the component factory is properly registered and can create components
    /// </summary>
    [Fact]
    public void Application_ShouldResolveComponentFactory_UsingDefaultConfig()
    {
        // Arrange
        var configuration = CreateDefaultConfiguration();
        var loggingConfig = CreateDefaultLoggingConfiguration();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var services = new ServiceCollection()
                .AddRuntimeServices(configuration, loggingConfig)
                .BuildServiceProvider();

            using var scope = services.CreateScope();

            // Verify that IComponentFactory can be resolved (this was the second DI issue)
            var componentFactory = scope.ServiceProvider.GetRequiredService<IComponentFactory>();

            Assert.NotNull(componentFactory);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Creates a minimal configuration similar to what the desktop application uses
    /// </summary>
    private static IConfiguration CreateDefaultConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
            ["ConsoleLogging:MinimumLevel"] = "Debug",
            ["ConsoleLogging:ShowTimestamp"] = "true",
            ["ConsoleLogging:ShowContext"] = "true",
            ["ConsoleLogging:UseColors"] = "true",
            ["Game:ApplicationName"] = "NexusRealms - Test",
            ["Game:Version"] = "1.0.0-test",
            ["Game:UserDataPath"] = "%TEMP%\\NexusRealms\\Test"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    /// <summary>
    /// Creates a default logging configuration for testing
    /// </summary>
    private static LoggingConfiguration CreateDefaultLoggingConfiguration()
    {
        return new LoggingConfiguration
        {
            MinimumLevel = LogLevel.Debug,
            ShowTimestamp = true,
            ShowContext = true,
            UseColors = false // Disable colors for testing
        };
    }

    /// <summary>
    /// Creates a default logger instance using the same pattern as the desktop application
    /// </summary>
    private static ILogger CreateDefaultLogger()
    {
        var loggingConfig = CreateDefaultLoggingConfiguration();
        return new ConsoleLogger("Nexus.GameEngine.Tests", loggingConfig);
    }
}