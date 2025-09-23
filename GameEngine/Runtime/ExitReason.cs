namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Reasons why an application might exit
/// </summary>
public enum ExitReason
{
    /// <summary>
    /// Application completed successfully
    /// </summary>
    Success,

    /// <summary>
    /// User requested shutdown
    /// </summary>
    UserRequested,

    /// <summary>
    /// Configuration error
    /// </summary>
    ConfigurationError,

    /// <summary>
    /// Graphics initialization failed
    /// </summary>
    GraphicsInitError,

    /// <summary>
    /// Audio initialization failed
    /// </summary>
    AudioInitError,

    /// <summary>
    /// Input system initialization failed
    /// </summary>
    InputInitError,

    /// <summary>
    /// Unhandled exception
    /// </summary>
    UnhandledException,

    /// <summary>
    /// Service registration failed
    /// </summary>
    ServiceRegistrationError
}
