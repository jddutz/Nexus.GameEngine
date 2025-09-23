namespace Main.Data
{
    public class DataSourceChangedEventArgs : EventArgs
    {
        public object? OldDataSource { get; }
        public object? NewDataSource { get; }
        public DataSourceChangedEventArgs(object? oldDataSource, object? newDataSource)
        {
            OldDataSource = oldDataSource;
            NewDataSource = newDataSource;
        }
    }
}
