namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Describes vertex input bindings and attributes for a pipeline.
/// Specifies how vertex data is laid out in memory and mapped to shader inputs.
/// </summary>
public record VertexInputDescription
{
    /// <summary>
    /// Vertex input binding descriptions (one per vertex buffer).
    /// </summary>
    public required VertexInputBindingDescription[] Bindings { get; init; }

    /// <summary>
    /// Vertex input attribute descriptions (one per vertex shader input).
    /// </summary>
    public required VertexInputAttributeDescription[] Attributes { get; init; }
}