namespace Nexus.GameEngine.Components;

/// <summary>
/// Attribute that defines caching behavior for components.
/// Applied to component classes to control how the factory caches instances.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ComponentCachingAttribute.
/// </remarks>
/// <param name="strategy">The caching strategy to use</param>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ComponentCachingAttribute(CachingStrategy strategy) : Attribute
{
    /// <summary>
    /// The caching strategy to use for this component type.
    /// Determines whether to check cache before creation and whether to cache the result.
    /// </summary>
    public CachingStrategy Strategy { get; } = strategy;
}
