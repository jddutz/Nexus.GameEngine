namespace Main.Data
{
    public class BindingUpdateEventArgs : EventArgs
    {
        public DataBinding Binding { get; }
        public string PropertyName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; set; }
        public bool Cancel { get; set; }
        public BindingUpdateEventArgs(DataBinding binding, string propertyName, object? oldValue, object? newValue)
        {
            Binding = binding;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
