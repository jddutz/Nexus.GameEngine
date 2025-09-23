namespace Main.Data
{
    public class BindingRemovedEventArgs : EventArgs
    {
        public DataBinding Binding { get; }
        public BindingRemovedEventArgs(DataBinding binding)
        {
            Binding = binding;
        }
    }
}
