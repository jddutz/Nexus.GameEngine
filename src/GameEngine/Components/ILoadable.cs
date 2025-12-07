namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Defines the contract for component identity, hierarchy, lifecycle, and event management.
/// </summary>
public interface ILoadable
{
    // Configuration Events
    event EventHandler<ConfigurationEventArgs>? Loading;
    event EventHandler<ConfigurationEventArgs>? Loaded;
    event EventHandler? Unloading;
    event EventHandler? Unloaded;

    /// <summary>
    /// Gets whether the component is currently loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Load the component using the specified template.
    /// </summary>
    /// <param name="template">ComponentTemplate used for configuration. Can be null for template-less components.</param>
    void Load(Template template);

    /// <summary>
    /// Unload the component and release resources.
    /// </summary>
    void Unload();
}