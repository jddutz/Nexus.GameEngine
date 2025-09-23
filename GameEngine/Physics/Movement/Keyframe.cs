namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Represents a keyframe in an animation curve.
/// </summary>
public struct Keyframe
{
    public float Time { get; }
    public float Value { get; }
    public float InTangent { get; }
    public float OutTangent { get; }
    public Keyframe(float time, float value, float inTangent = 0f, float outTangent = 0f)
    {
        Time = time;
        Value = value;
        InTangent = inTangent;
        OutTangent = outTangent;
    }
}
