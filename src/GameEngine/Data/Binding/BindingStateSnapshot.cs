namespace Main.Data
{
    public class BindingStateSnapshot(IReadOnlyList<DataBinding> bindings, IReadOnlyList<BindingError> errors, Dictionary<string, object?> propertyValues)
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public IReadOnlyList<DataBinding> Bindings { get; } = bindings;
        public IReadOnlyList<BindingError> Errors { get; } = errors;
        public Dictionary<string, object?> PropertyValues { get; } = propertyValues;
    }
}
