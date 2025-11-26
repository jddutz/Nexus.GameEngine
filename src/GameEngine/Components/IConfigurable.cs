namespace Nexus.GameEngine.Components;

/// <summary>
/// Composite interface that combines loading and validation capabilities.
/// Used by source generators to identify components that support template-based configuration.
/// </summary>
public interface IConfigurable : IEntity, ILoadable, IValidatable
{
}
