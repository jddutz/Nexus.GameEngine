using Nexus.GameEngine.Animation;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Runtime component that provides position, rotation, and scale in 3D space.
/// Implements the ITransformable interface with full animation support.
/// Uses Vulkan/Silk.NET conventions: -Z forward, +Y up, +X right (right-handed).
/// </summary>
public partial class Transformable : RuntimeComponent, ITransformable
{
    /// <summary>
    /// Template for configuring Transformable components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Initial local position relative to parent.
        /// Default: (0, 0, 0)
        /// </summary>
        public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
        
        /// <summary>
        /// Initial local rotation.
        /// Default: Identity (no rotation, facing -Z, up is +Y)
        /// </summary>
        public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;
        
        /// <summary>
        /// Initial local scale.
        /// Default: (1, 1, 1)
        /// </summary>
        public Vector3D<float> Scale { get; set; } = new Vector3D<float>(1f, 1f, 1f);
    }
    
    // ==========================================
    // ANIMATED PROPERTIES (with ComponentProperty attribute)
    // ==========================================
    
    [ComponentProperty(Duration = 0.2f, Interpolation = InterpolationMode.Linear)]
    private Vector3D<float> _position = Vector3D<float>.Zero;
    
    [ComponentProperty(Duration = 0.2f, Interpolation = InterpolationMode.Linear)]
    private Quaternion<float> _rotation = Quaternion<float>.Identity;
    
    [ComponentProperty(Duration = 0.2f, Interpolation = InterpolationMode.Linear)]
    private Vector3D<float> _scale = new Vector3D<float>(1f, 1f, 1f);
    
    // ==========================================
    // TRANSFORM MATRICES
    // ==========================================
    
    public Matrix4X4<float> LocalMatrix
    {
        get
        {
            // Standard SRT (Scale-Rotation-Translation) matrix composition
            // Silk.NET uses column-major matrices with column vectors
            // This scales first, then rotates, then translates
            // For hierarchical transforms: WorldMatrix = ParentWorld * ChildLocal
            return Matrix4X4.CreateScale(_scale) *
                   Matrix4X4.CreateFromQuaternion(_rotation) *
                   Matrix4X4.CreateTranslation(_position);
        }
    }
    
    public Matrix4X4<float> WorldMatrix
    {
        get
        {
            // Hierarchical transform composition: WorldMatrix = ChildLocal * ParentWorld
            // With S*R*T matrices, this order correctly transforms the child's local position
            // by the parent's rotation, then adds the parent's world position
            if (Parent is ITransformable parentTransform)
                return LocalMatrix * parentTransform.WorldMatrix;
            else
                return LocalMatrix; // Root object: local space = world space
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
    
    public void SetPosition(Vector3D<float> position)
    {
        Position = position;
    }
    
    public void Translate(Vector3D<float> delta)
    {
        Position += delta;
    }
    
    public void TranslateLocal(Vector3D<float> delta)
    {
        // Transform delta from local space to world space using current rotation
        var worldDelta = Vector3D.Transform(delta, _rotation);
        Position += worldDelta;
    }
    
    // ==========================================
    // ROTATION METHODS
    // ==========================================
    
    public void SetRotation(Quaternion<float> rotation)
    {
        Rotation = rotation;
    }
    
    public void RotateX(float radians)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, radians);
        Rotation = _rotation * rotation;
    }
    
    public void RotateY(float radians)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, radians);
        Rotation = _rotation * rotation;
    }
    
    public void RotateZ(float radians)
    {
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, radians);
        Rotation = _rotation * rotation;
    }
    
    public void RotateAxis(Vector3D<float> axis, float radians)
    {
        var normalizedAxis = Vector3D.Normalize(axis);
        var rotation = Quaternion<float>.CreateFromAxisAngle(normalizedAxis, radians);
        Rotation = _rotation * rotation;
    }
    
    public void LookAt(Vector3D<float> target)
    {
        LookAt(target, Vector3D<float>.UnitY);
    }
    
    public void LookAt(Vector3D<float> target, Vector3D<float> worldUp)
    {
        var currentPosition = Parent is ITransformable parentTransform 
            ? WorldPosition 
            : _position;
        
        var forward = Vector3D.Normalize(target - currentPosition);
        
        // Handle edge case: if target is same as position, use default forward
        if (float.IsNaN(forward.X) || float.IsNaN(forward.Y) || float.IsNaN(forward.Z))
        {
            Rotation = Quaternion<float>.Identity;
            return;
        }
        
        // We want to rotate from default forward (-Z) to point toward target
        var defaultForward = -Vector3D<float>.UnitZ;
        
        // Calculate rotation quaternion from defaultForward to forward
        var dot = Vector3D.Dot(defaultForward, forward);
        
        if (dot >= 0.999999f)
        {
            // Vectors are already aligned
            Rotation = Quaternion<float>.Identity;
            return;
        }
        else if (dot <= -0.999999f)
        {
            // Vectors are opposite, rotate 180° around world up
            Rotation = Quaternion<float>.CreateFromAxisAngle(worldUp, MathF.PI);
            return;
        }
        
        // General case: rotate around the perpendicular axis
        var axis = Vector3D.Normalize(Vector3D.Cross(defaultForward, forward));
        var angle = MathF.Acos(dot);
        Rotation = Quaternion<float>.CreateFromAxisAngle(axis, angle);
    }
    
    // ==========================================
    // SCALE METHODS
    // ==========================================
    
    public void SetScale(Vector3D<float> scale)
    {
        Scale = scale;
    }
    
    public void ScaleBy(Vector3D<float> scaleFactor)
    {
        Scale = new Vector3D<float>(
            _scale.X * scaleFactor.X,
            _scale.Y * scaleFactor.Y,
            _scale.Z * scaleFactor.Z);
    }
    
    public void ScaleUniform(float scaleFactor)
    {
        Scale = _scale * scaleFactor;
    }
    
    // ==========================================
    // CONFIGURATION
    // ==========================================
    
    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            Position = template.Position;
            Rotation = template.Rotation;
            Scale = template.Scale;
        }
    }
}
