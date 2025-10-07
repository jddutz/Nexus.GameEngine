using System.Reflection;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Represents a shader source that can be loaded from various sources (embedded resources, files, inline strings).
/// Uses a lazy-loading lambda pattern - the actual source is loaded only when needed.
/// </summary>
public class ShaderSource : IShaderSource
{
    private readonly Func<string> _loader;

    private ShaderSource(Func<string> loader)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    /// <summary>
    /// Creates a ShaderSource that loads from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource file (e.g., "basic-quad-vert.glsl")</param>
    /// <param name="assembly">Optional assembly to load from. If null, uses the GameEngine assembly.</param>
    /// <returns>A ShaderSource descriptor</returns>
    public static ShaderSource FromEmbeddedResource(string resourceName, Assembly? assembly = null)
    {
        return new ShaderSource(() => LoadEmbeddedResource(resourceName, assembly));
    }

    /// <summary>
    /// Creates a ShaderSource that loads from a file path.
    /// </summary>
    /// <param name="filePath">The file path to load from</param>
    /// <returns>A ShaderSource descriptor</returns>
    public static ShaderSource FromFile(string filePath)
    {
        return new ShaderSource(() => LoadFile(filePath));
    }

    /// <summary>
    /// Creates a ShaderSource with inline GLSL source code.
    /// </summary>
    /// <param name="sourceCode">The GLSL source code</param>
    /// <returns>A ShaderSource descriptor</returns>
    public static ShaderSource FromString(string sourceCode)
    {
        return new ShaderSource(() => sourceCode);
    }

    /// <summary>
    /// Loads the actual shader source code by calling the loader lambda.
    /// This is called by the ResourceManager when it needs the actual source.
    /// </summary>
    /// <returns>The shader source code as a string</returns>
    public string Load()
    {
        return _loader();
    }

    private static string LoadEmbeddedResource(string resourceName, Assembly? assembly)
    {
        var targetAssembly = assembly ?? typeof(ShaderSource).Assembly;
        var fullResourceName = $"Nexus.GameEngine.Resources.Shaders.{resourceName}";

        using var stream = targetAssembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            // List available resources for debugging
            var availableResources = targetAssembly.GetManifestResourceNames();
            var resourceList = string.Join(", ", availableResources);
            throw new FileNotFoundException(
                $"Embedded shader resource '{fullResourceName}' not found. Available resources: {resourceList}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Shader file not found: {filePath}");
        }

        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// Implicit conversion from string to ShaderSource (treats as inline source)
    /// </summary>
    public static implicit operator ShaderSource(string sourceCode) => FromString(sourceCode);

    /// <summary>
    /// Implicit conversion from ShaderSource to string (loads the source)
    /// </summary>
    public static implicit operator string(ShaderSource shaderSource) => shaderSource.Load();
}
