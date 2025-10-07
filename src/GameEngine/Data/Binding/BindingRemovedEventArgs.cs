namespace Main.Data
{
    public class BindingRemovedEventArgs(DataBinding binding) : EventArgs
    {
        public DataBinding Binding { get; } = binding;
    }
}
