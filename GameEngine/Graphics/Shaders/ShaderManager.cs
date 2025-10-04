using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using Nexus.GameEngine.Resources;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nexus.GameEngine.Graphics.Shaders;

/// <summary>
/// Manages shader compilation, program linking, and uniform management
/// </summary>
public class ShaderManager : IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly ResourcePool _resourcePool;
    private readonly ConcurrentDictionary<string, ManagedShader> _shaderCache;
    private readonly ConcurrentDictionary<string, ManagedShaderProgram> _programCache;
    private readonly object _lockObject = new();

    private int _cacheHits;
    private int _cacheMisses;
    private long _totalCompilationTime;
    private long _totalLinkingTime;
    private ManagedShaderProgram? _currentProgram;
    private bool _disposed;

    /// <summary>
    /// Gets whether this manager has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the number of loaded shaders
    /// </summary>
    public int LoadedShaderCount => _shaderCache.Count;

    /// <summary>
    /// Gets the number of loaded programs
    /// </summary>
    public int LoadedProgramCount => _programCache.Count;

    /// <summary>
    /// Gets the currently active program
    /// </summary>
    public ManagedShaderProgram? CurrentProgram => _currentProgram;

    public ShaderManager(GL gl, ILogger logger, ResourcePool resourcePool)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourcePool = resourcePool ?? throw new ArgumentNullException(nameof(resourcePool));
        _shaderCache = new ConcurrentDictionary<string, ManagedShader>();
        _programCache = new ConcurrentDictionary<string, ManagedShaderProgram>();

        _logger.LogDebug("Created ShaderManager");
    }

    /// <summary>
    /// Loads and compiles a shader from source code
    /// </summary>
    /// <param name="name">Unique name for the shader</param>
    /// <param name="source">Shader source code</param>
    /// <param name="type">Shader type</param>
    /// <returns>Compiled shader</returns>
    public ManagedShader LoadShader(string name, string source, ShaderType type)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Shader name cannot be null or empty", nameof(name));

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("Shader source cannot be null or empty", nameof(source));

        // Check cache first
        if (_shaderCache.TryGetValue(name, out var cachedShader))
        {
            cachedShader.OnAccess();
            _cacheHits++;
            _logger.LogDebug("Cache hit for shader: {Name}", name);
            return cachedShader;
        }

        _cacheMisses++;

        // Compile new shader
        var shader = CompileShaderInternal(name, source, type);
        _shaderCache.TryAdd(name, shader);

        _logger.LogDebug("Loaded and compiled shader: {Name} ({Type})", name, type);

        return shader;
    }

    /// <summary>
    /// Reloads a shader with new source code (for hot reloading)
    /// </summary>
    /// <param name="name">Name of the shader to reload</param>
    /// <param name="newSource">New shader source code</param>
    /// <returns>Recompiled shader</returns>
    public ManagedShader ReloadShader(string name, string newSource)
    {
        ThrowIfDisposed();

        if (!_shaderCache.TryGetValue(name, out var existingShader))
            throw new ArgumentException($"Shader '{name}' not found", nameof(name));

        // Delete old shader
        _gl.DeleteShader(existingShader.ShaderId);

        // Compile new shader
        var newShader = CompileShaderInternal(name, newSource, existingShader.Type);
        _shaderCache[name] = newShader;

        _logger.LogDebug("Reloaded shader: {Name}", name);

        return newShader;
    }

    /// <summary>
    /// Creates and links a shader program from multiple shaders
    /// </summary>
    /// <param name="name">Unique name for the program</param>
    /// <param name="shaders">Shaders to attach to the program</param>
    /// <returns>Linked shader program</returns>
    public ManagedShaderProgram CreateProgram(string name, params ManagedShader[] shaders)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Program name cannot be null or empty", nameof(name));

        if (shaders == null || shaders.Length == 0)
            throw new ArgumentException("At least one shader must be provided", nameof(shaders));

        // Check cache first
        if (_programCache.TryGetValue(name, out var cachedProgram))
        {
            cachedProgram.OnAccess();
            _cacheHits++;
            _logger.LogDebug("Cache hit for program: {Name}", name);
            return cachedProgram;
        }

        _cacheMisses++;

        // Link new program
        var program = LinkProgramInternal(name, shaders);
        _programCache.TryAdd(name, program);

        _logger.LogDebug("Created and linked program: {Name} with {ShaderCount} shaders",
            name, shaders.Length);

        return program;
    }

    /// <summary>
    /// Gets a shader by name
    /// </summary>
    /// <param name="name">Name of the shader</param>
    /// <returns>Shader or null if not found</returns>
    public ManagedShader? GetShader(string name)
    {
        ThrowIfDisposed();

        if (_shaderCache.TryGetValue(name, out var shader))
        {
            shader.OnAccess();
            return shader;
        }

        return null;
    }

    /// <summary>
    /// Gets a program by name
    /// </summary>
    /// <param name="name">Name of the program</param>
    /// <returns>Program or null if not found</returns>
    public ManagedShaderProgram? GetProgram(string name)
    {
        ThrowIfDisposed();

        if (_programCache.TryGetValue(name, out var program))
        {
            program.OnAccess();
            return program;
        }

        return null;
    }

    /// <summary>
    /// Activates a shader program for rendering
    /// </summary>
    /// <param name="program">Program to activate</param>
    public void UseProgram(ManagedShaderProgram program)
    {
        ThrowIfDisposed();

        if (program == null)
            throw new ArgumentNullException(nameof(program));

        if (!program.IsLinked)
            throw new InvalidOperationException($"Program '{program.Name}' is not linked");

        _gl.UseProgram(program.ProgramId);
        _currentProgram = program;
        program.OnAccess();

        _logger.LogDebug("Activated program: {Name}", program.Name);
    }

    /// <summary>
    /// Sets a uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="value">Value to set</param>
    public void SetUniform(string name, float value)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.Uniform1(location, value);
        }
    }

    /// <summary>
    /// Sets a uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="value">Value to set</param>
    public void SetUniform(string name, int value)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.Uniform1(location, value);
        }
    }

    /// <summary>
    /// Sets a uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="x">X component</param>
    /// <param name="y">Y component</param>
    public void SetUniform(string name, float x, float y)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.Uniform2(location, x, y);
        }
    }

    /// <summary>
    /// Sets a uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="x">X component</param>
    /// <param name="y">Y component</param>
    /// <param name="z">Z component</param>
    public void SetUniform(string name, float x, float y, float z)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.Uniform3(location, x, y, z);
        }
    }

    /// <summary>
    /// Sets a uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="x">X component</param>
    /// <param name="y">Y component</param>
    /// <param name="z">Z component</param>
    /// <param name="w">W component</param>
    public void SetUniform(string name, float x, float y, float z, float w)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.Uniform4(location, x, y, z, w);
        }
    }

    /// <summary>
    /// Sets a matrix uniform value in the currently active program
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="matrix">Matrix data (16 floats for mat4)</param>
    /// <param name="transpose">Whether to transpose the matrix</param>
    public void SetUniformMatrix4(string name, ReadOnlySpan<float> matrix, bool transpose = false)
    {
        ThrowIfDisposed();

        if (_currentProgram == null)
            throw new InvalidOperationException("No program is currently active");

        if (matrix.Length != 16)
            throw new ArgumentException("Matrix must contain exactly 16 elements", nameof(matrix));

        var location = _currentProgram.GetUniformLocation(name, _gl);
        if (location != -1)
        {
            _gl.UniformMatrix4(location, transpose, matrix);
        }
    }

    /// <summary>
    /// Unloads a shader and removes it from cache
    /// </summary>
    /// <param name="name">Name of the shader to unload</param>
    /// <returns>True if shader was found and unloaded</returns>
    public bool UnloadShader(string name)
    {
        ThrowIfDisposed();

        if (_shaderCache.TryRemove(name, out var shader))
        {
            _gl.DeleteShader(shader.ShaderId);
            _logger.LogDebug("Unloaded shader: {Name}", name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unloads a program and removes it from cache
    /// </summary>
    /// <param name="name">Name of the program to unload</param>
    /// <returns>True if program was found and unloaded</returns>
    public bool UnloadProgram(string name)
    {
        ThrowIfDisposed();

        if (_programCache.TryRemove(name, out var program))
        {
            _gl.DeleteProgram(program.ProgramId);
            _logger.LogDebug("Unloaded program: {Name}", name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets shader manager statistics
    /// </summary>
    /// <returns>Statistics</returns>
    public ShaderManagerStatistics GetStatistics()
    {
        ThrowIfDisposed();

        var shaders = _shaderCache.Values.ToArray();

        var vertexShaders = shaders.Count(s => s.Type == ShaderType.VertexShader);
        var fragmentShaders = shaders.Count(s => s.Type == ShaderType.FragmentShader);
        var geometryShaders = shaders.Count(s => s.Type == ShaderType.GeometryShader);
        var computeShaders = shaders.Count(s => s.Type == ShaderType.ComputeShader);
        var tessControlShaders = shaders.Count(s => s.Type == ShaderType.TessControlShader);
        var tessEvalShaders = shaders.Count(s => s.Type == ShaderType.TessEvaluationShader);

        return new ShaderManagerStatistics(
            shaders.Length,
            _programCache.Count,
            vertexShaders,
            fragmentShaders,
            geometryShaders,
            computeShaders,
            tessControlShaders,
            tessEvalShaders,
            _cacheHits,
            _cacheMisses,
            _totalCompilationTime,
            _totalLinkingTime);
    }

    /// <summary>
    /// Compiles a shader from source code
    /// </summary>
    private ManagedShader CompileShaderInternal(string name, string source, ShaderType type)
    {
        var stopwatch = Stopwatch.StartNew();

        var shaderId = _gl.CreateShader(type);
        _gl.ShaderSource(shaderId, source);
        _gl.CompileShader(shaderId);

        _gl.GetShader(shaderId, ShaderParameterName.CompileStatus, out var compileStatus);
        var isCompiled = compileStatus == 1;

        string? compilationLog = null;
        if (!isCompiled)
        {
            compilationLog = _gl.GetShaderInfoLog(shaderId);
            _gl.DeleteShader(shaderId);
            throw new InvalidOperationException($"Shader compilation failed for '{name}': {compilationLog}");
        }

        stopwatch.Stop();
        _totalCompilationTime += stopwatch.ElapsedMilliseconds;

        return new ManagedShader(name, shaderId, type, source, isCompiled, compilationLog);
    }

    /// <summary>
    /// Links a shader program from multiple shaders
    /// </summary>
    private ManagedShaderProgram LinkProgramInternal(string name, ManagedShader[] shaders)
    {
        var stopwatch = Stopwatch.StartNew();

        var programId = _gl.CreateProgram();

        // Attach all shaders
        foreach (var shader in shaders)
        {
            if (!shader.IsCompiled)
                throw new InvalidOperationException($"Cannot link program '{name}': shader '{shader.Name}' is not compiled");

            _gl.AttachShader(programId, shader.ShaderId);
        }

        _gl.LinkProgram(programId);

        _gl.GetProgram(programId, ProgramPropertyARB.LinkStatus, out var linkStatus);
        var isLinked = linkStatus == 1;

        string? linkingLog = null;
        if (!isLinked)
        {
            linkingLog = _gl.GetProgramInfoLog(programId);

            // Detach shaders before deleting program
            foreach (var shader in shaders)
            {
                _gl.DetachShader(programId, shader.ShaderId);
            }

            _gl.DeleteProgram(programId);
            throw new InvalidOperationException($"Program linking failed for '{name}': {linkingLog}");
        }

        stopwatch.Stop();
        _totalLinkingTime += stopwatch.ElapsedMilliseconds;

        return new ManagedShaderProgram(name, programId, shaders, isLinked, linkingLog);
    }

    /// <summary>
    /// Throws if the manager has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ShaderManager));
    }

    /// <summary>
    /// Disposes the manager and releases all resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing ShaderManager with {ShaderCount} shaders and {ProgramCount} programs",
            _shaderCache.Count, _programCache.Count);

        // Delete all programs first
        foreach (var program in _programCache.Values)
        {
            _gl.DeleteProgram(program.ProgramId);
            _logger.LogDebug("Deleted program {Name} (ID: {ProgramId})", program.Name, program.ProgramId);
        }

        // Delete all shaders
        foreach (var shader in _shaderCache.Values)
        {
            _gl.DeleteShader(shader.ShaderId);
            _logger.LogDebug("Deleted shader {Name} (ID: {ShaderId})", shader.Name, shader.ShaderId);
        }

        _shaderCache.Clear();
        _programCache.Clear();
        _currentProgram = null;
        _disposed = true;
    }
}