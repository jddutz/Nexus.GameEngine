namespace Nexus.GameEngine.Data.Binding
{
    public class BindingRemovedEventArgs(DataBinding binding) : EventArgs
    {
        public DataBinding Binding { get; } = binding;
    }
}
