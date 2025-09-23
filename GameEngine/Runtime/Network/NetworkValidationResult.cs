namespace Main.Data
{
    public struct NetworkValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<string> Conflicts { get; }
        public NetworkValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, IReadOnlyList<string> conflicts)
        {
            IsValid = isValid;
            Errors = errors;
            Warnings = warnings;
            Conflicts = conflicts;
        }
    }
}
