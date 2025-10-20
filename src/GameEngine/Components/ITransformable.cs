using Silk.NET.Maths;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Interface for components that have position, rotation, and scale in 3D space.
/// Uses Vulkan/Silk.NET conventions: -Z forward, +Y up, +X right (right-handed coordinate system).
/// All spatial objects are fundamentally 3D - 2D games achieve their appearance via orthographic projection.
/// </summary>
/// <remarks>
/// <para>
/// <b>Coordinate System:</b>
/// - Forward: -Z axis (objects and cameras face toward negative Z)
/// - Up: +Y axis (vertical axis points upward)
/// - Right: +X axis (horizontal right)
/// - System: Right-handed
/// </para>
/// <para>
/// <b>Transform Hierarchy:</b>
/// - Position, Rotation, Scale are local (relative to parent)
/// - WorldMatrix is computed on-demand by walking up the parent chain
/// - Cost: O(h) where h = hierarchy depth (typically 2-4 levels)
/// - Benefits: O(1) updates, no cascading invalidation
/// </para>
/// <para>
/// <b>Animation Support:</b>
/// All transform properties support animation via the ComponentProperty system:
/// - Position: Linear interpolation
/// - Rotation: Quaternion SLERP interpolation
/// - Scale: Linear interpolation
/// - Changes are deferred until UpdateAnimations() call
/// </para>
/// </remarks>
public interface ITransformable
{
    // ==========================================
    // CORE TRANSFORM PROPERTIES
    // ==========================================
    
    /// <summary>
    /// Local position relative to parent (or world position if no parent).
    /// Animated property with interpolation support.
    /// </summary>
    Vector3D<float> Position { get; }
    
    /// <summary>
    /// Local rotation as quaternion (avoids gimbal lock, smooth SLERP interpolation).
    /// Animated property with quaternion spherical linear interpolation support.
    /// Identity quaternion (default) represents no rotation: facing -Z, up is +Y.
    /// </summary>
    Quaternion<float> Rotation { get; }
    
    /// <summary>
    /// Local scale factor for non-uniform scaling.
    /// Default: (1, 1, 1). Animated property with interpolation support.
    /// </summary>
    Vector3D<float> Scale { get; }
    
    // ==========================================
    // TRANSFORM MATRICES
    // ==========================================
    
    /// <summary>
    /// Local transformation matrix (relative to parent).
    /// Computed as: Scale × Rotation × Translation (SRT order).
    /// Cached and recomputed when transform properties change.
    /// </summary>
    Matrix4X4<float> LocalMatrix { get; }
    
    /// <summary>
    /// World transformation matrix (absolute position in world space).
    /// Computed as: LocalMatrix × Parent.WorldMatrix (if parent exists).
    /// Computed on-demand by walking up hierarchy - not cached.
    /// </summary>
    /// <remarks>
    /// This property walks up the parent chain on each access. For frequently-queried
    /// transforms (e.g., bullet spawning), the cost is typically negligible due to shallow
    /// hierarchies (2-4 levels). If profiling shows this is a bottleneck, caching can be added.
    /// </remarks>
    Matrix4X4<float> WorldMatrix { get; }
    
    // ==========================================
    // LOCAL COORDINATE FRAME (Computed from Rotation)
    // ==========================================
    
    /// <summary>
    /// Forward direction in local space (computed from rotation).
    /// Default orientation (Identity quaternion) points -Z.
    /// This is the direction a gun would shoot, a character would face, etc.
    /// </summary>
    Vector3D<float> Forward { get; }
    
    /// <summary>
    /// Right direction in local space (computed from rotation).
    /// Default orientation (Identity quaternion) points +X.
    /// </summary>
    Vector3D<float> Right { get; }
    
    /// <summary>
    /// Up direction in local space (computed from rotation).
    /// Default orientation (Identity quaternion) points +Y.
    /// </summary>
    Vector3D<float> Up { get; }
    
    // ==========================================
    // WORLD COORDINATE FRAME (Computed from WorldMatrix)
    // ==========================================
    
    /// <summary>
    /// Forward direction in world space (accounts for parent rotations).
    /// Use this when shooting bullets, checking facing direction, etc.
    /// Extracted from WorldMatrix - computed on-demand.
    /// </summary>
    Vector3D<float> WorldForward { get; }
    
    /// <summary>
    /// Right direction in world space (accounts for parent rotations).
    /// Extracted from WorldMatrix - computed on-demand.
    /// </summary>
    Vector3D<float> WorldRight { get; }
    
    /// <summary>
    /// Up direction in world space (accounts for parent rotations).
    /// Extracted from WorldMatrix - computed on-demand.
    /// </summary>
    Vector3D<float> WorldUp { get; }
    
    /// <summary>
    /// World position (accounts for parent transforms).
    /// Convenience property equivalent to extracting translation from WorldMatrix.
    /// Computed on-demand.
    /// </summary>
    Vector3D<float> WorldPosition { get; }
    
    // ==========================================
    // POSITION METHODS
    // ==========================================
    
    /// <summary>
    /// Set local position (relative to parent).
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="position">New local position.</param>
    void SetPosition(Vector3D<float> position);
    
    /// <summary>
    /// Move in world space (absolute direction, ignoring rotation).
    /// Example: Translate(new Vector3D(1, 0, 0)) always moves right regardless of rotation.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="delta">World-space movement delta.</param>
    void Translate(Vector3D<float> delta);
    
    /// <summary>
    /// Move in local space (relative to current rotation).
    /// Example: TranslateLocal(new Vector3D(0, 0, -1)) moves forward in the direction the object is facing.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="delta">Local-space movement delta.</param>
    void TranslateLocal(Vector3D<float> delta);
    
    // ==========================================
    // ROTATION METHODS
    // ==========================================
    
    /// <summary>
    /// Set absolute rotation.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="rotation">New rotation quaternion.</param>
    void SetRotation(Quaternion<float> rotation);
    
    /// <summary>
    /// Rotate around local X axis (pitch - nod up/down).
    /// Positive radians nod down (right-hand rule: thumb points +X, fingers curl down).
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    void RotateX(float radians);
    
    /// <summary>
    /// Rotate around local Y axis (yaw - turn left/right).
    /// Positive radians turn left (right-hand rule: thumb points +Y, fingers curl left).
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    void RotateY(float radians);
    
    /// <summary>
    /// Rotate around local Z axis (roll - tilt side-to-side).
    /// Positive radians tilt counter-clockwise (right-hand rule: thumb points +Z toward viewer, fingers curl CCW).
    /// Most common rotation for 2D sprite orientation.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    void RotateZ(float radians);
    
    /// <summary>
    /// Rotate around arbitrary axis (in local space).
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="axis">Rotation axis (will be normalized).</param>
    /// <param name="radians">Rotation angle in radians.</param>
    void RotateAxis(Vector3D<float> axis, float radians);
    
    /// <summary>
    /// Orient to face target position.
    /// Uses default world up vector (+Y).
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    void LookAt(Vector3D<float> target);
    
    /// <summary>
    /// Orient to face target with specified world up vector.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    /// <param name="worldUp">World up direction (will be normalized).</param>
    void LookAt(Vector3D<float> target, Vector3D<float> worldUp);
    
    // ==========================================
    // SCALE METHODS
    // ==========================================
    
    /// <summary>
    /// Set absolute scale.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="scale">New scale vector.</param>
    void SetScale(Vector3D<float> scale);
    
    /// <summary>
    /// Multiply current scale by factor.
    /// Example: ScaleBy(new Vector3D(2, 1, 1)) doubles width while keeping height/depth.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="scaleFactor">Scale multiplier per axis.</param>
    void ScaleBy(Vector3D<float> scaleFactor);
    
    /// <summary>
    /// Uniform scaling (multiply all axes by same factor).
    /// Example: ScaleUniform(2) doubles size in all dimensions.
    /// Change is deferred until next UpdateAnimations() call if component is active.
    /// </summary>
    /// <param name="scaleFactor">Uniform scale multiplier.</param>
    void ScaleUniform(float scaleFactor);
}
