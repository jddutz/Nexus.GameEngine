using System;

namespace Nexus.GameEngine.Data;

/// <summary>
/// Converts a fraction (0.0-1.0) to a percentage (0-100).
/// </summary>
public class PercentageConverter : IBidirectionalConverter
{
    private const float Factor = 100.0f;

    public object? Convert(object? value)
    {
        if (value == null) return null;

        try
        {
            float floatValue = System.Convert.ToSingle(value);
            return floatValue * Factor;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value)
    {
        if (value == null) return null;

        try
        {
            float floatValue = System.Convert.ToSingle(value);
            return floatValue / Factor;
        }
        catch
        {
            return null;
        }
    }
}
