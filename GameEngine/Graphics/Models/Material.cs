using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Material properties for 3D model rendering.
/// </summary>
public struct Material
{
    /// <summary>
    /// The diffuse color of the material.
    /// </summary>
    public Vector3D<float> DiffuseColor { get; set; }

    /// <summary>
    /// The specular color and intensity.
    /// </summary>
    public Vector3D<float> SpecularColor { get; set; }

    /// <summary>
    /// The shininess/glossiness of the material.
    /// Higher values create smaller, more focused highlights.
    /// </summary>
    public float Shininess { get; set; }

    /// <summary>
    /// The opacity of the material.
    /// 1.0 is fully opaque, 0.0 is fully transparent.
    /// </summary>
    public float Opacity { get; set; }
}