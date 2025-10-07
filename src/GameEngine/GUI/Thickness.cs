namespace Nexus.GameEngine.GUI;

/// <summary>
/// Represents the thickness of a border
/// </summary>
public record Thickness(float Left, float Top, float Right, float Bottom)
{
    public static Thickness Zero => new(0, 0, 0, 0);
    public static Thickness All(float value) => new(value, value, value, value);
    public static Thickness Horizontal(float value) => new(value, 0, value, 0);
    public static Thickness Vertical(float value) => new(0, value, 0, value);

    public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
}