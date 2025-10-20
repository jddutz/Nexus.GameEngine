namespace Nexus.GameEngine.Data.Binding
{
    public class BindingCreatedEventArgs(DataBinding binding) : EventArgs
    {
        public DataBinding Binding { get; } = binding;
    }
}
