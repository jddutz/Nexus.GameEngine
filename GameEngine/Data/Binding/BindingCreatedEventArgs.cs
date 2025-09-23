namespace Main.Data
{
    public class BindingCreatedEventArgs : EventArgs
    {
        public DataBinding Binding { get; }
        public BindingCreatedEventArgs(DataBinding binding)
        {
            Binding = binding;
        }
    }
}
