namespace Main.Data
{
    public class BindingStateSnapshot
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public IReadOnlyList<DataBinding> Bindings { get; }
        public IReadOnlyList<BindingError> Errors { get; }
        public Dictionary<string, object?> PropertyValues { get; }
        public BindingStateSnapshot(IReadOnlyList<DataBinding> bindings, IReadOnlyList<BindingError> errors, Dictionary<string, object?> propertyValues)
        {
            Bindings = bindings;
            Errors = errors;
            PropertyValues = propertyValues;
        }
    }
}
