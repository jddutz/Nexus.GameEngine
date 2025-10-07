namespace Main.Data
{
    public struct BindingValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        public bool IsValid { get; } = isValid;
        public IReadOnlyList<string> Errors { get; } = errors;
        public IReadOnlyList<string> Warnings { get; } = warnings;
    }
}
