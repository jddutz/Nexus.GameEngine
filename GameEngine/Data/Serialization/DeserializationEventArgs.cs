namespace Main.Data
{
    /// <summary>
    /// Provides data for deserialization events.
    /// </summary>
    public class DeserializationEventArgs : EventArgs
    {
        public ISerializable Serializable { get; }
        public SerializationFormatEnum Format { get; }
        public SerializationContext Context { get; }
        public object SourceData { get; }
        public bool Cancel { get; set; }
        public DeserializationEventArgs(ISerializable serializable, SerializationFormatEnum format, SerializationContext context, object sourceData)
        {
            Serializable = serializable;
            Format = format;
            Context = context;
            SourceData = sourceData;
        }
    }
}
