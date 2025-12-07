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

The engine is built around a hierarchical component system with a unified `Component` base class:

- **Unified Component Class**: A single `Component` class (defined via partial classes) provides identity, configuration, hierarchy, and lifecycle capabilities.
  - **Component.Identity.cs**: Entity identification (Name, Tags)
  - **Component.Configuration.cs**: Template loading and validation
  - **Component.Hierarchy.cs**: Parent-child relationships and tree navigation
  - **Component.Lifecycle.cs**: Activation, updates, and disposal
- **Component Trees**: All game objects, UI elements, and systems form parent-child hierarchies.
- **Behavior Interfaces**: Components implement only the behaviors they need:
  - `IActivatable`: Lifecycle activation/deactivation
  - `IUpdatable`: Per-frame update logic
  - `IDrawable`: Rendering capabilities
  - `ILoadable`: Template configuration
  - `IValidatable`: State validation
- **Template Configuration**: Components are configured using strongly-typed template records.
- **Dependency Injection**: Components receive dependencies through constructor injection.
- **Property Bindings**: Type-safe property synchronization using `PropertyBinding<TSource, TValue>` with fluent API.

#### Transform System

The engine uses a unified 3D transform system for all spatial objects:

- **ITransformable**: Interface for components with position, rotation, and scale in 3D space
- **Coordinate System**: Right-handed, -Z forward, +Y up (Vulkan/Silk.NET conventions)
- **Transform Properties**: 
  - `Position` (Vector3D<float>): Local position relative to parent
  - `Rotation` (Quaternion<float>): Local rotation with SLERP interpolation
  - `Scale` (Vector3D<float>): Non-uniform scaling
- **Transform Matrices**:
  - `LocalMatrix`: Transform relative to parent (Scale → Rotate → Translate)
  - `WorldMatrix`: Absolute transform in world space (computed on-demand from hierarchy)
- **Direction Vectors**: Forward, Right, Up computed from rotation (local and world space)
- **Parent/Child Hierarchy**: Lazy computation of world transforms by walking up parent chain
- **Animated Properties**: All transform properties support interpolation via ComponentProperty attribute
- **2D and 3D**: 2D games use orthographic cameras; all transforms are fundamentally 3D

### Content Management and Camera Tracking

The **IContentManager** service manages component lifecycle and tracks active cameras in the component tree:

- **Camera Registration**: All ICamera components are automatically registered when loaded
- **Default Camera**: A default UI camera is auto-created on initialization for zero-configuration rendering
- **Active Cameras**: The `ActiveCameras` property provides access to all active, registered cameras
- **Tree Walking**: Camera registration happens via tree traversal during component Load/Unload
- **Lifecycle Integration**: Cameras are registered/unregistered automatically with content lifecycle
- **Batteries Included**: Applications work immediately without explicit camera or viewport setup

This "batteries included" approach enables simple applications to render UI and content without any camera configuration, while still supporting advanced multi-camera scenarios for split-screen, minimaps, and effects rendering.

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
- **Font Resources**: Font atlases with embedded glyph geometry (see Font Rendering System)

### Font Rendering System

The font rendering system provides efficient text display using font atlases and shared geometry:

- **Font Atlas Generation**: TrueType fonts rasterized into GPU textures (R8_UNORM format)
- **Shared Geometry**: Single vertex buffer per font shared by all TextElements (~6KB for ASCII)
- **Resource Caching**: IFontResourceManager caches font atlases with reference counting
- **Per-Glyph Rendering**: Each character emits a DrawCommand referencing shared geometry
- **Batch Optimization**: N text DrawCommands batch into 1 GPU draw call
- **StbTrueTypeSharp**: Pure C# TrueType parsing and glyph rasterization

#### Font Resource Components

- **IFontResourceManager**: Manages font atlas lifecycle and caching
- **FontResource**: GPU-resident font data (atlas texture, shared geometry, glyph metrics)
- **FontDefinition**: Immutable font configuration (source, size, character range)
- **GlyphInfo**: Per-character metrics and UV coordinates
- **IFontSource**: Abstraction for loading font data (embedded, file, URI)
- **TextElement**: UI component for rendering text using font atlases

### Graphics System

The graphics system provides a flexible rendering pipeline:

- **IRenderer**: Core rendering interface with OpenGL access
- **IDrawable**: Interface for components that can be rendered
- **Render Passes**: Configurable multi-pass rendering pipeline
- **Resource Sharing**: Efficient sharing of geometry, shaders, and textures

### GUI System

Component-based user interface system:

- **Layout Management**: Flexible layout containers (Vertical, Horizontal, Grid)
- **Visual Components**: Text, borders, background layers, interactive elements
- **Event-Driven**: Reactive property updates and rendering
- **Declarative Syntax**: Template-based UI composition
- **Texture Support**: Elements can render with textures or solid colors using uber-shader pipeline

#### GUI Components

- **Element**: Base UI component with texture support
  - Texture property with WhiteDummy default for solid color rendering
  - ComponentProperty attributes for deferred updates
  - UIElementPushConstants for model matrix and tint color
  - TexturedQuad geometry for UV-mapped rendering
  - Descriptor set binding for texture sampler (set=1, binding=0)
  - Uber-shader pipeline supporting both textured and solid color rendering

- **TextElement**: Text rendering component using font atlases
  - Text property with TemplateProperty and ComponentProperty attributes
  - Font atlas texture binding via descriptor sets
  - Per-glyph DrawCommand emission with shared geometry
  - Automatic text measurement and Size calculation
  - Position, AnchorPoint, Scale inherited from Element
  - Reuses UIElement shader for rendering (no new shaders required)

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
