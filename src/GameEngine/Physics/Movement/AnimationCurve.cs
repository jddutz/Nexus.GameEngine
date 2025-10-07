namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Represents an animation curve for custom interpolation.
/// </summary>
public class AnimationCurve
{
    public List<Keyframe> Keyframes { get; }
    public WrapModeEnum PreWrapModeEnum { get; set; }
    public WrapModeEnum PostWrapModeEnum { get; set; }
    public AnimationCurve()
    {
        Keyframes = [];
        PreWrapModeEnum = WrapModeEnum.Clamp;
        PostWrapModeEnum = WrapModeEnum.Clamp;
    }
    public float Evaluate(float time)
    {
        if (Keyframes.Count == 0) return 0f;
        if (Keyframes.Count == 1) return Keyframes[0].Value;
        for (int i = 0; i < Keyframes.Count - 1; i++)
        {
            if (time >= Keyframes[i].Time && time <= Keyframes[i + 1].Time)
            {
                float t = (time - Keyframes[i].Time) / (Keyframes[i + 1].Time - Keyframes[i].Time);
                return Keyframes[i].Value + t * (Keyframes[i + 1].Value - Keyframes[i].Value);
            }
        }
        return Keyframes[Keyframes.Count - 1].Value;
    }
    public void AddKey(float time, float value)
    {
        Keyframes.Add(new Keyframe(time, value));
        Keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
    }
}
