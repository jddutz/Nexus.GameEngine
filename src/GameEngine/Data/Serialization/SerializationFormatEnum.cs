namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Specifies serialization formats.
    /// </summary>
    public enum SerializationFormatEnum
    {
        /// <summary>
        /// Binary format (compact, fast).
        /// </summary>
        Binary,

        /// <summary>
        /// JSON format (human-readable, cross-platform).
        /// </summary>
        Json,

        /// <summary>
        /// XML format (structured, extensible).
        /// </summary>
        Xml,

        /// <summary>
        /// MessagePack format (binary JSON alternative).
        /// </summary>
        MessagePack,

        /// <summary>
        /// Protobuf format (efficient binary).
        /// </summary>
        Protobuf,

        /// <summary>
        /// YAML format (human-readable configuration).
        /// </summary>
        Yaml,

        /// <summary>
        /// BSON format (binary JSON).
        /// </summary>
        Bson,

        /// <summary>
        /// Avro format (schema evolution).
        /// </summary>
        Avro,

        /// <summary>
        /// Custom format defined by implementation.
        /// </summary>
        Custom
    }
}
