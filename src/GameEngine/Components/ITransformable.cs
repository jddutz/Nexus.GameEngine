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
/// - Changes are deferred until ApplyUpdates() call
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
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="position">New local position.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void SetPosition(Vector3D<float> position, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Set local position immediately.
    /// </summary>
    /// <param name="position">New local position.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void SetCurrentPosition(Vector3D<float> position, bool setTarget = false);
    
    /// <summary>
    /// Move in world space (absolute direction, ignoring rotation).
    /// Example: Translate(new Vector3D(1, 0, 0)) always moves right regardless of rotation.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="delta">World-space movement delta.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void Translate(Vector3D<float> delta, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Move in world space immediately.
    /// </summary>
    /// <param name="delta">World-space movement delta.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void TranslateImmediate(Vector3D<float> delta, bool setTarget = false);
    
    /// <summary>
    /// Move in local space (relative to current rotation).
    /// Example: TranslateLocal(new Vector3D(0, 0, -1)) moves forward in the direction the object is facing.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="delta">Local-space movement delta.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void TranslateLocal(Vector3D<float> delta, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Move in local space immediately.
    /// </summary>
    /// <param name="delta">Local-space movement delta.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void TranslateLocalImmediate(Vector3D<float> delta, bool setTarget = false);
    
    /// <summary>
    /// Sets the linear velocity for continuous movement (units per second).
    /// Applied automatically during OnComponentUpdate. Call with Zero to stop movement.
    /// </summary>
    /// <param name="velocity">Linear velocity in world space (units per second).</param>
    void SetVelocity(Vector3D<float> velocity);
    
    // ==========================================
    // ROTATION METHODS
    // ==========================================
    
    /// <summary>
    /// Set absolute rotation.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="rotation">New rotation quaternion.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void SetRotation(Quaternion<float> rotation, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Set absolute rotation immediately.
    /// </summary>
    /// <param name="rotation">New rotation quaternion.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void SetCurrentRotation(Quaternion<float> rotation, bool setTarget = false);
    
    /// <summary>
    /// Rotate around local X axis (pitch - nod up/down).
    /// Positive radians nod down (right-hand rule: thumb points +X, fingers curl down).
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void RotateX(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Rotate around local X axis immediately.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void RotateXImmediate(float radians, bool setTarget = false);
    
    /// <summary>
    /// Rotate around local Y axis (yaw - turn left/right).
    /// Positive radians turn left (right-hand rule: thumb points +Y, fingers curl left).
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void RotateY(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Rotate around local Y axis immediately.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void RotateYImmediate(float radians, bool setTarget = false);
    
    /// <summary>
    /// Rotate around local Z axis (roll - tilt side-to-side).
    /// Positive radians tilt counter-clockwise (right-hand rule: thumb points +Z toward viewer, fingers curl CCW).
    /// Most common rotation for 2D sprite orientation.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void RotateZ(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Rotate around local Z axis immediately.
    /// </summary>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void RotateZImmediate(float radians, bool setTarget = false);
    
    /// <summary>
    /// Rotate around arbitrary axis (in local space).
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="axis">Rotation axis (will be normalized).</param>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void RotateAxis(Vector3D<float> axis, float radians, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Rotate around arbitrary axis immediately.
    /// </summary>
    /// <param name="axis">Rotation axis (will be normalized).</param>
    /// <param name="radians">Rotation angle in radians.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void RotateAxisImmediate(Vector3D<float> axis, float radians, bool setTarget = false);
    
    /// <summary>
    /// Orient to face target position.
    /// Uses default world up vector (+Y).
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void LookAt(Vector3D<float> target, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Orient to face target position immediately.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void LookAtImmediate(Vector3D<float> target, bool setTarget = false);
    
    /// <summary>
    /// Orient to face target with specified world up vector.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    /// <param name="worldUp">World up direction (will be normalized).</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void LookAt(Vector3D<float> target, Vector3D<float> worldUp, InterpolationFunction<Quaternion<float>>? interpolator = null);

    /// <summary>
    /// Orient to face target with specified world up vector immediately.
    /// </summary>
    /// <param name="target">World position to look at.</param>
    /// <param name="worldUp">World up direction (will be normalized).</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void LookAtImmediate(Vector3D<float> target, Vector3D<float> worldUp, bool setTarget = false);
    
    /// <summary>
    /// Sets the angular velocity for continuous rotation (radians per second).
    /// Applied automatically during OnComponentUpdate as Yaw, Pitch, Roll.
    /// Call with Zero to stop rotation.
    /// </summary>
    /// <param name="angularVelocity">Angular velocity (Y=yaw, X=pitch, Z=roll) in radians per second.</param>
    void SetAngularVelocity(Vector3D<float> angularVelocity);
    
    // ==========================================
    // SCALE METHODS
    // ==========================================
    
    /// <summary>
    /// Set absolute scale.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="scale">New scale vector.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void SetScale(Vector3D<float> scale, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Set absolute scale immediately.
    /// </summary>
    /// <param name="scale">New scale vector.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void SetCurrentScale(Vector3D<float> scale, bool setTarget = false);
    
    /// <summary>
    /// Multiply current scale by factor.
    /// Example: ScaleBy(new Vector3D(2, 1, 1)) doubles width while keeping height/depth.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="scaleFactor">Scale multiplier per axis.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void ScaleBy(Vector3D<float> scaleFactor, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Multiply current scale by factor immediately.
    /// </summary>
    /// <param name="scaleFactor">Scale multiplier per axis.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void ScaleByImmediate(Vector3D<float> scaleFactor, bool setTarget = false);
    
    /// <summary>
    /// Uniform scaling (multiply all axes by same factor).
    /// Example: ScaleUniform(2) doubles size in all dimensions.
    /// Change is deferred until next ApplyUpdates() call if component is active.
    /// </summary>
    /// <param name="scaleFactor">Uniform scale multiplier.</param>
    /// <param name="interpolator">Optional interpolation function.</param>
    void ScaleUniform(float scaleFactor, InterpolationFunction<Vector3D<float>>? interpolator = null);

    /// <summary>
    /// Uniform scaling immediately.
    /// </summary>
    /// <param name="scaleFactor">Uniform scale multiplier.</param>
    /// <param name="setTarget">If true, also updates the target value to prevent interpolation back to old target.</param>
    void ScaleUniformImmediate(float scaleFactor, bool setTarget = false);
}
