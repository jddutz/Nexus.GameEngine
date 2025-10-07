namespace Main.Data
{
    /// <summary>
    /// Specifies error handling behavior for binding operations.
    /// </summary>
    public enum BindingErrorHandlingEnum
    {
        Ignore,
        Log,
        Throw,
        UseDefault,
        Custom
    }
}
