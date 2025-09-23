namespace Main.Data
{
    public class BindingErrorEventArgs : EventArgs
    {
        public BindingError Error { get; }
        public DataBinding? Binding { get; }
        public bool Ignore { get; set; }
        public BindingErrorEventArgs(BindingError error, DataBinding? binding = null)
        {
            Error = error;
            Binding = binding;
        }
    }
}
