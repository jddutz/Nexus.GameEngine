namespace Nexus.GameEngine.Assets;

/// <summary>
/// Represents an asset identifier with address information.
/// </summary>
public record AssetId
{
    /// <summary>
    /// The address/path of the asset.
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// Additional identifier information.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Creates a new AssetId with the specified address.
    /// </summary>
    /// <param name="address">The asset address/path</param>
    public AssetId(string address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Creates a new AssetId with the specified address and id.
    /// </summary>
    /// <param name="address">The asset address/path</param>
    /// <param name="id">Additional identifier</param>
    public AssetId(string address, string? id)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Id = id;
    }

    /// <summary>
    /// Default constructor for record initialization.
    /// </summary>
    public AssetId() { }

    /// <summary>
    /// Implicit conversion from string to AssetId.
    /// </summary>
    /// <param name="address">The asset address</param>
    public static implicit operator AssetId(string address) => new(address);

    /// <summary>
    /// String representation of the AssetId.
    /// </summary>
    /// <returns>The address string</returns>
    public override string ToString() => Address;
}