namespace Nexus.GameEngine.Data.Binding
{
    public class DataSourceChangedEventArgs(object? oldDataSource, object? newDataSource) : EventArgs
    {
        public object? OldDataSource { get; } = oldDataSource;
        public object? NewDataSource { get; } = newDataSource;
    }
}
