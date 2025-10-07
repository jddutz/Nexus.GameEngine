namespace Main.Data
{
    /// <summary>
    /// Provides data for deserialization events.
    /// </summary>
    public class DeserializationEventArgs(ISerializable serializable, SerializationFormatEnum format, SerializationContext context, object sourceData) : EventArgs
    {
        public ISerializable Serializable { get; } = serializable;
        public SerializationFormatEnum Format { get; } = format;
        public SerializationContext Context { get; } = context;
        public object SourceData { get; } = sourceData;
        public bool Cancel { get; set; }
    }
}
