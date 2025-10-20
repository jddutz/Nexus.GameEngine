using System.Text;

namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Represents a component that provides custom serialization and deserialization capabilities.
    /// Supports multiple serialization formats, versioning, and advanced serialization features.
    /// </summary>
    public interface ISerializable
    {
        SerializationFormatEnum SerializationFormatEnum { get; set; }
        int SerializationVersion { get; set; }
        bool IncludeTypeInfo { get; set; }
        bool PrettyFormat { get; set; }
        bool CompressData { get; set; }
        bool EncryptData { get; set; }
        Encoding TextEncoding { get; set; }
        SerializationSettings Settings { get; set; }
        HashSet<string> ExcludedProperties { get; set; }
        HashSet<string> IncludedProperties { get; set; }
        Dictionary<string, string> PropertyMappings { get; set; }
        Dictionary<string, object> SerializationMetadata { get; set; }
        bool HandleCircularReferences { get; set; }
        int MaxSerializationDepth { get; set; }
        Dictionary<Type, ICustomSerializer> CustomSerializers { get; set; }
        bool CanSerialize { get; }
        bool CanDeserialize { get; }
        bool IsSerializing { get; }
        bool IsDeserializing { get; }
        event EventHandler<SerializationEventArgs> BeforeSerialize;
        event EventHandler<SerializationEventArgs> AfterSerialize;
        event EventHandler<SerializationErrorEventArgs> SerializationFailed;
        event EventHandler<DeserializationEventArgs> BeforeDeserialize;
        event EventHandler<DeserializationEventArgs> AfterDeserialize;
        event EventHandler<DeserializationErrorEventArgs> DeserializationFailed;
        string Serialize();
        Task<string> SerializeAsync();
        byte[] SerializeToBytes();
        Task<byte[]> SerializeToBytesAsync();
        void SerializeToStream(Stream stream);
        Task SerializeToStreamAsync(Stream stream);
        void Deserialize(string data);
        Task DeserializeAsync(string data);
        void DeserializeFromBytes(byte[] data);
        Task DeserializeFromBytesAsync(byte[] data);
        void DeserializeFromStream(Stream stream);
        Task DeserializeFromStreamAsync(Stream stream);
        object Clone();
        Task<object> CloneAsync();
        long GetSerializedSize();
        SerializationValidationResult ValidateSerializability();
        SerializationInfo GetSerializationInfo();
        string ConvertToFormat(SerializationFormatEnum targetFormat);
        bool SerializedEquals(object other);
        int GetSerializedHashCode();
        void UpdateFrom(ISerializable source);
        void MergeFrom(ISerializable source, MergeStrategyEnum strategy);
        IReadOnlyList<SerializationDifference> GetDifferences(ISerializable other);
        void ApplyPatch(SerializationPatch patch);
        SerializationPatch CreatePatch(ISerializable source);
        void ExcludeProperty(string propertyName);
        void IncludeProperty(string propertyName);
        void MapProperty(string propertyName, string serializedName);
        void UnmapProperty(string propertyName);
        void RegisterCustomSerializer(Type type, ICustomSerializer serializer);
        void UnregisterCustomSerializer(Type type);
        void SetMetadata(string key, object value);
        object? GetMetadata(string key);
        bool RemoveMetadata(string key);
    }
}
