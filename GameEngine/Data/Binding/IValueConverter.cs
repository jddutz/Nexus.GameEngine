namespace Main.Data
{
    /// <summary>
    /// Defines value conversion logic for data binding.
    /// </summary>
    public interface IValueConverter
    {
        object? Convert(object? value, Type targetType, object? parameter = null);
        object? ConvertBack(object? value, Type sourceType, object? parameter = null);
        bool CanConvert(Type sourceType, Type targetType);
    }
}
