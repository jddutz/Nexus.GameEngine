namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Generic vertex structure with position and one attribute.
/// Use for simple geometry like colored vertices (position + color).
/// </summary>
/// <typeparam name="TPosition">Position type (Vector2D, Vector3D, etc.)</typeparam>
/// <typeparam name="TAttr1">First attribute type (color, texcoord, normal, etc.)</typeparam>
public struct Vertex<TPosition, TAttr1>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
{
    /// <summary>
    /// Vertex position (maps to shader location 0)
    /// </summary>
    public TPosition Position;
    
    /// <summary>
    /// First vertex attribute (maps to shader location 1)
    /// </summary>
    public TAttr1 Attribute1;
}

/// <summary>
/// Generic vertex structure with position and two attributes.
/// Use for textured geometry with normals (position + normal + texcoord).
/// </summary>
/// <typeparam name="TPosition">Position type</typeparam>
/// <typeparam name="TAttr1">First attribute type</typeparam>
/// <typeparam name="TAttr2">Second attribute type</typeparam>
public struct Vertex<TPosition, TAttr1, TAttr2>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
    where TAttr2 : unmanaged
{
    /// <summary>
    /// Vertex position (maps to shader location 0)
    /// </summary>
    public TPosition Position;
    
    /// <summary>
    /// First vertex attribute (maps to shader location 1)
    /// </summary>
    public TAttr1 Attribute1;
    
    /// <summary>
    /// Second vertex attribute (maps to shader location 2)
    /// </summary>
    public TAttr2 Attribute2;
}

/// <summary>
/// Generic vertex structure with position and three attributes.
/// Use for PBR materials (position + normal + texcoord + tangent).
/// </summary>
/// <typeparam name="TPosition">Position type</typeparam>
/// <typeparam name="TAttr1">First attribute type</typeparam>
/// <typeparam name="TAttr2">Second attribute type</typeparam>
/// <typeparam name="TAttr3">Third attribute type</typeparam>
public struct Vertex<TPosition, TAttr1, TAttr2, TAttr3>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
    where TAttr2 : unmanaged
    where TAttr3 : unmanaged
{
    /// <summary>
    /// Vertex position (maps to shader location 0)
    /// </summary>
    public TPosition Position;
    
    /// <summary>
    /// First vertex attribute (maps to shader location 1)
    /// </summary>
    public TAttr1 Attribute1;
    
    /// <summary>
    /// Second vertex attribute (maps to shader location 2)
    /// </summary>
    public TAttr2 Attribute2;
    
    /// <summary>
    /// Third vertex attribute (maps to shader location 3)
    /// </summary>
    public TAttr3 Attribute3;
}

/// <summary>
/// Generic vertex structure with position and four attributes.
/// Use for advanced materials with multiple texture coordinates or additional data.
/// </summary>
/// <typeparam name="TPosition">Position type</typeparam>
/// <typeparam name="TAttr1">First attribute type</typeparam>
/// <typeparam name="TAttr2">Second attribute type</typeparam>
/// <typeparam name="TAttr3">Third attribute type</typeparam>
/// <typeparam name="TAttr4">Fourth attribute type</typeparam>
public struct Vertex<TPosition, TAttr1, TAttr2, TAttr3, TAttr4>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
    where TAttr2 : unmanaged
    where TAttr3 : unmanaged
    where TAttr4 : unmanaged
{
    /// <summary>
    /// Vertex position (maps to shader location 0)
    /// </summary>
    public TPosition Position;
    
    /// <summary>
    /// First vertex attribute (maps to shader location 1)
    /// </summary>
    public TAttr1 Attribute1;
    
    /// <summary>
    /// Second vertex attribute (maps to shader location 2)
    /// </summary>
    public TAttr2 Attribute2;
    
    /// <summary>
    /// Third vertex attribute (maps to shader location 3)
    /// </summary>
    public TAttr3 Attribute3;
    
    /// <summary>
    /// Fourth vertex attribute (maps to shader location 4)
    /// </summary>
    public TAttr4 Attribute4;
}
