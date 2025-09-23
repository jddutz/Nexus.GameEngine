namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents a unique identifier for components based on hash codes.
/// Provides fast integer comparisons while supporting meaningful string-based creation.
/// </summary>
public class ComponentId : IEquatable<ComponentId>
{
    /// <summary>
    /// The hash code value that uniquely identifies this component.
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// Creates a new ComponentId with a random hash value.
    /// Used for transient components that should never match existing cached components.
    /// </summary>
    public ComponentId()
    {
        Value = Guid.NewGuid().GetHashCode();
    }

    /// <summary>
    /// Creates a ComponentId with the specified hash value.
    /// </summary>
    /// <param name="value">The hash code value</param>
    public ComponentId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// An empty ComponentId with no value
    /// </summary>
    public static readonly ComponentId None = new(0);

    /// <summary>
    /// Creates a ComponentId from a string by computing its hash code.
    /// Same strings will always produce the same ComponentId.
    /// </summary>
    /// <param name="key">The string to hash</param>
    /// <returns>A ComponentId based on the string's hash</returns>
    public static ComponentId FromString(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return new ComponentId(key.GetHashCode());
    }

    /// <summary>
    /// Creates a ComponentId from a type by using the type's full name hash.
    /// Useful for singleton components where one instance per type is desired.
    /// </summary>
    /// <param name="type">The type to create an ID for</param>
    /// <returns>A ComponentId based on the type's full name</returns>
    public static ComponentId FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return FromString($"type-{type.FullName}");
    }

    /// <summary>
    /// Creates a ComponentId from a type using a generic type parameter.
    /// Useful for singleton components where one instance per type is desired.
    /// </summary>
    /// <typeparam name="T">The type to create an ID for</typeparam>
    /// <returns>A ComponentId based on the type's full name</returns>
    public static ComponentId FromType<T>()
    {
        return FromType(typeof(T));
    }

    /// <summary>
    /// Generates a new random ComponentId.
    /// Each call produces a different ID, useful for transient components.
    /// </summary>
    /// <returns>A new random ComponentId</returns>
    public static ComponentId Generate()
    {
        return new ComponentId();
    }

    /// <summary>
    /// Creates a ComponentId by combining multiple string values.
    /// Useful for contextual components that need composite keys.
    /// </summary>
    /// <param name="parts">The string parts to combine</param>
    /// <returns>A ComponentId based on the combined string hash</returns>
    public static ComponentId FromParts(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
            throw new ArgumentException("Parts cannot be null or empty", nameof(parts));

        var combined = string.Join("-", parts);
        return FromString(combined);
    }

    /// <summary>
    /// Determines whether the specified ComponentId is equal to this one.
    /// </summary>
    public bool Equals(ComponentId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    /// <summary>
    /// Determines whether the specified object is equal to this ComponentId.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ComponentId);
    }

    /// <summary>
    /// Returns the hash code for this ComponentId.
    /// </summary>
    public override int GetHashCode()
    {
        return Value;
    }

    /// <summary>
    /// Returns a string representation of this ComponentId.
    /// </summary>
    public override string ToString()
    {
        return $"ComponentId({Value})";
    }

    /// <summary>
    /// Determines whether two ComponentId instances are equal.
    /// </summary>
    public static bool operator ==(ComponentId? left, ComponentId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two ComponentId instances are not equal.
    /// </summary>
    public static bool operator !=(ComponentId? left, ComponentId? right)
    {
        return !Equals(left, right);
    }
}