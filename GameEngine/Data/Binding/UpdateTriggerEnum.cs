namespace Main.Data
{
    /// <summary>
    /// Specifies when binding updates are triggered.
    /// </summary>
    public enum UpdateTriggerEnum
    {
        PropertyChanged,
        LostFocus,
        Explicit,
        Timer,
        AnyChange
    }
}
