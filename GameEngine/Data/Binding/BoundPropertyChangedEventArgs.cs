namespace Main.Data
{
    public class BoundPropertyChangedEventArgs : EventArgs
    {
        public string PropertyName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }
        public DataBinding Binding { get; }
        public BoundPropertyChangedEventArgs(string propertyName, object? oldValue, object? newValue, DataBinding binding)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
            Binding = binding;
        }
    }
}
