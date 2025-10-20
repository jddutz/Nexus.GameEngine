namespace Nexus.GameEngine.Data.Binding
{
    public class BindingConfiguration
    {
        public List<BindingDefinition> Bindings { get; set; } = [];
        public BindingModeEnum DefaultMode { get; set; } = BindingModeEnum.OneWay;
        public Dictionary<string, object> Settings { get; set; } = [];
    }
    public class BindingDefinition
    {
        public string SourceProperty { get; set; } = string.Empty;
        public string TargetProperty { get; set; } = string.Empty;
        public BindingModeEnum Mode { get; set; } = BindingModeEnum.OneWay;
        public string? ConverterType { get; set; }
        public string? ValidatorType { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = [];
    }
}
