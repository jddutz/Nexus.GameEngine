using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Input;
using Silk.NET.Maths;
using System.Numerics;

namespace Nexus.GameEngine.Runtime.Extensions;

/// <summary>
/// Extension methods for the input system to provide easy access to common input operations.
/// </summary>
public static class InputSystemExtensions
{
    /// <summary>
    /// Gets the underlying input context.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <returns>The input context.</returns>
    public static IInputContext GetInputContext(this IInputSystem system)
    {
        return ((InputSystem)system).InputContext;
    }
    
    /// <summary>
    /// Gets the primary keyboard device.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <returns>The primary keyboard.</returns>
    public static IKeyboard GetKeyboard(this IInputSystem system)
    {
        return ((InputSystem)system).Keyboard;
    }

    /// <summary>
    /// Gets the primary mouse device.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <returns>The primary mouse.</returns>
    public static IMouse GetMouse(this IInputSystem system)
    {
        return ((InputSystem)system).Mouse;
    }

    /// <summary>
    /// Executes an action asynchronously.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <param name="actionId">The ID of the action to execute.</param>
    /// <param name="context">The component context for the action.</param>
    /// <returns>A task representing the asynchronous operation, containing the action result.</returns>
    public static Task<ActionResult> ExecuteActionAsync(this IInputSystem system, ActionId actionId, IComponent context)
    {
        return ((InputSystem)system).ActionFactory.ExecuteAsync(actionId, context);
    }

    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    public static bool IsKeyPressed(this IInputSystem system, Key key)
    {
        return ((InputSystem)system).Keyboard.IsKeyPressed(key);
    }

    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <returns>The current mouse position as a Vector2.</returns>
    public static Vector2 GetMousePosition(this IInputSystem system)
    {
        return ((InputSystem)system).Mouse.Position;
    }

    /// <summary>
    /// Checks if a specific mouse button is currently pressed.
    /// </summary>
    /// <param name="system">The input system.</param>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    public static bool IsButtonPressed(this IInputSystem system, MouseButton button)
    {
        return ((InputSystem)system).Mouse.IsButtonPressed(button);
    }
}
