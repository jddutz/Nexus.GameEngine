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

    /// <summary>
    /// Load the component using the specified template.
    /// </summary>
    /// <param name="template">ComponentTemplate used for configuration. Can be null for template-less components.</param>
    void Load(Template template);
}