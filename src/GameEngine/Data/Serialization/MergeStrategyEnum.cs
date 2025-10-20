namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Defines merge strategies for combining serializable objects.
    /// </summary>
    public enum MergeStrategyEnum
    {
        /// <summary>
        /// Overwrite all properties with source values.
        /// </summary>
        Overwrite,

        /// <summary>
        /// Merge only non-null or non-default values.
        /// </summary>
        MergeNonNull,

        /// <summary>
        /// Merge collections by union.
        /// </summary>
        UnionCollections,

        /// <summary>
        /// Merge collections by intersection.
        /// </summary>
        IntersectCollections,

        /// <summary>
        /// Custom merge strategy defined by implementation.
        /// </summary>
        Custom
    }
}
