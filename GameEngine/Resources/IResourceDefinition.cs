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
}