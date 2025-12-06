using System;

namespace Nexus.GameEngine.Data;

/// <summary>
/// Converts a value to a formatted string.
/// </summary>
public class StringFormatConverter : IValueConverter
{
    private readonly string _format;

    public StringFormatConverter(string format)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public object? Convert(object? value)
    {
        if (value == null)
        {
            // If value is null, we can either return null or format it as empty string/null placeholder
            // Based on tests, we expect "Value: " for null input if format is "Value: {0}"
            // string.Format handles null arguments by treating them as empty string usually? 
            // Actually string.Format("{0}", null) throws or prints empty string?
            // string.Format("Value: {0}", null) -> "Value: "
            return string.Format(_format, "");
        }

        try
        {
            return string.Format(_format, value);
        }
        catch (FormatException)
        {
            // Fallback or log? Interface says "Implementations MUST NOT throw exceptions"
            return value?.ToString();
        }
    }
}
