namespace Nexus.GameEngine.Data.Binding
{
    public class BindingUpdateEventArgs(DataBinding binding, string propertyName, object? oldValue, object? newValue) : EventArgs
    {
        public DataBinding Binding { get; } = binding;
        public string PropertyName { get; } = propertyName;
        public object? OldValue { get; } = oldValue;
        public object? NewValue { get; set; } = newValue;
        public bool Cancel { get; set; }
    }
}
