using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Manages the lifecycle of OpenGL resources with caching and validation
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly IWindowService _windowService;
    private readonly ILogger<ResourceManager> _logger;
    private readonly ConcurrentDictionary<string, uint> _resourceCache;
    private readonly ConcurrentDictionary<string, IResourceDefinition> _resourceDefinitions;
    private readonly object _lockObject = new();
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private bool _disposed = false;

    /// <summary>
    /// Gets the Silk.NET OpenGL interface for use by resource management.
    /// Lazily initializes the GL context on first access using the window service.
    /// </summary>
    private GL? _gl;
    private GL GL
    {
        get
        {
            _gl ??= _windowService.GetOrCreateWindow().CreateOpenGL();
            return _gl;
        }
    }

    public ResourceManager(IWindowService windowService, ILogger<ResourceManager> logger)
    {
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceCache = new ConcurrentDictionary<string, uint>();
        _resourceDefinitions = new ConcurrentDictionary<string, IResourceDefinition>();

        _logger.LogDebug("ResourceManager initialized");
    }

    public uint GetOrCreateResource(IResourceDefinition definition, IAssetService? assetService = null)
    {
        ThrowIfDisposed();

        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        // Check cache first
        if (_resourceCache.TryGetValue(definition.Name, out var cachedResource))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("Resource cache hit for {ResourceName}", definition.Name);
            return cachedResource;
        }

        lock (_lockObject)
        {
            // Double-check after acquiring lock
            if (_resourceCache.TryGetValue(definition.Name, out cachedResource))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedResource;
            }

            // Validate resource before creation
            var validation = ValidateResource(definition);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors);
                throw new InvalidOperationException($"Resource validation failed for '{definition.Name}': {errors}");
            }

            // Create the resource
            var resourceId = CreateResource(definition, assetService);

            _resourceCache.TryAdd(definition.Name, resourceId);
            _resourceDefinitions.TryAdd(definition.Name, definition);

            Interlocked.Increment(ref _cacheMisses);
            _logger.LogDebug("Created resource {ResourceName} with ID {ResourceId}", definition.Name, resourceId);

            return resourceId;
        }
    }

    public ResourceValidationResult ValidateResource(IResourceDefinition definition)
    {
        ThrowIfDisposed();

        if (definition == null)
            return ResourceValidationResult.Failed("Resource definition is null");

        if (string.IsNullOrWhiteSpace(definition.Name))
            return ResourceValidationResult.Failed("Resource name is required");

        return definition switch
        {
            GeometryDefinition geometry => ValidateGeometry(geometry),
            ShaderDefinition shader => ValidateShader(shader),
            _ => ResourceValidationResult.Failed($"Unknown resource type: {definition.GetType().Name}")
        };
    }

    public bool ReleaseResource(string resourceName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(resourceName))
            return false;

        lock (_lockObject)
        {
            if (!_resourceCache.TryRemove(resourceName, out var resourceId))
                return false;

            _resourceDefinitions.TryRemove(resourceName, out _);

            // Delete the OpenGL resource
            DeleteResource(resourceId, resourceName);

            _logger.LogDebug("Released resource {ResourceName} with ID {ResourceId}", resourceName, resourceId);
            return true;
        }
    }

    public ResourceManagerStatistics GetStatistics()
    {
        ThrowIfDisposed();

        var geometryCount = _resourceDefinitions.Values.OfType<GeometryDefinition>().Count();
        var shaderCount = _resourceDefinitions.Values.OfType<ShaderDefinition>().Count();
        var textureCount = _resourceDefinitions.Count - geometryCount - shaderCount;

        return new ResourceManagerStatistics
        {
            TotalResources = _resourceCache.Count,
            GeometryCount = geometryCount,
            ShaderCount = shaderCount,
            TextureCount = textureCount,
            EstimatedMemoryUsage = EstimateMemoryUsage(),
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses
        };
    }

    public int PurgeUnusedResources()
    {
        ThrowIfDisposed();

        // For now, we don't track resource usage, so this is a no-op
        // In a full implementation, we would track reference counts
        _logger.LogDebug("PurgeUnusedResources called - no unused resources to purge");
        return 0;
    }

    private uint CreateResource(IResourceDefinition definition, IAssetService? assetService)
    {
        return definition switch
        {
            GeometryDefinition geometry => CreateGeometry(geometry),
            ShaderDefinition shader => CreateShader(shader),
            _ => throw new NotSupportedException($"Resource type {definition.GetType().Name} is not supported")
        };
    }

    private uint CreateGeometry(GeometryDefinition geometry)
    {
        _logger.LogDebug("Creating geometry '{GeometryName}' with {VertexCount} vertices, {IndexCount} indices",
            geometry.Name, geometry.Vertices.Length, geometry.Indices?.Length ?? 0);

        // Debug: Log vertex data
        if (geometry.Vertices.Length <= 20) // Only log if small array
        {
            _logger.LogDebug("Vertex data: [{VertexData}]", string.Join(", ", geometry.Vertices));
        }

        // Create VAO
        var vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        _logger.LogDebug("Created VAO {VAOId}", vao);

        // Create VBO
        var vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        _logger.LogDebug("Created VBO {VBOId}", vbo);

        var vertices = geometry.Vertices.ToArray();
        unsafe
        {
            fixed (float* vertexPtr = vertices)
            {
                GL.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(vertices.Length * sizeof(float)),
                    vertexPtr,
                    ConvertUsageHint(geometry.UsageHint));
            }
        }

        var error = GL.GetError();
        if (error != GLEnum.NoError)
        {
            _logger.LogWarning("GL Error after VBO creation: {Error}", error);
        }

        // Create EBO if indices are provided
        uint? ebo = null;
        if (geometry.Indices.HasValue)
        {
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo.Value);

            var indices = geometry.Indices.Value.ToArray();
            unsafe
            {
                fixed (uint* indexPtr = indices)
                {
                    GL.BufferData(BufferTargetARB.ElementArrayBuffer,
                        (nuint)(indices.Length * sizeof(uint)),
                        indexPtr,
                        ConvertUsageHint(geometry.UsageHint));
                }
            }
        }

        // Set up vertex attributes
        foreach (var attribute in geometry.Attributes)
        {
            _logger.LogDebug("Setting up vertex attribute {Location}: {ComponentCount} components, stride {Stride}, offset {Offset}",
                attribute.Location, attribute.ComponentCount, attribute.Stride, attribute.Offset);

            GL.EnableVertexAttribArray(attribute.Location);
            unsafe
            {
                GL.VertexAttribPointer(
                    attribute.Location,
                    attribute.ComponentCount,
                    ConvertVertexAttribType(attribute.Type),
                    attribute.Normalized,
                    attribute.Stride,
                    (void*)attribute.Offset);
            }

            var attrError = GL.GetError();
            if (attrError != GLEnum.NoError)
            {
                _logger.LogWarning("GL Error setting up attribute {Location}: {Error}", attribute.Location, attrError);
            }
        }

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        return vao;
    }

    private uint CreateShader(ShaderDefinition shader)
    {
        var vertexShader = CompileShader(ShaderType.VertexShader, shader.VertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, shader.FragmentSource);
        uint? geometryShader = null;

        if (!string.IsNullOrEmpty(shader.GeometrySource))
        {
            geometryShader = CompileShader(ShaderType.GeometryShader, shader.GeometrySource);
        }

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);

        if (geometryShader.HasValue)
            GL.AttachShader(program, geometryShader.Value);

        GL.LinkProgram(program);

        // Check link status
        GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            var infoLog = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            throw new InvalidOperationException($"Shader program linking failed: {infoLog}");
        }

        // Clean up individual shaders
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        if (geometryShader.HasValue)
            GL.DeleteShader(geometryShader.Value);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameterName.CompileStatus, out var compileStatus);
        if (compileStatus == 0)
        {
            var infoLog = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Shader compilation failed ({type}): {infoLog}");
        }

        return shader;
    }

    private ResourceValidationResult ValidateGeometry(GeometryDefinition geometry)
    {
        var errors = new List<string>();

        if (geometry.Vertices.IsEmpty)
            errors.Add("Vertex data is required");

        if (geometry.Attributes.Count == 0)
            errors.Add("At least one vertex attribute is required");

        foreach (var attr in geometry.Attributes)
        {
            if (attr.ComponentCount < 1 || attr.ComponentCount > 4)
                errors.Add($"Invalid component count {attr.ComponentCount} for attribute {attr.Location}");
        }

        return errors.Count == 0 ? ResourceValidationResult.Success() : ResourceValidationResult.Failed(errors);
    }

    private ResourceValidationResult ValidateShader(ShaderDefinition shader)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(shader.VertexSource))
            errors.Add("Vertex shader source is required");

        if (string.IsNullOrWhiteSpace(shader.FragmentSource))
            errors.Add("Fragment shader source is required");

        return errors.Count == 0 ? ResourceValidationResult.Success() : ResourceValidationResult.Failed(errors);
    }

    private void DeleteResource(uint resourceId, string resourceName)
    {
        // This is a simplified deletion - in practice, we'd need to track resource types
        // For now, assume it's a VAO or shader program
        try
        {
            GL.DeleteVertexArray(resourceId);
        }
        catch
        {
            try
            {
                GL.DeleteProgram(resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete resource {ResourceName} with ID {ResourceId}", resourceName, resourceId);
            }
        }
    }

    private long EstimateMemoryUsage()
    {
        // Simplified estimation - in practice, we'd track actual sizes
        return _resourceCache.Count * 1024; // Assume 1KB per resource on average
    }

    private static BufferUsageARB ConvertUsageHint(BufferUsageHint hint) => hint switch
    {
        BufferUsageHint.StaticDraw => BufferUsageARB.StaticDraw,
        BufferUsageHint.DynamicDraw => BufferUsageARB.DynamicDraw,
        BufferUsageHint.StreamDraw => BufferUsageARB.StreamDraw,
        _ => BufferUsageARB.StaticDraw
    };

    private static Silk.NET.OpenGL.VertexAttribPointerType ConvertVertexAttribType(Geometry.VertexAttribPointerType type) => type switch
    {
        Geometry.VertexAttribPointerType.Byte => Silk.NET.OpenGL.VertexAttribPointerType.Byte,
        Geometry.VertexAttribPointerType.UnsignedByte => Silk.NET.OpenGL.VertexAttribPointerType.UnsignedByte,
        Geometry.VertexAttribPointerType.Short => Silk.NET.OpenGL.VertexAttribPointerType.Short,
        Geometry.VertexAttribPointerType.UnsignedShort => Silk.NET.OpenGL.VertexAttribPointerType.UnsignedShort,
        Geometry.VertexAttribPointerType.Int => Silk.NET.OpenGL.VertexAttribPointerType.Int,
        Geometry.VertexAttribPointerType.UnsignedInt => Silk.NET.OpenGL.VertexAttribPointerType.UnsignedInt,
        Geometry.VertexAttribPointerType.Float => Silk.NET.OpenGL.VertexAttribPointerType.Float,
        Geometry.VertexAttribPointerType.Double => Silk.NET.OpenGL.VertexAttribPointerType.Double,
        _ => Silk.NET.OpenGL.VertexAttribPointerType.Float
    };

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResourceManager));
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            foreach (var kvp in _resourceCache)
            {
                DeleteResource(kvp.Value, kvp.Key);
            }

            _resourceCache.Clear();
            _resourceDefinitions.Clear();
        }

        _disposed = true;
        _logger.LogDebug("ResourceManager disposed");
    }
}
