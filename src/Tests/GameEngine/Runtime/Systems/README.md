# Mock System Helpers

This directory contains helper utilities for creating mocked system implementations for unit testing.

## Purpose

The `MockSystemHelpers` class provides a convenient way to create mocked instances of the core engine systems (`IGraphicsSystem`, `IResourceSystem`, etc.) along with their internal dependencies. This allows you to test framework classes that depend on these systems without needing to instantiate the full engine infrastructure.

## Usage

### Basic Usage

To create a mocked system, use the static factory methods on `MockSystemHelpers`:

```csharp
using Tests.GameEngine.Runtime.Systems;

// Create a mock graphics system
var mockGraphics = MockSystemHelpers.CreateGraphics();

// Access the system interface (to pass to your component)
IGraphicsSystem system = mockGraphics.System;

// Setup expectations on the internal dependencies
mockGraphics.PipelineManager
    .Setup(x => x.GetOrCreatePipeline(It.IsAny<PipelineDescriptor>()))
    .Returns(new PipelineHandle(1));
```

### Available Mocks

| Helper Method | System Interface | Mocked Dependencies |
| :--- | :--- | :--- |
| `CreateGraphics()` | `IGraphicsSystem` | `IGraphicsContext`, `IPipelineManager`, `IDescriptorManager`, `ISwapChain`, `ISyncManager`, `ICommandPoolManager` |
| `CreateResources()` | `IResourceSystem` | `IResourceManager`, `IBufferManager` |
| `CreateContent()` | `IContentSystem` | `IContentManager` |
| `CreateWindow()` | `IWindowSystem` | `IWindowService`, `IWindow` |
| `CreateInput()` | `IInputSystem` | `IWindowService`, `IActionFactory`, `IInputContext` |

## Example: Testing a Component

```csharp
[Fact]
public void MyComponent_DrawsQuad_WhenRendered()
{
    // Arrange
    var mockGraphics = MockSystemHelpers.CreateGraphics();
    var component = new MyComponent();
    
    // Inject the mocked system (assuming property injection or similar)
    // Note: Components usually access systems via 'this.Graphics', which uses the ServiceProvider.
    // For unit testing components, you might need to mock the ServiceProvider or use a test harness.
}
```
