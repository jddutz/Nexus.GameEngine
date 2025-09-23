namespace Nexus.GameEngine.Graphics.Rendering.Shaders;

/// <summary>
/// Statistics for shader manager performance monitoring
/// </summary>
public record ShaderManagerStatistics(
    int LoadedShaders,
    int LoadedPrograms,
    int VertexShaders,
    int FragmentShaders,
    int GeometryShaders,
    int ComputeShaders,
    int TessellationControlShaders,
    int TessellationEvaluationShaders,
    int CacheHits,
    int CacheMisses,
    long CompilationTimeMs,
    long LinkingTimeMs
);