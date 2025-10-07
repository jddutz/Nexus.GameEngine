namespace Main.Data
{
    /// <summary>
    /// Defines custom serialization logic for specific types.
    /// </summary>
    public interface ICustomSerializer
    {
        /// <summary>
        /// Gets the type this serializer handles.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="context">The serialization context.</param>
        /// <returns>Serialized representation.</returns>
        object Serialize(object obj, SerializationContext context);

        /// <summary>
        /// Deserializes the object.
        /// </summary>
        /// <param name="data">The serialized data.</param>
        /// <param name="context">The deserialization context.</param>
        /// <returns>Deserialized object.</returns>
        object Deserialize(object data, SerializationContext context);

        /// <summary>
        /// Gets whether this serializer can handle the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type can be handled.</returns>
        bool CanSerialize(Type type);
    }
}
