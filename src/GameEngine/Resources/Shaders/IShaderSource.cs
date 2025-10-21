using Nexus.GameEngine.Resources.Sources;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Source for loading shader data.
/// Implementations handle different shader formats and compilation mechanisms.
/// </summary>
public interface IShaderSource : IResourceSource<ShaderSourceData>
{
}
