namespace Nexus.GameEngine.Data.Binding
{
    public struct BindingError(string propertyName, string message, BindingErrorSeverityEnum severity, Exception? exception = null)
    {
        public string PropertyName { get; } = propertyName;
        public string Message { get; } = message;
        public Exception? Exception { get; } = exception;
        public BindingErrorSeverityEnum Severity { get; } = severity;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}
