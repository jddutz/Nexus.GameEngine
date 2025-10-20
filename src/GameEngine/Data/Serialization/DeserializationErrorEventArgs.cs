namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Provides data for deserialization error events.
    /// </summary>
    public class DeserializationErrorEventArgs(ISerializable serializable, Exception exception, string? propertyPath = null, object? sourceData = null) : EventArgs
    {
        public ISerializable Serializable { get; } = serializable;
        public Exception Exception { get; } = exception;
        public string? PropertyPath { get; } = propertyPath;
        public object? SourceData { get; } = sourceData;
        public bool Ignore { get; set; }
    }
}
