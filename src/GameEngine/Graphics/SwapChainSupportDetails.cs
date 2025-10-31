/// <summary>
/// Details about swap chain support for a physical device.
/// </summary>
public record struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities { get; init; }
    public SurfaceFormatKHR[] Formats { get; init; }
    public PresentModeKHR[] PresentModes { get; init; }

    /// <summary>
    /// Returns true if the device supports at least one format and one present mode.
    /// </summary>
    public readonly bool IsAdequate => Formats.Length > 0 && PresentModes.Length > 0;
}