namespace Main.Data
{
    public class BoundPropertyChangedEventArgs(string propertyName, object? oldValue, object? newValue, DataBinding binding) : EventArgs
    {
        public string PropertyName { get; } = propertyName;
        public object? OldValue { get; } = oldValue;
        public object? NewValue { get; } = newValue;
        public DataBinding Binding { get; } = binding;
    }
}
