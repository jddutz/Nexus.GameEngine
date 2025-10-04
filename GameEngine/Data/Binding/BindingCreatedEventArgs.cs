namespace Main.Data
{
    public class BindingCreatedEventArgs(DataBinding binding) : EventArgs
    {
        public DataBinding Binding { get; } = binding;
    }
}
