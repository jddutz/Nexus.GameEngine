using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TestApp;


/// <summary>
/// Logger provider that creates instances of our custom TestLogger for Microsoft's logging framework.
/// This enables support for ILogger&lt;T&gt; typed loggers while using our custom logging implementation.
/// Each category gets its own TestLogger instance with context-specific naming.
/// </summary>
/// <param name="configuration">Configuration to use for all created loggers</param>
public class TestLoggerFactory(TestLoggerConfiguration config) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();

    /// <summary>
    /// Creates a logger for the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger (typically the full type name)</param>
    /// <returns>A TestLogger instance with the specified context</returns>
    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new TestLogger(categoryName, config));

    public TestLogger GetOrCreateLogger<T>()
    {
        var categoryName = typeof(T).Name;

        _loggers.TryGetValue(categoryName, out ILogger? logger);

        if (logger is TestLogger exTestLogger) return exTestLogger;

        logger = CreateLogger(categoryName) as TestLogger;

        if (logger is TestLogger newTestLogger) return newTestLogger;

        throw new InvalidOperationException($"Unable to create logger for type {categoryName}");
    }

    /// <summary>
    /// Disposes the logger provider and its resources.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _loggers.Clear();
    }
}