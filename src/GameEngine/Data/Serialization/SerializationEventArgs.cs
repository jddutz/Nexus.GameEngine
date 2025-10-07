namespace Main.Data
{
    /// <summary>
    /// Provides data for serialization events.
    /// </summary>
    public class SerializationEventArgs(ISerializable serializable, SerializationFormatEnum format, SerializationContext context) : EventArgs
    {
        public ISerializable Serializable { get; } = serializable;
        public SerializationFormatEnum Format { get; } = format;
        public SerializationContext Context { get; } = context;
        public bool Cancel { get; set; }
    }
}
