namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Represents a keyframe in an animation curve.
/// </summary>
public struct Keyframe(float time, float value, float inTangent = 0f, float outTangent = 0f)
{
    public float Time { get; } = time;
    public float Value { get; } = value;
    public float InTangent { get; } = inTangent;
    public float OutTangent { get; } = outTangent;
}
