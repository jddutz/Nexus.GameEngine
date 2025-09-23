namespace Main.Data
{
    public class DataBinding
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string SourceProperty { get; }
        public string TargetProperty { get; }
        public BindingModeEnum Mode { get; }
        public IValueConverter? Converter { get; }
        public IBindingValidator? Validator { get; }
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
        public DataBinding(string sourceProperty, string targetProperty, BindingModeEnum mode,
                      IValueConverter? converter = null, IBindingValidator? validator = null)
        {
            SourceProperty = sourceProperty;
            TargetProperty = targetProperty;
            Mode = mode;
            Converter = converter;
            Validator = validator;
        }
    }
}
