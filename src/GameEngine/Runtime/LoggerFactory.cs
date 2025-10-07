using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexus.GameEngine.Runtime;


/// <summary>
/// Logger provider that creates instances of our custom ConsoleLogger for Microsoft's logging framework.
/// This enables support for ILogger&lt;T&gt; typed loggers while using our custom logging implementation.
/// Each category gets its own ConsoleLogger instance with context-specific naming.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConsoleLoggerFactory.
/// </remarks>
/// <param name="configuration">The logging configuration to use for all created loggers</param>
public class ConsoleLoggerFactory(LoggingConfiguration configuration) : ILoggerProvider
{
    private readonly LoggingConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();

    /// <summary>
    /// Creates a logger for the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger (typically the full type name)</param>
    /// <returns>A ConsoleLogger instance with the specified context</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new ConsoleLogger(name, _configuration));
    }

    /// <summary>
    /// Disposes the logger provider and its resources.
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
    }
}