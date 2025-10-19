using System.Reflection;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Base interface for resource definitions - pure data descriptions of resources
/// </summary>
public interface IResourceDefinition
{
    /// <summary>
    /// Unique name for this resource
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The assembly containing embedded resource files for this resource.
    /// If null, defaults to the GameEngine assembly.
    /// This allows loading resources from game projects or external assemblies.
    /// </summary>
    Assembly? SourceAssembly => null;
}