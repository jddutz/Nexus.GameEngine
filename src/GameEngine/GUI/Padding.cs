namespace Nexus.GameEngine.GUI;

/// <summary>
/// Represents padding (inner spacing) around layout content
/// </summary>
public record Padding(int Left, int Top, int Right, int Bottom)
{
    public static Padding Zero => new(0, 0, 0, 0);
    public static Padding All(int value) => new(value, value, value, value);
    public static Padding Horizontal(int value) => new(value, 0, value, 0);
    public static Padding Vertical(int value) => new(0, value, 0, value);

    public Padding(int uniform) : this(uniform, uniform, uniform, uniform) { }
    public Padding(int horizontal, int vertical) : this(horizontal, vertical, horizontal, vertical) { }
}