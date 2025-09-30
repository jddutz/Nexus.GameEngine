using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Manages reusable content trees that can be assigned to viewports.
/// Content can include UI screens, game levels, menus, or any component hierarchy.
/// </summary>
public interface IContentManager : IDisposable
{
    /// <summary>
    /// Creates a content tree from the provided template and stores it with the given name.
    /// If content with the same name already exists, this operation is ignored.
    /// </summary>
    /// <param name="template">Template to instantiate</param>
    void Create(IComponentTemplate template);

    /// <summary>
    /// Attempts to retrieve content by name.
    /// </summary>
    /// <param name="name">Name of the content to retrieve</param>
    /// <returns>The content component if found, null otherwise</returns>
    IRuntimeComponent? TryGet(string name);

    /// <summary>
    /// Gets existing content or creates it from the template if it doesn't exist.
    /// </summary>
    /// <param name="template">Template to use if content needs to be created</param>
    /// <returns>The content component, or null if creation failed</returns>
    IRuntimeComponent? GetOrCreate(IComponentTemplate template);

    /// <summary>
    /// Removes and disposes content by name.
    /// </summary>
    /// <param name="name">Name of the content to remove</param>
    /// <returns>True if content was found and removed, false otherwise</returns>
    bool Remove(string name);

    /// <summary>
    /// Gets all content names currently managed.
    /// </summary>
    IEnumerable<string> GetContentNames();
}