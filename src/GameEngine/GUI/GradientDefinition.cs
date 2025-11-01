namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines a gradient with multiple color stops.
/// Supports up to 32 color stops for high-quality gradients.
/// </summary>
public record GradientDefinition
{
    /// <summary>
    /// Maximum number of gradient stops supported (limited by UBO size).
    /// </summary>
    public const int MaxStops = 32;
    
    /// <summary>
    /// Minimum number of gradient stops required.
    /// </summary>
    public const int MinStops = 2;
    
    /// <summary>
    /// Array of gradient stops defining colors at specific positions.
    /// Must be sorted by position in ascending order.
    /// Defaults to a simple black-to-white gradient.
    /// </summary>
    public GradientStop[] Stops { get; init; } = 
    [
        new GradientStop(0.0f, new Vector4D<float>(0, 0, 0, 1)),
        new GradientStop(1.0f, new Vector4D<float>(1, 1, 1, 1))
    ];
    
    /// <summary>
    /// Creates a simple two-color gradient.
    /// </summary>
    public static GradientDefinition TwoColor(Vector4D<float> startColor, Vector4D<float> endColor)
    {
        return new GradientDefinition
        {
            Stops = 
            [
                new GradientStop(0.0f, startColor),
                new GradientStop(1.0f, endColor)
            ]
        };
    }
    
    /// <summary>
    /// Creates a three-color gradient with a middle color.
    /// </summary>
    public static GradientDefinition ThreeColor(Vector4D<float> startColor, Vector4D<float> middleColor, Vector4D<float> endColor, float middlePosition = 0.5f)
    {
        return new GradientDefinition
        {
            Stops = 
            [
                new GradientStop(0.0f, startColor),
                new GradientStop(middlePosition, middleColor),
                new GradientStop(1.0f, endColor)
            ]
        };
    }
    
    /// <summary>
    /// Validates the gradient definition.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if gradient is invalid.</exception>
    public void Validate()
    {
        if (Stops == null || Stops.Length < MinStops)
            throw new ArgumentException($"Gradient must have at least {MinStops} stops");
            
        if (Stops.Length > MaxStops)
            throw new ArgumentException($"Gradient cannot have more than {MaxStops} stops");
        
        // Validate each stop
        foreach (var stop in Stops)
        {
            stop.Validate();
        }
        
        // Verify stops are sorted by position
        for (int i = 1; i < Stops.Length; i++)
        {
            if (Stops[i].Position < Stops[i - 1].Position)
            {
                throw new ArgumentException(
                    $"Gradient stops must be sorted by position. " +
                    $"Stop {i} has position {Stops[i].Position} which is less than stop {i-1} position {Stops[i-1].Position}");
            }
        }
    }
}
