using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering;

/// <summary>
/// Lambda-based batch data structure that groups similar draw calls.
/// Instead of storing instance data, stores lambda commands for maximum flexibility.
/// </summary>
public class DrawBatch
{
    /// <summary>
    /// Unique key identifying this batch (can be any object - typically tuple of render state)
    /// </summary>
    public object BatchKey { get; init; } = null!;

    /// <summary>
    /// List of lambda draw commands to execute for this batch
    /// </summary>
    public List<Action> DrawCommands { get; } = [];

    /// <summary>
    /// Number of draw commands in this batch
    /// </summary>
    public int CommandCount => DrawCommands.Count;

    /// <summary>
    /// Adds a lambda draw command to this batch
    /// </summary>
    /// <param name="drawCommand">Lambda that performs the actual GL draw calls</param>
    public void Draw(Action drawCommand)
    {
        DrawCommands.Add(drawCommand);
    }

    /// <summary>
    /// Executes all lambda commands in this batch
    /// </summary>
    public void Execute()
    {
        foreach (var command in DrawCommands)
        {
            command();
        }
    }

    /// <summary>
    /// Clears all commands from this batch for reuse in the next frame
    /// </summary>
    public void Clear()
    {
        DrawCommands.Clear();
    }
}