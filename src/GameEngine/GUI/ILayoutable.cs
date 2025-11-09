namespace Nexus.GameEngine.GUI;

/// <summary>
/// Lightweight alias for layout-facing elements.
/// Kept as a separate interface to make layout-specific APIs explicit while
/// reusing the existing `IUserInterfaceElement` contract.
/// </summary>
public interface ILayoutable : IUserInterfaceElement
{
}
