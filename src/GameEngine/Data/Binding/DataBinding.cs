namespace Nexus.GameEngine.Data.Binding
{
    public class DataBinding(string sourceProperty, string targetProperty, BindingModeEnum mode,
                  IValueConverter? converter = null, IBindingValidator? validator = null)
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string SourceProperty { get; } = sourceProperty;
        public string TargetProperty { get; } = targetProperty;
        public BindingModeEnum Mode { get; } = mode;
        public IValueConverter? Converter { get; } = converter;
        public IBindingValidator? Validator { get; } = validator;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedTime { get; } = DateTime.UtcNow;
        public DateTime? LastUpdateTime { get; private set; }
        public int UpdateCount { get; private set; }
        public Dictionary<string, object> Metadata { get; } = [];
        internal void RecordUpdate()
        {
            LastUpdateTime = DateTime.UtcNow;
            UpdateCount++;
        }
    }
}
