namespace Main.Data
{
    /// <summary>
    /// Provides data for serialization error events.
    /// </summary>
    public class SerializationErrorEventArgs : EventArgs
    {
        public ISerializable Serializable { get; }
        public Exception Exception { get; }
        public string? PropertyPath { get; }
        public bool Ignore { get; set; }
        public SerializationErrorEventArgs(ISerializable serializable, Exception exception, string? propertyPath = null)
        {
            Serializable = serializable;
            Exception = exception;
            PropertyPath = propertyPath;
        }
    }
}
