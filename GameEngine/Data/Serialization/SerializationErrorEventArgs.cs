namespace Main.Data
{
    /// <summary>
    /// Provides data for serialization error events.
    /// </summary>
    public class SerializationErrorEventArgs(ISerializable serializable, Exception exception, string? propertyPath = null) : EventArgs
    {
        public ISerializable Serializable { get; } = serializable;
        public Exception Exception { get; } = exception;
        public string? PropertyPath { get; } = propertyPath;
        public bool Ignore { get; set; }
    }
}
