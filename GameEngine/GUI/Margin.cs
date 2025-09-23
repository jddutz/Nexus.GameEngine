namespace Nexus.GameEngine.GUI;

/// <summary>
/// Represents margin (outer spacing) around layout container
/// </summary>
public record Margin(float Left, float Top, float Right, float Bottom)
{
    public static Margin Zero => new(0, 0, 0, 0);
    public static Margin All(float value) => new(value, value, value, value);
    public static Margin Horizontal(float value) => new(value, 0, value, 0);
    public static Margin Vertical(float value) => new(0, value, 0, value);

    public Margin(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Margin(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
}