namespace Nexus.GameEngine.GUI.Layout
{
    /// <summary>
    /// Defines spacing distribution modes used by directional layout components.
    /// </summary>
    public enum SpacingMode
    {
        /// <summary>
        /// Items are stacked directly against each other with no spacing.
        /// </summary>
        Stacked = 0,

        /// <summary>
        /// Space between items only (first at start, last at end).
        /// </summary>
        Justified = 1,

        /// <summary>
        /// Space before, between, and after items (equal everywhere).
        /// </summary>
        Distributed = 2
    }
}
