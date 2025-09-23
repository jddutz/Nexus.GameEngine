namespace Main.Data
{
    /// <summary>
    /// Provides data for deserialization error events.
    /// </summary>
    public class DeserializationErrorEventArgs : EventArgs
    {
        public ISerializable Serializable { get; }
        public Exception Exception { get; }
        public string? PropertyPath { get; }
        public object? SourceData { get; }
        public bool Ignore { get; set; }
        public DeserializationErrorEventArgs(ISerializable serializable, Exception exception, string? propertyPath = null, object? sourceData = null)
        {
            Serializable = serializable;
            Exception = exception;
            PropertyPath = propertyPath;
            SourceData = sourceData;
        }
    }
}
