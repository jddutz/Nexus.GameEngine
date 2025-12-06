using System;

namespace Nexus.GameEngine.Data;

/// <summary>
/// Multiplies a numeric value by a factor.
/// </summary>
public class MultiplyConverter : IBidirectionalConverter
{
    private readonly float _factor;

    public MultiplyConverter(float factor)
    {
        _factor = factor;
    }

    public object? Convert(object? value)
    {
        if (value == null) return null;

        try
        {
            // Handle various numeric types by converting to float
            float floatValue = System.Convert.ToSingle(value);
            return floatValue * _factor;
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
            
            // Avoid division by zero
            if (Math.Abs(_factor) < float.Epsilon)
            {
                return 0.0f; 
            }

            return floatValue / _factor;
        }
        catch
        {
            return null;
        }
    }
}
