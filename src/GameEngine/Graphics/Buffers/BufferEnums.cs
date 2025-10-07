namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Buffer storage flags for creating immutable buffers
/// </summary>
[Flags]
public enum BufferStorageFlags : uint
{
    None = 0,
    MapReadBit = 0x0001,
    MapWriteBit = 0x0002,
    MapPersistentBit = 0x0040,
    MapCoherentBit = 0x0080,
    DynamicStorageBit = 0x0100,
    ClientStorageBit = 0x0200
}

/// <summary>
/// Map buffer access mask for mapping buffer ranges
/// </summary>
[Flags]
public enum MapBufferAccessMask : uint
{
    MapReadBit = 0x0001,
    MapWriteBit = 0x0002,
    MapInvalidateRangeBit = 0x0004,
    MapInvalidateBufferBit = 0x0008,
    MapFlushExplicitBit = 0x0010,
    MapUnsynchronizedBit = 0x0020,
    MapPersistentBit = 0x0040,
    MapCoherentBit = 0x0080
}