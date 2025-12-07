# Data Model: Systems Architecture

**Date**: December 6, 2025  
**Feature**: Systems Architecture Pattern  
**Purpose**: Define entities, relationships, and state machines for systems pattern implementation

## Entity Definitions

### 1. System (Abstract Concept)

A **System** represents a cohesive grouping of framework services accessed via marker interface and extension methods.

**Properties**:
- `Name`: string - Logical name (e.g., "Graphics", "Resources", "Content")
- `MarkerInterface`: Type - Empty interface serving as extension point (e.g., `IGraphicsSystem`)
- `Implementation`: Type - Internal sealed class wrapping services (e.g., `GraphicsSystem`)
- `Services`: IReadOnlyList<Type> - Framework services wrapped by system (e.g., `IGraphicsContext`, `IPipelineManager`)

**Relationships**:
- Has many **Extension Methods** (1-to-many)
- Wraps many **Framework Services** (1-to-many)
- Referenced by **Framework Classes** via properties (many-to-many)

**Invariants**:
- Marker interface MUST be empty (no members)
- Implementation MUST be internal sealed
- Implementation MUST wrap at least one framework service
- Extension methods MUST operate on marker interface

**Example**:
```csharp
// Marker interface (empty)
public interface IGraphicsSystem { }

// Implementation (internal sealed, wraps services)
internal sealed class GraphicsSystem : IGraphicsSystem
{
    public IGraphicsContext Context { get; }
    public IPipelineManager PipelineManager { get; }
    public ISwapChain SwapChain { get; }
    public ICommandPoolManager CommandPools { get; }
    public ISyncManager Sync { get; }
    public IDescriptorManager Descriptors { get; }
    
    public GraphicsSystem(
        IGraphicsContext context,
        IPipelineManager pipelineManager,
        ISwapChain swapChain,
        ICommandPoolManager commandPools,
        ISyncManager sync,
        IDescriptorManager descriptors)
    {
        Context = context;
        PipelineManager = pipelineManager;
        SwapChain = swapChain;
        CommandPools = commandPools;
        Sync = sync;
        Descriptors = descriptors;
    }
}
```

### 2. Extension Method

An **Extension Method** provides functionality for a system by operating on its marker interface.

**Properties**:
- `Name`: string - Method name (e.g., "DrawQuad", "LoadTexture")
- `System`: Type - Marker interface it extends (e.g., `IGraphicsSystem`)
- `Parameters`: IReadOnlyList<Parameter> - Method parameters
- `ReturnType`: Type - Return type (void or value)
- `AccessesImplementation`: bool - Whether it casts to internal implementation

**Relationships**:
- Extends one **System** marker interface (many-to-one)
- May delegate to **Framework Services** (many-to-many)

**Invariants**:
- First parameter MUST be `this {MarkerInterface}` (extension method syntax)
- MUST cast marker interface to implementation to access services
- MUST NOT expose implementation types in signature (keeps marker interface abstract)

**Example**:
```csharp
public static class GraphicsSystemExtensions
{
    // Cast helper (private)
    private static GraphicsSystem AsImpl(this IGraphicsSystem system) 
        => (GraphicsSystem)system;
    
    // Extension method
    public static void DrawQuad(
        this IGraphicsSystem graphics,
        Vector2 position,
        Vector2 size,
        Vector4 color)
    {
        var impl = graphics.AsImpl();
        var pipeline = impl.PipelineManager.GetOrCreate("quad-pipeline", ...);
        impl.CommandPools.CurrentBuffer.BindPipeline(pipeline);
        // ... rendering logic
    }
}
```

### 3. Framework Class

A **Framework Class** is any engine infrastructure class that requires access to framework services.

**Properties**:
- `Name`: string - Class name (e.g., "Renderer", "PipelineManager")
- `SystemProperties`: IReadOnlyList<SystemProperty> - System properties (e.g., `Graphics`, `Resources`)
- `RequiresSystems`: bool - Whether implements `IRequiresSystems` interface
- `ConstructorParameters`: IReadOnlyList<Parameter> - Constructor parameters (should be 0 for framework services)

**Relationships**:
- References **Systems** via properties (many-to-many)
- May be initialized by **DI Container** (many-to-one)

**Invariants**:
- MUST implement `IRequiresSystems` if using system properties
- System properties MUST have public getter and internal setter
- System properties MUST be initialized in `InitializeSystems()` method
- Constructor MUST NOT take framework services as parameters (domain dependencies allowed)

**Example**:
```csharp
public class Renderer : IRenderer, IRequiresSystems
{
    // System properties
    public IGraphicsSystem Graphics { get; internal set; } = null!;
    public IResourceSystem Resources { get; internal set; } = null!;
    
    // Parameterless constructor (no framework services)
    public Renderer() { }
    
    // Initialization method called by DI
    public void InitializeSystems(IServiceProvider serviceProvider)
    {
        Graphics = serviceProvider.GetRequiredService<IGraphicsSystem>();
        Resources = serviceProvider.GetRequiredService<IResourceSystem>();
    }
    
    // Methods use systems
    public void RenderFrame()
    {
        Graphics.BeginFrame();
        Graphics.DrawQuad(new Vector2(0, 0), new Vector2(100, 100), new Vector4(1, 0, 0, 1));
        Graphics.EndFrame();
    }
}
```

### 4. System Property

A **System Property** is a property on a framework class that provides access to a system.

**Properties**:
- `Name`: string - Property name (e.g., "Graphics", "Resources")
- `Type`: Type - System marker interface (e.g., `IGraphicsSystem`)
- `IsRequired`: bool - Whether property must be initialized (default true)
- `Owner`: Type - Framework class owning the property

**Relationships**:
- Owned by one **Framework Class** (many-to-one)
- References one **System** (many-to-one)

**Invariants**:
- Type MUST be a system marker interface
- MUST have public getter
- MUST have internal setter (prevents external modification)
- MUST be initialized in `IRequiresSystems.InitializeSystems()` method
- SHOULD be non-nullable reference type (enforced via `= null!;` and nullable context)

**Example**:
```csharp
// Required system property
public IGraphicsSystem Graphics { get; internal set; } = null!;

// Optional system property (rare)
public IInputSystem? Input { get; internal set; }
```

### 5. Framework Service (Existing Entity)

A **Framework Service** is an existing engine service that gets wrapped by a system.

**Properties**:
- `InterfaceType`: Type - Service interface (e.g., `IGraphicsContext`)
- `ImplementationType`: Type - Service implementation (e.g., `Context`)
- `Lifetime`: ServiceLifetime - DI lifetime (typically Singleton)
- `WrappedBySystem`: Type? - System that wraps this service

**Relationships**:
- Wrapped by zero or one **System** (many-to-one optional)
- Registered in **DI Container** (many-to-one)

**Invariants**:
- MUST be registered in DI container
- MUST remain unchanged (systems wrap, don't replace)
- MAY be accessed directly or through system

**Example Services**:
```csharp
// Graphics services wrapped by IGraphicsSystem
IGraphicsContext → GraphicsSystem.Context
IPipelineManager → GraphicsSystem.PipelineManager
ISwapChain → GraphicsSystem.SwapChain
ICommandPoolManager → GraphicsSystem.CommandPools
ISyncManager → GraphicsSystem.Sync
IDescriptorManager → GraphicsSystem.Descriptors

// Resource services wrapped by IResourceSystem
IResourceManager → ResourceSystem.Manager
IGeometryResourceManager → ResourceSystem.Geometry
IShaderResourceManager → ResourceSystem.Shaders
IBufferManager → ResourceSystem.Buffers

// Content services wrapped by IContentSystem
IContentManager → ContentSystem.Manager
IComponentFactory → ContentSystem.Factory

// Window services wrapped by IWindowSystem
IWindowService → WindowSystem.Window
IInputContext → WindowSystem.Input

// Input services wrapped by IInputSystem (future)
IKeyboard → InputSystem.Keyboard
IMouse → InputSystem.Mouse
```

## Entity Relationships

```
┌─────────────────┐
│     System      │
│  (Abstract)     │
└────────┬────────┘
         │ has many
         ▼
┌─────────────────────┐
│  Extension Method   │
│  (Static class)     │
└─────────────────────┘
         │ operates on
         ▼
┌──────────────────────────┐       ┌────────────────────┐
│   Marker Interface       │◄──────│  Framework Class   │
│   (Empty interface)      │       │  (Renderer, etc.)  │
└────────────┬─────────────┘       └──────────┬─────────┘
             │ implemented by                  │ has many
             ▼                                 ▼
┌────────────────────────┐         ┌────────────────────┐
│  System Implementation │         │  System Property   │
│  (Internal sealed)     │◄────────│  (Property)        │
└────────────┬───────────┘         └────────────────────┘
             │ wraps
             ▼
┌────────────────────────┐
│  Framework Service     │
│  (Existing services)   │
└────────────────────────┘
```

## State Transitions

### Framework Class Initialization State Machine

```
┌─────────────────┐
│  Constructed    │ (via DI factory or test)
│  Systems = null │
└────────┬────────┘
         │
         │ InitializeSystems() called
         ▼
┌─────────────────────┐
│  Systems Initialized│
│  Systems != null    │
└─────────────────────┘
         │
         │ Use systems
         ▼
┌─────────────────────┐
│   Operating         │
│   Normal execution  │
└─────────────────────┘
         │
         │ Dispose (if IDisposable)
         ▼
┌─────────────────────┐
│   Disposed          │
│   (Systems remain)  │
└─────────────────────┘
```

**State Definitions**:

1. **Constructed**: Instance created but systems not initialized
   - Valid transitions: → Systems Initialized (via InitializeSystems)
   - Invariants: System properties are null
   - Operations allowed: None (will throw if systems accessed)

2. **Systems Initialized**: Systems assigned and ready
   - Valid transitions: → Operating (via any method call)
   - Invariants: All required system properties != null
   - Operations allowed: All methods can safely access systems

3. **Operating**: Normal execution, systems in use
   - Valid transitions: → Disposed (if IDisposable)
   - Invariants: Systems remain valid
   - Operations allowed: All

4. **Disposed**: Resources released, but systems still referenced
   - Valid transitions: None (terminal state)
   - Invariants: Framework class resources released, systems unchanged (managed by DI)
   - Operations allowed: None

### Extension Method Execution Flow

```
┌──────────────────────┐
│  Extension called    │
│  graphics.DrawQuad() │
└──────────┬───────────┘
           │
           │ Cast to implementation
           ▼
┌──────────────────────┐
│  Access impl         │
│  var impl = AsImpl() │
└──────────┬───────────┘
           │
           │ Access wrapped services
           ▼
┌──────────────────────────┐
│  Use services            │
│  impl.PipelineManager... │
└──────────┬───────────────┘
           │
           │ Complete operation
           ▼
┌──────────────────────┐
│  Return result       │
└──────────────────────┘
```

## Validation Rules

### System Marker Interface Validation

- ✅ MUST be public interface
- ✅ MUST have zero members (empty interface)
- ✅ MUST NOT inherit from other interfaces (except marker interfaces)
- ✅ Name MUST follow pattern `I{Domain}System` (e.g., `IGraphicsSystem`)

### System Implementation Validation

- ✅ MUST be internal sealed class
- ✅ MUST implement corresponding marker interface
- ✅ MUST have constructor accepting all wrapped services
- ✅ MUST expose wrapped services as public properties
- ✅ Name MUST follow pattern `{Domain}System` (e.g., `GraphicsSystem`)

### Extension Method Validation

- ✅ MUST be public static method in public static class
- ✅ First parameter MUST be `this {MarkerInterface}`
- ✅ MUST NOT expose implementation types in signature
- ✅ Class name MUST follow pattern `{Domain}SystemExtensions`

### Framework Class Validation

- ✅ System properties MUST be public getter + internal setter
- ✅ MUST implement `IRequiresSystems` if has system properties
- ✅ Constructor MUST NOT accept framework services (domain only)
- ✅ `InitializeSystems()` MUST assign all required system properties

### DI Registration Validation

- ✅ All systems MUST be registered as singletons
- ✅ Framework classes with systems MUST use factory pattern
- ✅ Factory MUST call `InitializeSystems()` before returning instance

## Performance Characteristics

- **Extension Method Call**: O(1) - Direct static method call, JIT inlines
- **System Property Access**: O(1) - Direct property access
- **Cast to Implementation**: O(1) - Simple type cast, no boxing
- **Memory Overhead**: 5 singleton instances (one per system)
- **Initialization Time**: One-time cost at application startup

## Migration Checklist

For each framework class being migrated:

- [ ] Remove framework service constructor parameters
- [ ] Add system properties (public get, internal set)
- [ ] Implement `IRequiresSystems` interface
- [ ] Add `InitializeSystems()` method
- [ ] Update DI registration to use factory pattern
- [ ] Replace direct service access with system extension methods
- [ ] Update unit tests to use mock systems
- [ ] Verify integration tests pass unchanged
- [ ] Verify no performance degradation

## Summary

The systems architecture introduces five key entities: **System** (abstract concept), **Extension Method** (functionality), **Framework Class** (consumer), **System Property** (access point), and **Framework Service** (wrapped service). Framework classes access services through typed system properties initialized by DI, with extension methods providing discoverable operations. The pattern eliminates constructor injection while maintaining compile-time type safety and testability.
