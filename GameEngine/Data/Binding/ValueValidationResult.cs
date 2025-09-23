namespace Main.Data
{
    public struct ValueValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public BindingErrorSeverityEnum Severity { get; }
        public ValueValidationResult(bool isValid, string? errorMessage = null, BindingErrorSeverityEnum severity = BindingErrorSeverityEnum.Error)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Severity = severity;
        }
        public static ValueValidationResult Success => new ValueValidationResult(true);
        public static ValueValidationResult Failure(string errorMessage, BindingErrorSeverityEnum severity = BindingErrorSeverityEnum.Error)
        {
            return new ValueValidationResult(false, errorMessage, severity);
        }
    }
}
