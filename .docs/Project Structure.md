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

## Architecture Principles

- **Composition over Inheritance**: Use component composition instead of deep inheritance hierarchies
- **Dependency Injection**: All dependencies injected through constructors
- **Immutable Configuration**: Use record types for component templates
- **Event-Driven**: Reactive updates through event subscriptions
- **Resource Efficiency**: Shared resources with automatic lifecycle management
- **Type Safety**: Strong typing throughout the API surface
