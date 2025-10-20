using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using Xunit;

namespace Tests;

public class TransformableTests
{
    private const float Epsilon = 0.0001f;

    /// <summary>
    /// Helper to create an activated Transformable for testing.
    /// Activated components apply property changes immediately during OnActivate.
    /// </summary>
    private static Transformable CreateActivatedTransformable()
    {
        var transform = new Transformable();
        transform.Activate(); // This makes property changes apply after UpdateAnimations
        return transform;
    }

    private static bool ApproximatelyEqual(float a, float b, float epsilon = Epsilon)
    {
        return MathF.Abs(a - b) < epsilon;
    }

    private static bool ApproximatelyEqual(Vector3D<float> a, Vector3D<float> b, float epsilon = Epsilon)
    {
        return ApproximatelyEqual(a.X, b.X, epsilon) &&
               ApproximatelyEqual(a.Y, b.Y, epsilon) &&
               ApproximatelyEqual(a.Z, b.Z, epsilon);
    }

    private static bool ApproximatelyEqual(Quaternion<float> a, Quaternion<float> b, float epsilon = Epsilon)
    {
        return ApproximatelyEqual(a.X, b.X, epsilon) &&
               ApproximatelyEqual(a.Y, b.Y, epsilon) &&
               ApproximatelyEqual(a.Z, b.Z, epsilon) &&
               ApproximatelyEqual(a.W, b.W, epsilon);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var transform = new Transformable();

        // Assert
        Assert.Equal(Vector3D<float>.Zero, transform.Position);
        Assert.Equal(Quaternion<float>.Identity, transform.Rotation);
        Assert.Equal(new Vector3D<float>(1f, 1f, 1f), transform.Scale);
    }

    [Fact]
    public void SetPosition_UpdatesPosition()
    {
        // Arrange
        var transform = new Transformable();
        var newPosition = new Vector3D<float>(1f, 2f, 3f);

        // Act
        transform.SetPosition(newPosition);

        // Assert - ComponentProperty applies immediately when component is not active
        Assert.Equal(newPosition, transform.Position);
    }

    [Fact]
    public void SetRotation_UpdatesRotation()
    {
        // Arrange
        var transform = new Transformable();
        var rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, MathF.PI / 2);

        // Act
        transform.SetRotation(rotation);

        // Assert
        Assert.True(ApproximatelyEqual(rotation, transform.Rotation));
    }

    [Fact]
    public void SetScale_UpdatesScale()
    {
        // Arrange
        var transform = new Transformable();
        var newScale = new Vector3D<float>(2f, 3f, 4f);

        // Act
        transform.SetScale(newScale);

        // Assert
        Assert.Equal(newScale, transform.Scale);
    }

    [Fact]
    public void Translate_MovesInWorldSpace()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetPosition(new Vector3D<float>(1f, 0f, 0f));
        var delta = new Vector3D<float>(0f, 1f, 0f);

        // Act
        transform.Translate(delta);

        // Assert
        Assert.Equal(new Vector3D<float>(1f, 1f, 0f), transform.Position);
    }

    [Fact]
    public void TranslateLocal_MovesInLocalSpace()
    {
        // Arrange
        var transform = new Transformable();
        // Rotate 90 degrees around Y (CCW when viewed from above, turns to face -X)
        transform.RotateY(MathF.PI / 2);
        
        // Act
        // Move forward in local space (forward is now -X in world space)
        transform.TranslateLocal(new Vector3D<float>(0f, 0f, -1f));

        // Assert - should have moved in -X direction
        Assert.True(ApproximatelyEqual(new Vector3D<float>(-1f, 0f, 0f), transform.Position));
    }

    [Fact]
    public void RotateX_RotatesAroundXAxis()
    {
        // Arrange
        var transform = new Transformable();
        var angle = MathF.PI / 4; // 45 degrees

        // Act
        transform.RotateX(angle);

        // Assert
        var expected = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, angle);
        Assert.True(ApproximatelyEqual(expected, transform.Rotation));
    }

    [Fact]
    public void RotateY_RotatesAroundYAxis()
    {
        // Arrange
        var transform = new Transformable();
        var angle = MathF.PI / 2; // 90 degrees

        // Act
        transform.RotateY(angle);

        // Assert
        var expected = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, angle);
        Assert.True(ApproximatelyEqual(expected, transform.Rotation));
    }

    [Fact]
    public void RotateZ_RotatesAroundZAxis()
    {
        // Arrange
        var transform = new Transformable();
        var angle = MathF.PI / 3; // 60 degrees

        // Act
        transform.RotateZ(angle);

        // Assert
        var expected = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, angle);
        Assert.True(ApproximatelyEqual(expected, transform.Rotation));
    }

    [Fact]
    public void RotateAxis_RotatesAroundArbitraryAxis()
    {
        // Arrange
        var transform = new Transformable();
        var axis = Vector3D.Normalize(new Vector3D<float>(1f, 1f, 0f));
        var angle = MathF.PI / 6; // 30 degrees

        // Act
        transform.RotateAxis(axis, angle);

        // Assert
        var expected = Quaternion<float>.CreateFromAxisAngle(axis, angle);
        Assert.True(ApproximatelyEqual(expected, transform.Rotation));
    }

    [Fact]
    public void ScaleBy_MultipliesScale()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetScale(new Vector3D<float>(2f, 3f, 4f));
        var scaleFactor = new Vector3D<float>(0.5f, 2f, 1.5f);

        // Act
        transform.ScaleBy(scaleFactor);

        // Assert
        Assert.Equal(new Vector3D<float>(1f, 6f, 6f), transform.Scale);
    }

    [Fact]
    public void ScaleUniform_ScalesAllAxesEqually()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetScale(new Vector3D<float>(2f, 3f, 4f));

        // Act
        transform.ScaleUniform(2f);

        // Assert
        Assert.Equal(new Vector3D<float>(4f, 6f, 8f), transform.Scale);
    }

    [Fact]
    public void Forward_ReturnsNegativeZByDefault()
    {
        // Arrange
        var transform = new Transformable();

        // Act
        var forward = transform.Forward;

        // Assert
        Assert.True(ApproximatelyEqual(-Vector3D<float>.UnitZ, forward));
    }

    [Fact]
    public void Right_ReturnsPositiveXByDefault()
    {
        // Arrange
        var transform = new Transformable();

        // Act
        var right = transform.Right;

        // Assert
        Assert.True(ApproximatelyEqual(Vector3D<float>.UnitX, right));
    }

    [Fact]
    public void Up_ReturnsPositiveYByDefault()
    {
        // Arrange
        var transform = new Transformable();

        // Act
        var up = transform.Up;

        // Assert
        Assert.True(ApproximatelyEqual(Vector3D<float>.UnitY, up));
    }

    [Fact]
    public void Forward_UpdatesWithRotation()
    {
        // Arrange
        var transform = new Transformable();
        
        // Act - Rotate 90 degrees around Y (CCW from above, turns to face -X)
        transform.RotateY(MathF.PI / 2);
        var forward = transform.Forward;

        // Assert - Should now point in -X direction
        Assert.True(ApproximatelyEqual(-Vector3D<float>.UnitX, forward, 0.01f));
    }

    [Fact]
    public void LocalMatrix_ReflectsTransform()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetPosition(new Vector3D<float>(1f, 2f, 3f));
        transform.SetRotation(Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, MathF.PI / 4));
        transform.SetScale(new Vector3D<float>(2f, 2f, 2f));

        // Act
        var localMatrix = transform.LocalMatrix;

        // Assert - Build expected matrix using SRT order (Scale * Rotation * Translation)
        // This is the standard order expected by Silk.NET and used in scene graphs
        var expected =
            Matrix4X4.CreateScale(new Vector3D<float>(2f, 2f, 2f)) *
            Matrix4X4.CreateFromQuaternion(Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, MathF.PI / 4)) *
            Matrix4X4.CreateTranslation(new Vector3D<float>(1f, 2f, 3f));

        // Compare matrices element-wise
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                var expectedVal = i switch
                {
                    0 => j switch { 0 => expected.M11, 1 => expected.M12, 2 => expected.M13, 3 => expected.M14, _ => 0 },
                    1 => j switch { 0 => expected.M21, 1 => expected.M22, 2 => expected.M23, 3 => expected.M24, _ => 0 },
                    2 => j switch { 0 => expected.M31, 1 => expected.M32, 2 => expected.M33, 3 => expected.M34, _ => 0 },
                    3 => j switch { 0 => expected.M41, 1 => expected.M42, 2 => expected.M43, 3 => expected.M44, _ => 0 },
                    _ => 0
                };
                var actualVal = i switch
                {
                    0 => j switch { 0 => localMatrix.M11, 1 => localMatrix.M12, 2 => localMatrix.M13, 3 => localMatrix.M14, _ => 0 },
                    1 => j switch { 0 => localMatrix.M21, 1 => localMatrix.M22, 2 => localMatrix.M23, 3 => localMatrix.M24, _ => 0 },
                    2 => j switch { 0 => localMatrix.M31, 1 => localMatrix.M32, 2 => localMatrix.M33, 3 => localMatrix.M34, _ => 0 },
                    3 => j switch { 0 => localMatrix.M41, 1 => localMatrix.M42, 2 => localMatrix.M43, 3 => localMatrix.M44, _ => 0 },
                    _ => 0
                };
                Assert.True(ApproximatelyEqual(expectedVal, actualVal), $"Matrix mismatch at [{i},{j}]: expected {expectedVal}, got {actualVal}");
            }
    }

    [Fact]
    public void WorldMatrix_EqualsLocalMatrixWhenNoParent()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetPosition(new Vector3D<float>(1f, 2f, 3f));

        // Act
        var worldMatrix = transform.WorldMatrix;
        var localMatrix = transform.LocalMatrix;

        // Assert
        Assert.Equal(localMatrix, worldMatrix);
    }

    [Fact]
    public void WorldMatrix_CombinesParentTransform()
    {
        // Arrange
        var parent = new Transformable();
        parent.SetPosition(new Vector3D<float>(10f, 0f, 0f));
        parent.SetRotation(Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, MathF.PI / 2));

        var child = new Transformable();
        child.SetPosition(new Vector3D<float>(0f, 0f, -5f)); // 5 units forward in parent's local space

        // Act
        parent.AddChild(child);

        // Assert - Child should be at (0, 0, -5) local: +X => +Z, +Y => +Y, -Z (forward) => -X
        Assert.True(ApproximatelyEqual(new Vector3D<float>(5f, 0f, 0f), child.WorldPosition, 0.01f));
    }

    [Fact]
    public void WorldPosition_AccountsForParentTransform()
    {
        // Arrange
        var parent = new Transformable();
        parent.SetPosition(new Vector3D<float>(5f, 0f, 0f));

        var child = new Transformable();
        child.SetPosition(new Vector3D<float>(0f, 3f, 0f));
        parent.AddChild(child);

        // Act
        var worldPos = child.WorldPosition;

        // Assert
        Assert.True(ApproximatelyEqual(new Vector3D<float>(5f, 3f, 0f), worldPos));
    }

    [Fact]
    public void WorldForward_AccountsForParentRotation()
    {
        // Arrange
        var parent = new Transformable();
        parent.RotateY(MathF.PI / 2); // Turn parent 90 degrees CCW (faces -X)

        var child = new Transformable();
        // Child has no rotation, should inherit parent's rotation
        parent.AddChild(child);

        // Act
        var worldForward = child.WorldForward;

        // Assert - Should point in -X direction (parent's forward)
        Assert.True(ApproximatelyEqual(-Vector3D<float>.UnitX, worldForward, 0.01f));
    }

    [Fact]
    public void LookAt_OrientsFacingTarget()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetPosition(Vector3D<float>.Zero);
        var target = new Vector3D<float>(10f, 0f, 0f);

        // Act
        transform.LookAt(target);

        // Assert - Forward should point toward target (+X)
        var forward = transform.Forward;
        Assert.True(ApproximatelyEqual(Vector3D.Normalize(target), forward, 0.01f));
    }

    [Fact]
    public void LookAt_WithWorldUp_OrientsFacingTargetWithCorrectUp()
    {
        // Arrange
        var transform = new Transformable();
        transform.SetPosition(Vector3D<float>.Zero);
        var target = new Vector3D<float>(0f, 10f, 0f);
        var worldUp = Vector3D<float>.UnitZ;

        // Act
        transform.LookAt(target, worldUp);

        // Assert - Forward should point toward target (+Y)
        var forward = transform.Forward;
        Assert.True(ApproximatelyEqual(Vector3D.Normalize(target), forward, 0.01f));
    }

    [Fact]
    public void ParentChildHierarchy_ThreeLevelsDeep()
    {
        // Arrange - Create grandparent → parent → child hierarchy
        var grandparent = new Transformable();
        grandparent.SetPosition(new Vector3D<float>(100f, 0f, 0f));

        var parent = new Transformable();
        parent.SetPosition(new Vector3D<float>(10f, 0f, 0f));
        grandparent.AddChild(parent);

        var child = new Transformable();
        child.SetPosition(new Vector3D<float>(1f, 0f, 0f));
        parent.AddChild(child);

        // Act
        var worldPos = child.WorldPosition;

        // Assert - Should be sum of all positions
        Assert.True(ApproximatelyEqual(new Vector3D<float>(111f, 0f, 0f), worldPos));
    }

    [Fact]
    public void Template_ConfiguresInitialTransform()
    {
        // Arrange
        var template = new Transformable.Template
        {
            Position = new Vector3D<float>(5f, 10f, 15f),
            Rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, MathF.PI / 4),
            Scale = new Vector3D<float>(2f, 3f, 4f)
        };

        // Act
        var transform = new Transformable();
        transform.Configure(template);

        // Assert
        Assert.Equal(template.Position, transform.Position);
        Assert.True(ApproximatelyEqual(template.Rotation, transform.Rotation));
        Assert.Equal(template.Scale, transform.Scale);
    }
}
