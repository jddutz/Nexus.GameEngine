namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Target platform for asset processing.
/// </summary>
public enum TargetPlatformEnum
{
    /// <summary>
    /// Universal platform - works on all platforms.
    /// </summary>
    Universal = 0,

    /// <summary>
    /// Windows platform.
    /// </summary>
    Windows = 1,

    /// <summary>
    /// macOS platform.
    /// </summary>
    MacOS = 2,

    /// <summary>
    /// Linux platform.
    /// </summary>
    Linux = 3,

    /// <summary>
    /// Android platform.
    /// </summary>
    Android = 4,

    /// <summary>
    /// iOS platform.
    /// </summary>
    iOS = 5,

    /// <summary>
    /// Web platform (WebAssembly).
    /// </summary>
    Web = 6
}