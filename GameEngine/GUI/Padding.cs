namespace Nexus.GameEngine.GUI;

/// <summary>
/// Represents padding (inner spacing) around layout content
/// </summary>
public record Padding(float Left, float Top, float Right, float Bottom)
{
    public static Padding Zero => new(0, 0, 0, 0);
    public static Padding All(float value) => new(value, value, value, value);
    public static Padding Horizontal(float value) => new(value, 0, value, 0);
    public static Padding Vertical(float value) => new(0, value, 0, value);

    public Padding(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Padding(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
}