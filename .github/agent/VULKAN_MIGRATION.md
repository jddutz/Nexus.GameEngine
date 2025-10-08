# Vulkan Migration Tracker

**Date Started**: October 6, 2025  
**Goal**: Rebuild the game engine from the ground up using Vulkan instead of OpenGL

## Phase 1: Package Migration ✅

- [x] Remove `Silk.NET.OpenGL` from GameEngine.csproj
- [x] Add `Silk.NET.Vulkan` (2.22.0) to GameEngine.csproj
- [x] Add `Silk.NET.Vulkan.Extensions.KHR` (2.22.0) to GameEngine.csproj
- [x] Remove `Silk.NET.OpenGL` from TestApp.csproj
- [x] Add `Silk.NET.Vulkan` (2.22.0) to TestApp.csproj
- [x] Add `Silk.NET.Vulkan.Extensions.KHR` (2.22.0) to TestApp.csproj
- [x] Build solution to identify all breaking changes

**Result**: 84 compilation errors across 30 files

## Phase 2: Analysis - Files Requiring Changes

### Critical Graphics System Files (Core Infrastructure)

These need to be completely rebuilt for Vulkan:

1. **Graphics/IRenderer.cs** - Core renderer interface (currently exposes `GL` object)
2. **Graphics/Renderer.cs** - Main renderer implementation
3. **Graphics/RenderContext.cs** - Render context management
4. **Graphics/IRenderable.cs** - Renderable component interface
5. **Graphics/ElementData.cs** - Render element data structure
6. **Graphics/IBatchStrategy.cs** - Batching strategy interface
7. **Graphics/DefaultBatchStrategy.cs** - Default batching implementation
8. **Graphics/RenderEventArgs.cs** - Render event arguments

### Resource Management (High Priority)

Need complete rewrite for Vulkan memory management:

9. **Resources/ResourceManager.cs** - Main resource manager
10. **Resources/ResourcePool.cs** - Resource pooling
11. **Resources/Geometry/VertexAttribute.cs** - Vertex attribute definitions

### Shader System (Critical)

Must be rebuilt for SPIR-V and pipeline state objects:

12. **Graphics/Shaders/ShaderManager.cs** - Shader compilation/management
13. **Graphics/Shaders/ManagedShader.cs** - Shader wrapper class

### Texture System

Needs rewrite for Vulkan image/sampler model:

14. **Graphics/Textures/TextureManager.cs** - Texture loading/management

### Buffer Management

Needs complete Vulkan buffer implementation:

15. **Graphics/Buffers/UniformBufferManager.cs** - Uniform buffer management
16. **Graphics/Buffers/PersistentMappedBuffer.cs** - Persistent mapped buffers

### Lighting System

Depends on shaders and uniforms - will need updates:

17. **Graphics/Lighting/LightingManager.cs** - Lighting system
18. **Graphics/Lighting/ShadowMapRenderer.cs** - Shadow mapping

### GL Extension Files (Can be Deleted or Repurposed)

These are OpenGL-specific utilities:

19. **Graphics/Extensions/GLBlendingExtensions.cs**
20. **Graphics/Extensions/GLErrorExtensions.cs**
21. **Graphics/Extensions/GLMeshExtensions.cs**
22. **Graphics/Extensions/GLShaderExtensions.cs**
23. **Graphics/Extensions/GLTextureExtensions.cs**

### Rendering State

24. **Graphics/IRenderStateFactory.cs** - Factory for render states
25. **Graphics/AdaptiveErrorRecovery.cs** - GL error recovery

### GUI Components (Medium Priority)

These depend on the graphics system being rebuilt first:

26. **GUI/Components/BackgroundLayer.cs**
27. **GUI/Components/HelloQuad.cs**
28. **GUI/Components/TextElement.cs**
29. **GUI/Abstractions/LayoutBase.cs**

### Sprite System

30. **Graphics/Sprites/SpriteComponent.cs**

## Phase 3: Architecture Planning

### Keeping from Current Design

- ✅ Component-based architecture (`IRuntimeComponent`)
- ✅ Template-based configuration
- ✅ Dependency injection
- ✅ Resource definition pattern (declarative resources)
- ✅ Viewport/Camera system concepts
- ✅ Event system
- ✅ Animation system (not graphics-dependent)
- ✅ Input system (Silk.NET.Input is API-agnostic)

### New Vulkan Architecture Needed

#### 1. Vulkan Context & Initialization

- **VulkanInstance** - Vulkan instance management
- **VulkanDevice** - Physical/logical device selection
- **VulkanSurface** - Window surface creation
- **VulkanSwapchain** - Swapchain management with resize handling
- **ValidationLayers** - Debug validation layer support

#### 2. Command Buffer System

- **CommandPool** - Command pool management
- **CommandBuffer** - Command buffer recording/submission
- **CommandQueue** - Queue family management (graphics, present, transfer)

#### 3. Pipeline System (replaces GL state)

- **GraphicsPipeline** - Graphics pipeline state objects
- **PipelineCache** - Pipeline caching for performance
- **PipelineLayout** - Descriptor set layouts and push constants
- **RenderPass** - Render pass and subpass definitions

#### 4. Memory & Resource Management

- **VulkanMemoryAllocator** - Memory allocation strategy (consider VMA library)
- **BufferManager** - Vertex, index, uniform, staging buffers
- **ImageManager** - Texture/image management with layout transitions
- **DescriptorPool** - Descriptor set allocation
- **DescriptorSet** - Resource binding (textures, uniforms)

#### 5. Synchronization

- **FenceManager** - CPU-GPU synchronization
- **SemaphoreManager** - GPU-GPU synchronization
- **FrameSync** - Frame-in-flight management (typically 2-3 frames)

#### 6. Shader System

- **ShaderModule** - SPIR-V shader module wrapper
- **ShaderCompiler** - Runtime GLSL->SPIR-V compilation (using shaderc)
- **VertexInputDescription** - Vertex attribute binding

## Phase 4: Implementation Strategy

### Step 1: Core Vulkan Infrastructure (Week 1-2)

1. Create basic Vulkan instance
2. Enumerate physical devices and select one
3. Create logical device with queue families
4. Create surface from Silk.NET window
5. Create swapchain with present mode selection
6. Implement basic frame synchronization

### Step 2: Command System (Week 2-3)

1. Create command pool per frame
2. Implement command buffer recording
3. Basic render pass setup (clear screen)
4. Submit and present pipeline
5. Handle window resize

### Step 3: Simple Triangle (Week 3-4)

1. Create vertex/index buffers with staging
2. Compile simple vertex/fragment shaders to SPIR-V
3. Create graphics pipeline
4. Record draw commands
5. See a triangle on screen!

### Step 4: Resource Management (Week 4-5)

1. Implement proper memory allocation
2. Buffer pooling system
3. Descriptor set management
4. Texture/image loading with staging

### Step 5: Rebuild Component System (Week 5-6)

1. Update `IRenderer` interface for Vulkan
2. Update `IRenderable` to return Vulkan-compatible data
3. Implement basic rendering of components
4. Test with simple shapes

### Step 6: Advanced Features (Week 6+)

1. Multi-pass rendering
2. Texture support
3. Uniform buffers and push constants
4. Pipeline caching
5. GUI rendering
6. Text rendering

## Current Status

**Phase**: 2 (Cleanup) - IN PROGRESS  
**Progress**: 84 → 54 errors (replaced OpenGL usings with Vulkan)  
**Next Phase**: 3 (Stubbing/Architecture)  
**Next Steps**:

1. ✅ Package migration complete
2. ✅ Global using replacement (OpenGL → Vulkan)
3. 🔄 Stub or comment out broken OpenGL-specific code
4. Create minimal Vulkan infrastructure to get clean build
5. Begin Phase 4, Step 1: Core Vulkan Infrastructure

## Error Breakdown (54 remaining)

### Category 1: GL Type References (~15 errors)

Files with `GL` type parameters/fields that need Vulkan equivalents:

- TextureManager.cs
- ShaderManager.cs
- ResourceManager.cs
- ResourcePool.cs
- Renderer.cs
- Various other managers

**Strategy**: Replace `GL` with placeholder `VulkanContext` or stub temporarily

### Category 2: OpenGL Enum Types (~30 errors)

Missing OpenGL-specific enums:

- `PrimitiveType`, `DrawElementsType` (ElementData.cs)
- `InternalFormat`, `PixelFormat`, `PixelType` (TextureManager.cs)
- `TextureMinFilter`, `TextureMagFilter`, `TextureWrapMode`
- `TextureTarget`, `TextureParameterName`
- `ShaderType`, `VertexAttribPointerType`
- `BufferUsageARB`

**Strategy**: Create placeholder enums or comment out usage temporarily

### Category 3: OpenGL API Calls (~9 errors)

Direct GL API calls that don't exist in Vulkan:

- `_gl.BindTexture()`
- `_gl.TexImage2D()`
- `_gl.TexParameter()`
- `_gl.GenerateMipmap()`
- etc.

**Strategy**: Comment out or stub for now, will be completely rewritten for Vulkan

## Notes

- This is a **learning project** - take time to understand Vulkan concepts
- Keep the component model - it's working well
- Don't rush - Vulkan is verbose but powerful
- Use validation layers extensively during development
- Consider using `vk-bootstrap` patterns from C++ for inspiration

## Resources

- Silk.NET Vulkan docs: https://github.com/dotnet/Silk.NET
- Vulkan Tutorial: https://vulkan-tutorial.com/
- Vulkan Guide: https://github.com/KhronosGroup/Vulkan-Guide
- Validation Layers: Essential for debugging
