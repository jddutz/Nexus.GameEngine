namespace Main.Data
{
    /// <summary>
    /// Provides data for serialization events.
    /// </summary>
    public class SerializationEventArgs : EventArgs
    {
        public ISerializable Serializable { get; }
        public SerializationFormatEnum Format { get; }
        public SerializationContext Context { get; }
        public bool Cancel { get; set; }
        public SerializationEventArgs(ISerializable serializable, SerializationFormatEnum format, SerializationContext context)
        {
            Serializable = serializable;
            Format = format;
            Context = context;
        }
    }
}
