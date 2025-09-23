namespace Nexus.GameEngine.Components;

/// <summary>
/// Defines the caching strategy for components.
/// </summary>
public enum CachingStrategy
{
    /// <summary>
    /// RuntimeComponent is never cached - always creates new instances.
    /// Suitable for temporary objects like bullets, effects, or dialogs.
    /// </summary>
    Transient,

    /// <summary>
    /// One instance per component type - type serves as the cache key.
    /// Suitable for UI screens, managers, or other unique system components.
    /// </summary>
    Singleton,

    /// <summary>
    /// Multiple instances cached by component-generated identifier.
    /// Suitable for entities, NPCs, or other contextual objects that may be reused.
    /// </summary>
    Contextual
}