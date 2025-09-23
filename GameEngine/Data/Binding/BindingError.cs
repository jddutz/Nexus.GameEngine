namespace Main.Data
{
    public struct BindingError
    {
        public string PropertyName { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public BindingErrorSeverityEnum Severity { get; }
        public DateTime Timestamp { get; }
        public BindingError(string propertyName, string message, BindingErrorSeverityEnum severity, Exception? exception = null)
        {
            PropertyName = propertyName;
            Message = message;
            Severity = severity;
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }
    }
}
