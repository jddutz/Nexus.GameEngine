# Project Structure

## Overview

The Nexus Game Engine is organized into a modular, component-based architecture with clear separation of concerns. This document outlines the project structure and key architectural components.

## Solution Structure

```
Nexus.GameEngine.sln
├── GameEngine/                     # Core game engine library
│   ├── Actions/                    # Action system for input handling
│   ├── Assets/                     # Asset loading and management
│   ├── Audio/                      # Audio system components
│   ├── Components/                 # Core component system
│   ├── Data/                       # Data loading and serialization
│   ├── Events/                     # Event system
│   ├── Graphics/                   # Graphics and rendering system
│   │   ├── Rendering/              # Core rendering interfaces and implementations
│   │   ├── Resources/              # Resource management system
│   │   ├── Sprites/                # 2D sprite rendering
│   │   └── Models/                 # 3D model rendering
│   ├── GUI/                        # User interface components
│   │   ├── Components/             # UI component implementations
│   │   ├── Layout/                 # Layout management
│   │   └── Abstractions/           # UI system interfaces
│   ├── Input/                      # Input system
│   ├── Physics/                    # Physics system
│   └── Runtime/                    # Runtime services and game loop
├── OpenGLExample/                  # Example application
├── Tests/                          # Unit and integration tests
└── packages/                       # NuGet package output
```

## Core Systems

### Component System

The engine is built around a hierarchical component system where everything inherits from `IRuntimeComponent`:

- **Component Trees**: All game objects, UI elements, and systems form parent-child hierarchies
- **Behavior Interfaces**: Components implement only the behaviors they need (IRenderable, IUpdatable, etc.)
- **Template Configuration**: Components are configured using strongly-typed template records
- **Dependency Injection**: Components receive dependencies through constructor injection

### Resource Management System

A new attribute-based resource management system provides centralized, type-safe resource handling:

- **Declarative Resources**: Shared resources declared using attributes on static definitions
- **Automatic Discovery**: Resources discovered via reflection at startup
- **Component Scoping**: Resources can be scoped to specific components for automatic lifecycle management
- **Memory Management**: Automatic purging of unused component-scoped resources
- **Type Safety**: Compile-time checking and IntelliSense support for resource access

#### Resource Categories

- **Geometry Resources**: Shared vertex array objects (quads, sprites, etc.)
- **Shader Resources**: Compiled shader programs for different rendering techniques
- **Asset Resources**: Loaded textures, models, and other game assets
- **Material Resources**: Configured material properties and effect parameters

### Graphics System

The graphics system provides a flexible rendering pipeline:

- **IRenderer**: Core rendering interface with OpenGL access
- **IRenderable**: Interface for components that can be rendered
- **Render Passes**: Configurable multi-pass rendering pipeline
- **Resource Sharing**: Efficient sharing of geometry, shaders, and textures

### GUI System

Component-based user interface system:

- **Layout Management**: Flexible layout containers (Vertical, Horizontal, Grid)
- **Visual Components**: Text, borders, background layers, interactive elements
- **Event-Driven**: Reactive property updates and rendering
- **Declarative Syntax**: Template-based UI composition

#### New GUI Components

- **BackgroundLayer**: Full-screen background rendering with multiple material types
  - Solid color backgrounds with effects (tint, saturation, fade)
  - Texture-based backgrounds with UV manipulation
  - Procedural generation support for dynamic backgrounds
  - Asset loading integration with automatic fallbacks

## Testing Strategy

The project follows a documentation-first, test-driven development approach:

1. **Documentation First**: Update documentation before code changes
2. **Red Phase**: Write failing tests that define expected behavior
3. **Green Phase**: Implement code to make tests pass
4. **Refactor**: Clean up implementation while maintaining test coverage

Test coverage includes:

- Unit tests for individual components and systems
- Integration tests for system interactions
- Resource management and memory lifecycle tests
- Graphics rendering validation tests

## Build and Development

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022 or VS Code
- OpenGL 3.3+ compatible graphics hardware

### Build Commands

```bash
# Build the solution
dotnet build Nexus.GameEngine.sln --configuration Debug

# Run all tests
dotnet test

# Build and pack NuGet packages
./build-publish-local.ps1
```

### Adding New Components

When adding new components to the engine:

1. Update this documentation with the new component's purpose and architecture
2. Create comprehensive unit tests covering all functionality
3. Implement the component following the IRuntimeComponent pattern
4. Add integration tests for system interactions
5. Update README.md with usage examples if the component is part of the public API

### Resource Management Guidelines

When working with graphics resources:

1. Use the attribute-based resource system for shared resources
2. Declare persistent resources in static classes (Geometry, Shaders, CommonAssets)
3. Create component-scoped resources for component-specific assets
4. Let the resource manager handle lifecycle management automatically
5. Test memory management behavior with unit tests

## Application Lifecycle and Startup

### Startup Pattern

The engine uses a deferred creation pattern to ensure proper initialization order:

```csharp
// CORRECT: Use StartupTemplate for deferred creation
var app = services.GetRequiredService<IApplication>();
app.StartupTemplate = Templates.MainMenu;  // Created after window initialization
await app.RunAsync();
```

```csharp
// INCORRECT: Don't create InputContext-dependent components before app.RunAsync()
var contentManager = services.GetRequiredService<IContentManager>();
var mainMenu = contentManager.GetOrCreate(Templates.MainMenu);  // Will fail if contains KeyBinding
var renderer = services.GetRequiredService<IRenderer>();
renderer.Viewport.Content = mainMenu;  // InputContext not available yet
await app.RunAsync();
```

### Component Lifecycle Phases

1. **Service Registration**: Dependency injection container setup
2. **Application Creation**: IApplication service instantiated
3. **Template Assignment**: StartupTemplate set (no component creation yet)
4. **Window Initialization**: Window and InputContext created during app.RunAsync()
5. **Content Creation**: StartupTemplate instantiated after window is ready
6. **Component Activation**: Components activated and event subscriptions established
7. **Game Loop**: Update and render cycles begin

### InputContext Dependencies

Components that depend on InputContext (input bindings, input maps) cannot be created before window initialization:

- **KeyBinding**: Requires InputContext for keyboard event subscription
- **MouseBinding**: Requires InputContext for mouse event subscription
- **InputMap**: Manages collections of input bindings
- **GamepadBinding**: Requires InputContext for gamepad event subscription

These components will throw `InvalidOperationException` if created before the window is ready.

## Architecture Principles

- **Composition over Inheritance**: Use component composition instead of deep inheritance hierarchies
- **Dependency Injection**: All dependencies injected through constructors
- **Immutable Configuration**: Use record types for component templates
- **Event-Driven**: Reactive updates through event subscriptions
- **Resource Efficiency**: Shared resources with automatic lifecycle management
- **Type Safety**: Strong typing throughout the API surface
- **Deferred Creation**: Use StartupTemplate pattern for InputContext-dependent components
