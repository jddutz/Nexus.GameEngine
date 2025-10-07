namespace Main.Data
{
    public class DataContextChangedEventArgs(object? oldDataContext, object? newDataContext) : EventArgs
    {
        public object? OldDataContext { get; } = oldDataContext;
        public object? NewDataContext { get; } = newDataContext;
    }
}
