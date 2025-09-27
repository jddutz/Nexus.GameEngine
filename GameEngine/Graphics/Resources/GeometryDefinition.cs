namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Defines geometry data including vertices, indices, and vertex attribute layout.
/// </summary>
public record GeometryDefinition : IResourceDefinition
{
    /// <summary>
    /// The name of the geometry resource.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this geometry should be kept in memory and not purged during resource cleanup.
    /// </summary>
    public bool IsPersistent { get; init; } = true;

    /// <summary>
    /// The vertex data as a flat array of floats.
    /// </summary>
    public float[] Vertices { get; init; } = [];

    /// <summary>
    /// The index data for element buffer objects.
    /// </summary>
    public int[] Indices { get; init; } = [];

    /// <summary>
    /// The vertex attribute layout definitions.
    /// </summary>
    public VertexAttribute[] Attributes { get; init; } = [];
}