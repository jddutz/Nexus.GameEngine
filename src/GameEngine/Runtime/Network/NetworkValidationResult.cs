namespace Nexus.GameEngine.Data.Binding
{
    public struct NetworkValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, IReadOnlyList<string> conflicts)
    {
        public bool IsValid { get; } = isValid;
        public IReadOnlyList<string> Errors { get; } = errors;
        public IReadOnlyList<string> Warnings { get; } = warnings;
        public IReadOnlyList<string> Conflicts { get; } = conflicts;
    }
}
