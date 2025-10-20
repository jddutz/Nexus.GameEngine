namespace Nexus.GameEngine.Data.Binding
{
    public class DataContextChangedEventArgs(object? oldDataContext, object? newDataContext) : EventArgs
    {
        public object? OldDataContext { get; } = oldDataContext;
        public object? NewDataContext { get; } = newDataContext;
    }
}
