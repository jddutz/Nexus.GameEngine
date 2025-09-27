# Nexus Game Engine - Testing Framework

## Overview

This document outlines the testing strategy and framework for the Nexus Game Engine, with a focus on clean separation between OpenGL and non-OpenGL tests using modern .NET testing tools and best practices.

## Table of Contents

- [Testing Philosophy](#testing-philosophy)
- [Project Structure](#project-structure)
- [Test Infrastructure](#test-infrastructure)
- [OpenGL Testing Strategy](#opengl-testing-strategy)
- [Test Types](#test-types)
- [Running Tests](#running-tests)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Testing Philosophy

The Nexus Game Engine follows a **documentation-first, test-driven development** approach with these core principles:

1. **Clean Separation**: Separate OpenGL-dependent tests from regular unit tests to avoid context conflicts
2. **Sequential OpenGL Testing**: OpenGL tests run sequentially to prevent threading issues with graphics contexts
3. **Isolated Test Execution**: Each test runs in a clean state with proper resource management
4. **Visual Regression Testing**: Compare rendered output for graphics-related functionality
5. **Headless Testing**: Run tests without requiring a display (CI/CD compatible)

## Project Structure

The testing framework is organized into two distinct projects:

### Tests Project (`Tests/`)

- **Purpose**: Houses all non-OpenGL unit and integration tests
- **Focus**: Business logic, components, data structures, algorithms
- **Dependencies**: No direct OpenGL context requirements
- **Execution**: Runs in parallel for fast execution
- **Test Count**: 166 tests covering core engine functionality

### OpenGLTests Project (`OpenGLTests/`)

- **Purpose**: Dedicated project for all OpenGL-dependent tests
- **Focus**: Graphics rendering, shaders, visual validation
- **Dependencies**: Requires OpenGL context and graphics resources
- **Execution**: Sequential execution to prevent context conflicts
- **Test Count**: 13 tests covering graphics functionality

## Test Infrastructure

### Regular Tests (Tests Project)

#### Core Components

- **Moq Framework**: For mocking dependencies and isolating units under test
- **xUnit.net**: Test framework with excellent parallel execution support
- **Component Testing**: Validates component behavior without requiring graphics context
- **Integration Testing**: Tests multi-component interactions using mocked graphics dependencies

### OpenGL Tests (OpenGLTests Project)

#### Core Components

#### 1. OpenGLContextFixture

- **Purpose**: Provides a single, managed OpenGL context using xUnit Collection Fixtures
- **Benefits**: Eliminates context creation overhead and threading conflicts
- **Features**:
  - Lazy initialization of OpenGL context
  - Automatic state reset between tests
  - Proper resource cleanup and disposal
  - Thread-safe context management

#### 2. OpenGLTestBase

- **Purpose**: Base class for all OpenGL tests providing common functionality
- **Features**:
  - Access to shared GL context and window
  - Helper methods for common OpenGL operations
  - Automatic error checking with `AssertNoGLErrors()`
  - Frame capture capabilities for visual validation

#### 3. TestRenderer

- **Purpose**: Lightweight renderer implementation for graphics testing
- **Capabilities**:
  - Basic render pass management
  - Resource sharing and management
  - Configurable rendering pipeline
  - Resource sharing
  - Configurable rendering pipeline

### Modern Testing Stack

Our testing infrastructure leverages these modern .NET tools:

- **xUnit.net**: Primary test framework with excellent async support and Collection Fixtures
- **Microsoft.Testing.Platform**: New test platform for improved performance and tooling
- **Moq**: Mocking framework for isolating dependencies
- **Silk.NET**: Cross-platform OpenGL bindings with comprehensive API coverage

## OpenGL Testing Strategy

### Context Management

The OpenGL testing approach uses xUnit Collection Fixtures to ensure proper context management:

```csharp
// OpenGL Context Fixture - manages shared context lifecycle
public class OpenGLContextFixture : IDisposable
{
    public GL GL { get; private set; }
    public IWindow Window { get; private set; }
    public TestRenderer Renderer { get; private set; }

    public OpenGLContextFixture()
    {
        // Initialize OpenGL context once for all tests
        var options = WindowOptions.Default;
        options.IsVisible = false;
        Window = Silk.NET.Windowing.Window.Create(options);
        GL = Window.CreateOpenGL();
        Renderer = new TestRenderer(GL);
    }
}

// Collection definition for sequential OpenGL test execution
[CollectionDefinition("OpenGL")]
public class OpenGLCollection : ICollectionFixture<OpenGLContextFixture> { }

// Example OpenGL test class
[Collection("OpenGL")]
public class MyOpenGLTests : OpenGLTestBase
{
    public MyOpenGLTests(OpenGLContextFixture fixture) : base(fixture) { }

    [Fact]
    public void MyTest()
    {
        // GL context automatically available and clean
        GL.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);
        AssertNoGLErrors();
    }
}
```

### State Reset Between Tests

OpenGL state is **automatically reset** between each test method in the OpenGLTestBase:

```csharp
[Collection("OpenGL")]
public class MyGraphicsTests : OpenGLTestBase
{
    public MyGraphicsTests(OpenGLContextFixture fixture) : base(fixture) { }

    [Fact]
    public void MyGraphicsTest()
    {
        // OpenGL state is automatically clean for each test
        // GL context available via inherited properties
        GL.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);

        // Perform test operations with clean state
        RenderTestFrame();

        // Verify results with helper methods
        var frameData = CaptureFrame();
        AssertNoGLErrors();
    }
}
```

The automatic state reset system in OpenGLTestBase:

- **Resets before each test** in the base class constructor
- **Comprehensive state cleanup** including:
  - Clears all framebuffers with neutral values (black background)
  - Resets depth testing, blending, and culling state
  - Restores viewport to window dimensions
  - Disables scissor and stencil testing
  - Validates no GL errors occurred during reset
- **Sequential execution** prevents threading conflicts
- **Reliable isolation** ensures each test starts with clean OpenGL state

### Headless Testing Support

For CI/CD environments, we support headless testing using:

- **Xvfb (Linux)**: Virtual framebuffer for headless OpenGL
- **Mesa Software Rendering**: CPU-based OpenGL rendering
- **Invisible Windows**: Window creation with `IsVisible = false`

#### CI/CD Configuration Example

```yaml
# GitHub Actions example
- name: Install Xvfb (Linux)
  if: runner.os == 'Linux'
  run: |
    sudo apt-get update
    sudo apt-get install -y xvfb mesa-utils

- name: Run Tests with Virtual Display
  if: runner.os == 'Linux'
  run: xvfb-run -a dotnet test
```

## Test Types

### 1. Unit Tests (Tests Project)

- **Scope**: Individual components, utilities, and business logic
- **OpenGL**: No OpenGL dependencies - uses mocked graphics dependencies
- **Example**: Component validation, data structures, algorithms, input handling
- **Execution**: Parallel execution for fast test runs
- **Location**: `Tests/` project directory

### 2. Component Integration Tests (Tests Project)

- **Scope**: Multi-component interactions without requiring OpenGL context
- **OpenGL**: Uses mocked renderer and graphics interfaces
- **Example**: Component lifecycle, event handling, resource management abstractions
- **Execution**: Parallel execution with mocked dependencies

### 3. OpenGL Integration Tests (OpenGLTests Project)

- **Scope**: Graphics operations requiring real OpenGL context
- **OpenGL**: Uses OpenGLContextFixture for real GL operations
- **Example**: Renderer pipeline, shader compilation, state management
- **Execution**: Sequential execution to prevent context conflicts
- **Location**: `OpenGLTests/` project directory

### 4. Visual Regression Tests (OpenGLTests Project)

- **Scope**: Graphics output validation with frame capture
- **OpenGL**: Full rendering pipeline with pixel validation
- **Example**: Rendering correctness, visual effects, UI rendering

```csharp
[Collection("OpenGL")]
public class VisualRegressionTests : OpenGLTestBase
{
    public VisualRegressionTests(OpenGLContextFixture fixture) : base(fixture) { }

    [Fact]
    public void RenderTriangle_ProducesExpectedOutput()
    {
        // Arrange
        var expectedPixels = LoadReferenceImage("triangle.png");

        // Act
        RenderTriangle();
        var actualPixels = CaptureFrame();

        // Assert
        AssertImagesMatch(expectedPixels, actualPixels, tolerance: 0.01f);
    }
}
```

### 5. Performance Tests (OpenGLTests Project)

- **Scope**: Rendering performance and resource usage
- **OpenGL**: Measures frame times, draw calls, memory usage
- **Example**: Batch rendering efficiency, large scene performance
- **Execution**: Sequential execution for consistent performance measurements

## Running Tests

### Command Line

```bash
# Run all tests (both projects)
dotnet test

# Run only regular tests (fast, parallel execution)
dotnet test Tests/Tests.csproj

# Run only OpenGL tests (sequential execution)
dotnet test OpenGLTests/OpenGLTests.csproj

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test category
dotnet test --filter Category=Integration
```

### Visual Studio

1. Open Test Explorer (Test → Test Explorer)
2. Build solution to discover tests
3. Run tests individually or in groups
4. View test output and results

### CI/CD Integration

The testing framework is designed to work seamlessly in CI/CD environments:

- **Headless execution** supported on Linux with Xvfb
- **Cross-platform** testing on Windows, Linux, and macOS
- **Fast execution** with shared context and parallel tests
- **Reliable results** with proper state isolation

## Best Practices

### Test Organization

1. **Separate by OpenGL dependency**: Place OpenGL-dependent tests in `OpenGLTests/`, others in `Tests/`
2. **Group by feature area**: Organize tests in folders matching the source structure
3. **Use descriptive names**: Test method names should clearly describe what's being tested
4. **Follow AAA pattern**: Arrange, Act, Assert structure for clarity

### Regular Testing (Tests Project)

1. **Mock graphics dependencies**: Use Moq to mock IRenderer, IWindow, and other graphics interfaces
2. **Focus on logic**: Test business logic, algorithms, and component behavior
3. **Parallel execution**: Take advantage of fast parallel test execution
4. **Isolate dependencies**: Mock external services and graphics resources

### OpenGL Testing (OpenGLTests Project)

1. **Inherit from OpenGLTestBase**: Use the base class for automatic state management
2. **Use Collection attribute**: Ensure tests are in the "OpenGL" collection for proper execution
3. **State is automatically clean**: Each test starts with a clean OpenGL state
4. **Use helper methods**: Leverage `AssertNoGLErrors()`, `CaptureFrame()`, etc.
5. **Sequential execution**: Tests run sequentially to prevent context conflicts

Example OpenGL test structure:

```csharp
[Collection("OpenGL")]
public class MyOpenGLTests : OpenGLTestBase
{
    public MyOpenGLTests(OpenGLContextFixture fixture) : base(fixture) { }

    [Fact]
    public void MyTest()
    {
        // Test logic with clean GL state
        GL.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);
        AssertNoGLErrors();
    }
}
```

### Performance

1. **Separate test concerns**: Keep fast unit tests separate from slower OpenGL tests
2. **Use shared fixtures**: OpenGL context is shared across all OpenGL tests
3. **Clean up resources**: OpenGLTestBase handles automatic cleanup
4. **Optimize for CI**: Regular tests run fast in parallel, OpenGL tests run when needed

### Reliability

1. **Avoid timing dependencies**: Don't rely on Thread.Sleep or timing for test logic
2. **Handle async operations**: Use proper async/await patterns for async operations
3. **Sequential OpenGL execution**: Prevents threading conflicts with graphics contexts
4. **Comprehensive state reset**: Each OpenGL test starts with known clean state

## Test Framework Evolution

### Current Architecture (v2.0)

- **Separated test projects**: Clean separation between OpenGL and non-OpenGL tests
- **xUnit Collection Fixtures**: Proper OpenGL context management with sequential execution
- **Automatic state reset**: Each OpenGL test starts with clean state
- **Parallel non-OpenGL tests**: Fast execution of business logic tests
- **Comprehensive coverage**: 179 total tests (166 regular + 13 OpenGL)

### Previous Architecture (v1.0) - Deprecated

- Single shared OpenGL context with threading issues
- Mixed OpenGL and regular tests causing context conflicts
- Manual state reset requirements
- Threading conflicts and race conditions

### Planned Improvements (v3.0)

1. **Enhanced Visual Testing**

   - Automated reference image generation
   - Pixel-perfect comparison tools
   - Tolerance-based matching algorithms

2. **Advanced Performance Testing**

   - GPU profiling integration
   - Memory usage tracking
   - Benchmark comparison tools

3. **Extended Platform Support**

   - WebGL testing support
   - Mobile platform testing
   - Vulkan testing framework

4. **Test Automation**
   - Automatic test generation from examples
   - Property-based testing for graphics algorithms
   - Fuzzing for shader validation

## Troubleshooting

### Common Issues

#### "GLFW window class already registered"

- **Cause**: Attempting to create multiple OpenGL contexts (should not occur with new architecture)
- **Solution**: Ensure OpenGL tests use `[Collection("OpenGL")]` and inherit from `OpenGLTestBase`

#### Tests fail in CI but pass locally

- **Cause**: Missing headless display setup for OpenGL tests
- **Solution**: Install and configure Xvfb on Linux CI runners

#### OpenGL tests fail with context errors

- **Cause**: Test not properly using the OpenGL test infrastructure
- **Solution**: Ensure test inherits from `OpenGLTestBase` and is in the OpenGL collection

#### Performance tests are inconsistent

- **Cause**: System load affecting timing measurements
- **Solution**: Use relative performance metrics and multiple runs, ensure sequential execution

### Debug Information

When tests fail, the framework provides:

1. **OpenGL Error Codes**: `AssertNoGLErrors()` provides detailed error information
2. **Captured Frames**: Visual debugging through `CaptureFrame()` method
3. **State Information**: Current OpenGL state at failure point
4. **Test Isolation**: Each test runs with clean state for reliable debugging

### Getting Help

1. Check the [Silk.NET documentation](https://github.com/dotnet/Silk.NET/wiki) for OpenGL-specific issues
2. Review [OpenGL debugging best practices](https://www.khronos.org/opengl/wiki/Debug_Output)
3. Consult the engine's GitHub issues for known problems
4. Join the project Discord for real-time support

---

## Contributing to Tests

When adding new tests:

### For Regular Tests (Tests Project)

1. Place non-OpenGL tests in the `Tests/` project
2. Mock graphics dependencies using Moq
3. Focus on business logic and component behavior
4. Tests will run in parallel for fast execution

### For OpenGL Tests (OpenGLTests Project)

1. Place OpenGL-dependent tests in the `OpenGLTests/` project
2. Inherit from `OpenGLTestBase` and use `[Collection("OpenGL")]`
3. Use the provided GL context and helper methods
4. Tests will run sequentially for reliable OpenGL context management

### General Guidelines

1. Follow established patterns in existing tests
2. Add appropriate categories/traits for test organization
3. Include both positive and negative test cases
4. Document any special setup requirements
5. Update this README if introducing new testing patterns

## References

- [.NET Testing Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [xUnit.net Documentation](https://xunit.net/)
- [xUnit Collection Fixtures](https://xunit.net/docs/shared-context#collection-fixture)
- [Silk.NET OpenGL Documentation](https://github.com/dotnet/Silk.NET)
- [OpenGL Testing Best Practices](https://www.khronos.org/opengl/wiki/Testing)
- [Headless OpenGL with Mesa](https://www.mesa3d.org/)
- [Moq Framework Documentation](https://github.com/moq/moq4)

- [.NET Testing Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [xUnit.net Documentation](https://xunit.net/)
- [Silk.NET OpenGL Documentation](https://github.com/dotnet/Silk.NET)
- [OpenGL Testing Best Practices](https://www.khronos.org/opengl/wiki/Testing)
- [Headless OpenGL with Mesa](https://www.mesa3d.org/)
