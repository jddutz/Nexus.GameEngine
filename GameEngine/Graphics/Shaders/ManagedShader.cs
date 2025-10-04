using Silk.NET.OpenGL;
using System.Collections.ObjectModel;

namespace Nexus.GameEngine.Graphics.Shaders;

/// <summary>
/// Represents a compiled shader object
/// </summary>
public class ManagedShader(string name, uint shaderId, ShaderType type, string source,
    bool isCompiled, string? compilationLog = null)
{
    /// <summary>
    /// Gets the shader name
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets the OpenGL shader ID
    /// </summary>
    public uint ShaderId { get; } = shaderId;

    /// <summary>
    /// Gets the shader type
    /// </summary>
    public ShaderType Type { get; } = type;

    /// <summary>
    /// Gets the shader source code
    /// </summary>
    public string Source { get; } = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>
    /// Gets whether the shader was compiled successfully
    /// </summary>
    public bool IsCompiled { get; } = isCompiled;

    /// <summary>
    /// Gets the compilation error log (if any)
    /// </summary>
    public string? CompilationLog { get; } = compilationLog;

    /// <summary>
    /// Gets when the shader was created
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when the shader was last accessed
    /// </summary>
    public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Updates the last accessed time
    /// </summary>
    public void OnAccess()
    {
        LastAccessed = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a linked shader program
/// </summary>
public class ManagedShaderProgram(string name, uint programId, IList<ManagedShader> attachedShaders,
    bool isLinked, string? linkingLog = null)
{
    /// <summary>
    /// Gets the program name
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets the OpenGL program ID
    /// </summary>
    public uint ProgramId { get; } = programId;

    /// <summary>
    /// Gets the attached shaders
    /// </summary>
    public ReadOnlyCollection<ManagedShader> AttachedShaders { get; } = new ReadOnlyCollection<ManagedShader>(attachedShaders ?? throw new ArgumentNullException(nameof(attachedShaders)));

    /// <summary>
    /// Gets whether the program was linked successfully
    /// </summary>
    public bool IsLinked { get; } = isLinked;

    /// <summary>
    /// Gets the linking error log (if any)
    /// </summary>
    public string? LinkingLog { get; } = linkingLog;

    /// <summary>
    /// Gets when the program was created
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when the program was last accessed
    /// </summary>
    public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets uniform locations cache
    /// </summary>
    public Dictionary<string, int> UniformLocations { get; } = [];

    /// <summary>
    /// Updates the last accessed time
    /// </summary>
    public void OnAccess()
    {
        LastAccessed = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a uniform location, using cache for performance
    /// </summary>
    public int GetUniformLocation(string name, GL gl)
    {
        if (UniformLocations.TryGetValue(name, out var location))
            return location;

        location = gl.GetUniformLocation(ProgramId, name);
        UniformLocations[name] = location;
        return location;
    }
}