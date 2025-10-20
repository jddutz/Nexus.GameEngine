namespace Nexus.GameEngine.Data.Binding
{
    public struct ValueValidationResult(bool isValid, string? errorMessage = null, BindingErrorSeverityEnum severity = BindingErrorSeverityEnum.Error)
    {
        public bool IsValid { get; } = isValid;
        public string? ErrorMessage { get; } = errorMessage;
        public BindingErrorSeverityEnum Severity { get; } = severity;

        public static ValueValidationResult Success => new(true);
        public static ValueValidationResult Failure(string errorMessage, BindingErrorSeverityEnum severity = BindingErrorSeverityEnum.Error)
        {
            return new ValueValidationResult(false, errorMessage, severity);
        }
    }
}
