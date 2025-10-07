namespace Main.Data
{
    public class BindingErrorEventArgs(BindingError error, DataBinding? binding = null) : EventArgs
    {
        public BindingError Error { get; } = error;
        public DataBinding? Binding { get; } = binding;
        public bool Ignore { get; set; }
    }
}
