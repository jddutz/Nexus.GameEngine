namespace Nexus.GameEngine.Components;

/// <summary>
/// Runtime component that provides position, rotation, and scale in 3D space.
/// Implements the ITransformable interface with full animation support.
/// Uses Vulkan/Silk.NET conventions: -Z forward, +Y up, +X right (right-handed).
/// </summary>
public partial class Transformable : Component, ITransformable
{
    // ==========================================
    // ANIMATED PROPERTIES (with ComponentProperty attribute)
    // ==========================================

    [ComponentProperty]
    [TemplateProperty]
    protected Vector3D<float> _position = Vector3D<float>.Zero;    
    partial void OnPositionChanged(Vector3D<float> oldValue) => UpdateLocalMatrix();
    
    [ComponentProperty]
    [TemplateProperty]
    protected Quaternion<float> _rotation = Quaternion<float>.Identity;
    partial void OnRotationChanged(Quaternion<float> oldValue) => UpdateLocalMatrix();
    
    [ComponentProperty]
    [TemplateProperty]
    protected Vector3D<float> _scale = new Vector3D<float>(1f, 1f, 1f);
    partial void OnScaleChanged(Vector3D<float> oldValue) => UpdateLocalMatrix();
    
    // ==========================================
    // VELOCITY FIELDS (for continuous animations)
    // ==========================================
    
    /// <summary>
    /// Linear velocity in units per second (world space).
    /// Applied automatically during OnComponentUpdate.
    /// </summary>
    private Vector3D<float> _velocity = Vector3D<float>.Zero;
    
    /// <summary>
    /// Angular velocity in radians per second (Yaw, Pitch, Roll).
    /// Applied automatically during OnComponentUpdate.
    /// </summary>
    private Vector3D<float> _angularVelocity = Vector3D<float>.Zero;
    
    // ==========================================
    // TRANSFORM MATRICES (CACHED)
    // ==========================================
    
    /// <summary>
    /// Cached local transformation matrix. Recalculated when position, rotation, or scale changes.
    /// Protected to allow derived classes to set it in their UpdateLocalMatrix override.
    /// </summary>
    protected Matrix4X4<float> _localMatrix = Matrix4X4<float>.Identity;
    
    /// <summary>
    /// Cached world transformation matrix. Recalculated when local matrix or parent world matrix changes.
    /// </summary>
    protected Matrix4X4<float> _worldMatrix = Matrix4X4<float>.Identity;
    
    /// <summary>
    /// Tracks whether the cached world matrix is valid.
    /// Invalidated when local matrix changes or when parent changes notify us.
    /// </summary>
    protected bool _worldMatrixInvalid = true;
    
    /// <summary>
    /// Gets the cached local transformation matrix (SRT: Scale-Rotation-Translation).
    /// Virtual to allow derived classes to override with custom behavior.
    /// </summary>
    public virtual Matrix4X4<float> LocalMatrix => _localMatrix;
    
    /// <summary>
    /// Recalculates the local transformation matrix from current position, rotation, and scale.
    /// Called automatically when any transform property changes.
    /// Also invalidates the cached world matrix.
    /// </summary>
    protected virtual void UpdateLocalMatrix()
    {
        // Standard SRT (Scale-Rotation-Translation) matrix composition
        // Silk.NET uses column-major matrices with column vectors
        // This scales first, then rotates, then translates
        _localMatrix = Matrix4X4.CreateScale(_scale) *
                       Matrix4X4.CreateFromQuaternion(_rotation) *
                       Matrix4X4.CreateTranslation(_position);
        
        // Local matrix changed, so world matrix is now dirty
        InvalidateWorldMatrix();
    }
    
    /// <summary>
    /// Invalidates the cached world matrix for this component and all children.
    /// Called automatically when local transform changes or parent changes.
    /// </summary>
    protected virtual void InvalidateWorldMatrix()
    {
        _worldMatrixInvalid = true;
        
        // Propagate invalidation to all children that are transformable
        foreach (var child in GetChildren<Transformable>())
        {
            child.InvalidateWorldMatrix();
        }
    }
    
    /// <summary>
    /// Recalculates the world matrix if it's dirty.
    /// Called automatically when WorldMatrix property is accessed.
    /// </summary>
    protected virtual void UpdateWorldMatrix()
    {
        // Hierarchical transform composition: WorldMatrix = ChildLocal * ParentWorld
        // With S*R*T matrices, this order correctly transforms the child's local position
        // by the parent's rotation, then adds the parent's world position
        if (Parent is ITransformable parentTransform)
            _worldMatrix = LocalMatrix * parentTransform.WorldMatrix;
        else
            _worldMatrix = LocalMatrix; // Root object: local space = world space
        
        _worldMatrixInvalid = false;
    }
    
    public Matrix4X4<float> WorldMatrix
    {
        get
        {
            if (_worldMatrixInvalid) UpdateWorldMatrix();
            return _worldMatrix;
        }
    }
    
    // ==========================================
    // LOCAL COORDINATE FRAME
    // ==========================================
    
    public Vector3D<float> Forward => Vector3D.Normalize(Vector3D.Transform(
        -Vector3D<float>.UnitZ,  // Default forward direction (-Z)
        _rotation));
    
    public Vector3D<float> Right => Vector3D.Normalize(Vector3D.Transform(
        Vector3D<float>.UnitX,   // Default right direction (+X)
        _rotation));
    
    public Vector3D<float> Up => Vector3D.Normalize(Vector3D.Transform(
        Vector3D<float>.UnitY,   // Default up direction (+Y)
        _rotation));
    
    // ==========================================
    // WORLD COORDINATE FRAME
    // ==========================================
    
    public Vector3D<float> WorldForward
    {
        get
        {
            var worldMatrix = WorldMatrix;
            // Extract forward vector from third row of matrix, negated
            // Matrix row 3 contains the -Z basis vector (our forward is -Z)
            return Vector3D.Normalize(new Vector3D<float>(-worldMatrix.M31, -worldMatrix.M32, -worldMatrix.M33));
        }
    }
    
    public Vector3D<float> WorldRight
    {
        get
        {
            var worldMatrix = WorldMatrix;
            // Extract right vector from first row of matrix (+X basis vector)
            return Vector3D.Normalize(new Vector3D<float>(worldMatrix.M11, worldMatrix.M12, worldMatrix.M13));
        }
    }
    
    public Vector3D<float> WorldUp
    {
        get
        {
            var worldMatrix = WorldMatrix;
            // Extract up vector from second row of matrix (+Y basis vector)
            return Vector3D.Normalize(new Vector3D<float>(worldMatrix.M21, worldMatrix.M22, worldMatrix.M23));
        }
    }
    
    public Vector3D<float> WorldPosition
    {
        get
        {
            var worldMatrix = WorldMatrix;
            // Extract translation from fourth row of matrix (M41, M42, M43)
            return new Vector3D<float>(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
        }
    }
    
    // ==========================================
    // POSITION METHODS
    // ==========================================
    
    public void Translate(Vector3D<float> delta, InterpolationFunction<Vector3D<float>>? interpolator = null)
    {
        SetPosition(_position + delta, interpolator);
    }

    public void TranslateImmediate(Vector3D<float> delta, bool setTarget = false)
    {
        SetCurrentPosition(_position + delta, setTarget);
    }
    
    public void TranslateLocal(Vector3D<float> delta, InterpolationFunction<Vector3D<float>>? interpolator = null)
    {
        // Transform delta from local space to world space using current rotation
        var worldDelta = Vector3D.Transform(delta, _rotation);
        SetPosition(_position + worldDelta, interpolator);
    }

    public void TranslateLocalImmediate(Vector3D<float> delta, bool setTarget = false)
    {
        // Transform delta from local space to world space using current rotation
        var worldDelta = Vector3D.Transform(delta, _rotation);
        SetCurrentPosition(_position + worldDelta, setTarget);
    }
    
    /// <summary>
    /// Sets the linear velocity for continuous movement (units per second).
    /// Call with Zero to stop movement.
    /// </summary>
    public void SetVelocity(Vector3D<float> velocity)
    {
        _velocity = velocity;
    }
    
    // ==========================================
    // ROTATION METHODS
    // ==========================================
    
    public void RotateX(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, radians);
        SetRotation(_rotation * rotation, interpolator);
    }

    public void RotateXImmediate(float radians, bool setTarget = false)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, radians);
        SetCurrentRotation(_rotation * rotation, setTarget);
    }
    
    public void RotateY(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, radians);
        SetRotation(_rotation * rotation, interpolator);
    }

    public void RotateYImmediate(float radians, bool setTarget = false)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, radians);
        SetCurrentRotation(_rotation * rotation, setTarget);
    }
    
    public void RotateZ(float radians, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, radians);
        SetRotation(_rotation * rotation, interpolator);
    }

    public void RotateZImmediate(float radians, bool setTarget = false)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, radians);
        SetCurrentRotation(_rotation * rotation, setTarget);
    }
    
    public void RotateAxis(Vector3D<float> axis, float radians, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        var normalizedAxis = Vector3D.Normalize(axis);
        var rotation = Quaternion<float>.CreateFromAxisAngle(normalizedAxis, radians);
        SetRotation(_rotation * rotation, interpolator);
    }

    public void RotateAxisImmediate(Vector3D<float> axis, float radians, bool setTarget = false)
    {
        var normalizedAxis = Vector3D.Normalize(axis);
        var rotation = Quaternion<float>.CreateFromAxisAngle(normalizedAxis, radians);
        SetCurrentRotation(_rotation * rotation, setTarget);
    }
    
    public void LookAt(Vector3D<float> target, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        LookAt(target, Vector3D<float>.UnitY, interpolator);
    }

    public void LookAtImmediate(Vector3D<float> target, bool setTarget = false)
    {
        LookAtImmediate(target, Vector3D<float>.UnitY, setTarget);
    }
    
    public void LookAt(Vector3D<float> target, Vector3D<float> worldUp, InterpolationFunction<Quaternion<float>>? interpolator = null)
    {
        var rotation = CalculateLookAtRotation(target, worldUp);
        SetRotation(rotation, interpolator);
    }

    public void LookAtImmediate(Vector3D<float> target, Vector3D<float> worldUp, bool setTarget = false)
    {
        var rotation = CalculateLookAtRotation(target, worldUp);
        SetCurrentRotation(rotation, setTarget);
    }

    private Quaternion<float> CalculateLookAtRotation(Vector3D<float> target, Vector3D<float> worldUp)
    {
        var currentPosition = Parent is ITransformable parentTransform 
            ? WorldPosition 
            : _position;
        
        var forward = Vector3D.Normalize(target - currentPosition);
        
        // Handle edge case: if target is same as position, use default forward
        if (float.IsNaN(forward.X) || float.IsNaN(forward.Y) || float.IsNaN(forward.Z))
        {
            return Quaternion<float>.Identity;
        }
        
        // We want to rotate from default forward (-Z) to point toward target
        var defaultForward = -Vector3D<float>.UnitZ;
        
        // Calculate rotation quaternion from defaultForward to forward
        var dot = Vector3D.Dot(defaultForward, forward);
        
        if (dot >= 0.999999f)
        {
            // Vectors are already aligned
            return Quaternion<float>.Identity;
        }
        else if (dot <= -0.999999f)
        {
            // Vectors are opposite, rotate 180° around world up
            return Quaternion<float>.CreateFromAxisAngle(worldUp, MathF.PI);
        }
        
        // General case: rotate around the perpendicular axis
        var axis = Vector3D.Normalize(Vector3D.Cross(defaultForward, forward));
        var angle = MathF.Acos(dot);
        return Quaternion<float>.CreateFromAxisAngle(axis, angle);
    }
    
    /// <summary>
    /// Sets the angular velocity for continuous rotation (radians per second).
    /// Applied automatically during OnComponentUpdate. Call with Zero to stop rotation.
    /// </summary>
    public void SetAngularVelocity(Vector3D<float> angularVelocity)
    {
        _angularVelocity = angularVelocity;
    }
    
    // ==========================================
    // SCALE METHODS
    // ==========================================
    
    public void ScaleBy(Vector3D<float> scaleFactor, InterpolationFunction<Vector3D<float>>? interpolator = null)
    {
        SetScale(new Vector3D<float>(
            _scale.X * scaleFactor.X,
            _scale.Y * scaleFactor.Y,
            _scale.Z * scaleFactor.Z), interpolator);
    }

    public void ScaleByImmediate(Vector3D<float> scaleFactor, bool setTarget = false)
    {
        SetCurrentScale(new Vector3D<float>(
            _scale.X * scaleFactor.X,
            _scale.Y * scaleFactor.Y,
            _scale.Z * scaleFactor.Z), setTarget);
    }
    
    public void ScaleUniform(float scaleFactor, InterpolationFunction<Vector3D<float>>? interpolator = null)
    {
        SetScale(_scale * scaleFactor, interpolator);
    }

    public void ScaleUniformImmediate(float scaleFactor, bool setTarget = false)
    {
        SetCurrentScale(_scale * scaleFactor, setTarget);
    }
    
    // ==========================================
    // CONFIGURATION
    // ==========================================
    
    // OnLoad method is auto-generated from template
    
    /// <summary>
    /// Override AddChild to invalidate child world matrices when added to hierarchy.
    /// </summary>
    public override void AddChild(IComponent child)
    {
        base.AddChild(child);
        
        // Invalidate child's world matrix since it now has a new parent
        if (child is Transformable transformableChild)
        {
            transformableChild.InvalidateWorldMatrix();
        }
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();
        UpdateLocalMatrix(); // Initialize cached matrix
        InvalidateWorldMatrix(); // Ensure world matrix is recalculated on activation
    }
    
    // ==========================================
    // UPDATE (Apply velocities)
    // ==========================================
    
    protected override void OnUpdate(double deltaTime)
    {
        // Apply continuous linear velocity
        if (_velocity != Vector3D<float>.Zero)
        {
            SetPosition(_position + _velocity * (float)deltaTime);
        }
        
        // Apply continuous angular velocity
        if (_angularVelocity != Vector3D<float>.Zero)
        {
            var deltaRotation = Quaternion<float>.CreateFromYawPitchRoll(
                _angularVelocity.Y * (float)deltaTime,
                _angularVelocity.X * (float)deltaTime,
                _angularVelocity.Z * (float)deltaTime);
            SetRotation(_rotation * deltaRotation);
        }
    }
}
