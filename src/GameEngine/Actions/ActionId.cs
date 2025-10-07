namespace Nexus.GameEngine.Actions;

/// <summary>
/// Unique identifier for actions that provides fast lookup and serialization support.
/// Designed for high-performance scenarios like game event processing.
/// </summary>
public readonly struct ActionId : IEquatable<ActionId>
{
    /// <summary>
    /// Fast lookup hash for runtime performance
    /// </summary>
    public int Hash { get; }

    /// <summary>
    /// String identifier for serialization and debugging
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Action type for fast runtime lookup and instantiation
    /// </summary>
    public Type ActionType { get; }

    /// <summary>
    /// Creates a new ActionId with the specified type and identifier.
    /// </summary>
    /// <param name="actionType">The action type</param>
    /// <param name="identifier">String identifier for serialization</param>
    private ActionId(Type actionType, string identifier)
    {
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Hash = HashCode.Combine(actionType.GetHashCode(), identifier.GetHashCode());
    }

    /// <summary>
    /// Creates ActionId from action type using the type name as identifier.
    /// </summary>
    /// <typeparam name="T">Action type that implements IAction</typeparam>
    /// <returns>ActionId for the specified action type</returns>
    public static ActionId FromType<T>() where T : IAction
    {
        return FromType(typeof(T));
    }

    /// <summary>
    /// Creates ActionId from action type using the type name as identifier.
    /// </summary>
    /// <param name="actionType">Action type that implements IAction</param>
    /// <returns>ActionId for the specified action type</returns>
    public static ActionId FromType(Type actionType)
    {
        ArgumentNullException.ThrowIfNull(actionType);

        if (!typeof(IAction).IsAssignableFrom(actionType))
            throw new ArgumentException($"Type {actionType.Name} does not implement IAction", nameof(actionType));

        return new ActionId(actionType, actionType.Name);
    }

    /// <summary>
    /// Creates ActionId from action type with a custom identifier.
    /// </summary>
    /// <typeparam name="T">Action type that implements IAction</typeparam>
    /// <param name="identifier">Custom string identifier</param>
    /// <returns>ActionId for the specified action type</returns>
    public static ActionId Create<T>(string identifier) where T : IAction
    {
        return Create(typeof(T), identifier);
    }

    /// <summary>
    /// Creates ActionId from action type with a custom identifier.
    /// </summary>
    /// <param name="actionType">Action type that implements IAction</param>
    /// <param name="identifier">Custom string identifier</param>
    /// <returns>ActionId for the specified action type</returns>
    public static ActionId Create(Type actionType, string identifier)
    {
        ArgumentNullException.ThrowIfNull(actionType);

        if (!typeof(IAction).IsAssignableFrom(actionType))
            throw new ArgumentException($"Type {actionType.Name} does not implement IAction", nameof(actionType));

        if (string.IsNullOrEmpty(identifier))
            throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

        return new ActionId(actionType, identifier);
    }

    /// <summary>
    /// Represents a default or empty ActionId.
    /// </summary>
    public static readonly ActionId None = new(typeof(object), "None");

    /// <summary>
    /// Determines whether this ActionId is equal to another.
    /// </summary>
    public bool Equals(ActionId other)
    {
        return Hash == other.Hash &&
               ActionType == other.ActionType &&
               Identifier == other.Identifier;
    }

    /// <summary>
    /// Determines whether this ActionId is equal to the specified object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is ActionId other && Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this ActionId.
    /// </summary>
    public override int GetHashCode()
    {
        return Hash;
    }

    /// <summary>
    /// Returns a string representation of this ActionId.
    /// </summary>
    public override string ToString()
    {
        return $"ActionId({Identifier}, {ActionType.Name})";
    }

    /// <summary>
    /// Determines whether two ActionId instances are equal.
    /// </summary>
    public static bool operator ==(ActionId left, ActionId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two ActionId instances are not equal.
    /// </summary>
    public static bool operator !=(ActionId left, ActionId right)
    {
        return !left.Equals(right);
    }
}