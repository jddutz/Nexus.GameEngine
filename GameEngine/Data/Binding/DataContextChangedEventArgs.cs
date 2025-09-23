namespace Main.Data
{
    public class DataContextChangedEventArgs : EventArgs
    {
        public object? OldDataContext { get; }
        public object? NewDataContext { get; }
        public DataContextChangedEventArgs(object? oldDataContext, object? newDataContext)
        {
            OldDataContext = oldDataContext;
            NewDataContext = newDataContext;
        }
    }
}
