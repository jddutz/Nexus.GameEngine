using System;
using System.Linq.Expressions;
using Nexus.GameEngine.Components.Lookups;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Static helper class for creating PropertyBinding instances with a fluent, type-safe API.
/// </summary>
public static class Binding
{
    /// <summary>
    /// Creates a binding from a parent component of type T.
    /// </summary>
    public static PropertyBinding FromParent<T>(Expression<Func<T, object?>> propertySelector) where T : class, IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromParent<T>().GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Creates a binding from a sibling component of type T.
    /// </summary>
    public static PropertyBinding FromSibling<T>(Expression<Func<T, object?>> propertySelector) where T : IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromSibling<T>().GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Creates a binding from a child component of type T.
    /// </summary>
    public static PropertyBinding FromChild<T>(Expression<Func<T, object?>> propertySelector) where T : IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromChild<T>().GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Creates a binding from a named component.
    /// </summary>
    public static PropertyBinding FromNamedObject<T>(string name, Expression<Func<T, object?>> propertySelector) where T : IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromNamedObject(name).GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Creates a binding from a context component (ancestor) of type T.
    /// </summary>
    public static PropertyBinding FromContext<T>(Expression<Func<T, object?>> propertySelector) where T : IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromContext<T>().GetPropertyValue(propertyName);
    }

    /// <summary>
    /// Creates a two-way binding from a sibling component of type T.
    /// Defaults to SiblingLookup as it is the most common pattern for TwoWay bindings.
    /// </summary>
    public static PropertyBinding TwoWay<T>(Expression<Func<T, object?>> propertySelector) where T : IComponent
    {
        var propertyName = GetPropertyName(propertySelector);
        return PropertyBinding.FromSibling<T>()
            .GetPropertyValue(propertyName)
            .TwoWay();
    }

    private static string GetPropertyName<T>(Expression<Func<T, object?>> expression)
    {
        MemberExpression? member = null;

        if (expression.Body is UnaryExpression unary)
        {
            member = unary.Operand as MemberExpression;
        }
        else if (expression.Body is MemberExpression m)
        {
            member = m;
        }

        if (member == null)
        {
            throw new ArgumentException("Expression must be a member access", nameof(expression));
        }

        return member.Member.Name;
    }
}
