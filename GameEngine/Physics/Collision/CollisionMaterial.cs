namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Represents material properties for collision response.
/// </summary>
public struct CollisionMaterial(float friction = 0.5f, float restitution = 0.0f, float density = 1.0f, string name = "Default")
{
    public float Friction { get; } = Math.Clamp(friction, 0.0f, 1.0f);
    public float Restitution { get; } = Math.Clamp(restitution, 0.0f, 1.0f);
    public float Density { get; } = Math.Max(0.001f, density);
    public string Name { get; } = name ?? "Default";
    public static readonly CollisionMaterial Default = new CollisionMaterial();
    public static readonly CollisionMaterial Ice = new CollisionMaterial(0.1f, 0.1f, 0.9f, "Ice");
    public static readonly CollisionMaterial Rubber = new CollisionMaterial(0.8f, 0.9f, 1.2f, "Rubber");
    public static readonly CollisionMaterial Metal = new CollisionMaterial(0.3f, 0.2f, 7.8f, "Metal");
    public static readonly CollisionMaterial Wood = new CollisionMaterial(0.6f, 0.3f, 0.7f, "Wood");
}
