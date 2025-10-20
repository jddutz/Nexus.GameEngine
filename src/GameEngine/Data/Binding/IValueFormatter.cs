namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Defines value formatting logic for display purposes.
    /// </summary>
    public interface IValueFormatter
    {
        string Format(object? value, string? format = null);
        bool CanFormat(Type valueType);
    }
}
