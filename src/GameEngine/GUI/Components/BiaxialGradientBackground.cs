﻿using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Full-screen background with biaxial (4-corner) gradient.
/// Each corner has a distinct color, and the fragment shader performs bilinear interpolation.
/// Useful for complex color blending and corner-to-corner gradients.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class BiaxialGradientBackground(
    IPipelineManager pipelineManager,
    IResourceManager resources,
    IBufferManager bufferManager,
    IDescriptorManager descriptorManager)
    : DrawableComponent, IDrawable
{
    /// <summary>
    /// Template for configuring BiaxialGradientBackground components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Color at the top-left corner.
        /// Default: Black
        /// </summary>
        public Vector4D<float> TopLeft { get; set; } = Colors.Black;
        
        /// <summary>
        /// Color at the top-right corner.
        /// Default: Black
        /// </summary>
        public Vector4D<float> TopRight { get; set; } = Colors.Black;
        
        /// <summary>
        /// Color at the bottom-left corner.
        /// Default: Black
        /// </summary>
        public Vector4D<float> BottomLeft { get; set; } = Colors.Black;
        
        /// <summary>
        /// Color at the bottom-right corner.
        /// Default: Black
        /// </summary>
        public Vector4D<float> BottomRight { get; set; } = Colors.Black;
    }

    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;
    
    // UBO and descriptor set for corner colors
    private Silk.NET.Vulkan.Buffer? _colorUboBuffer;
    private DeviceMemory? _colorUboMemory;
    private DescriptorSet? _colorDescriptorSet;

    /// <summary>
    /// Corner colors. Can be animated using ComponentProperty.
    /// </summary>
    [ComponentProperty]
    private Vector4D<float> _topLeft = Colors.Black;

    [ComponentProperty]
    private Vector4D<float> _topRight = Colors.Black;

    [ComponentProperty]
    private Vector4D<float> _bottomLeft = Colors.Black;

    [ComponentProperty]
    private Vector4D<float> _bottomRight = Colors.Black;

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            SetTopLeft(template.TopLeft);
            SetTopRight(template.TopRight);
            SetBottomLeft(template.BottomLeft);
            SetBottomRight(template.BottomRight);
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Build pipeline for biaxial gradient rendering
        _pipeline = pipelineManager.GetBuilder()
            .WithShader(ShaderDefinitions.BiaxialGradient)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
            .WithDepthTest()
            .WithDepthWrite()
            .Build("BiaxialGradientBackground_Pipeline");


        // Create position-only full-screen quad geometry
        _geometry = resources.Geometry.GetOrCreate(GeometryDefinitions.UniformColorQuad);

        // Create UBO and descriptor set for corner colors
        CreateCornerColorsUBO();
    }

    /// <summary>
    /// Creates a UBO buffer and descriptor set for the corner colors.
    /// </summary>
    private void CreateCornerColorsUBO()
    {
        
        // Create UBO structure
        var ubo = CornerColorsUBO.FromCorners(_topLeft, _topRight, _bottomLeft, _bottomRight);
        
        // Create uniform buffer
        var uboSize = CornerColorsUBO.SizeInBytes;
        (_colorUboBuffer, _colorUboMemory) = bufferManager.CreateUniformBuffer(uboSize);
        
        // Upload UBO data to buffer
        var uboBytes = ubo.AsBytes();
        bufferManager.UpdateUniformBuffer(_colorUboMemory.Value, uboBytes);
        
        
        // Get or create descriptor set layout
        var shader = ShaderDefinitions.BiaxialGradient;
        if (shader.DescriptorSetLayoutBindings == null || shader.DescriptorSetLayoutBindings.Length == 0)
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout bindings");
        }
        
        var layout = descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayoutBindings);
        
        // Allocate descriptor set
        _colorDescriptorSet = descriptorManager.AllocateDescriptorSet(layout);
        
        // Update descriptor set to point to our UBO buffer
        descriptorManager.UpdateDescriptorSet(
            _colorDescriptorSet.Value,
            _colorUboBuffer.Value,
            uboSize,
            0);  // binding = 0
    }

    /// <summary>
    /// Updates the UBO when corner colors change during animation.
    /// </summary>
    private void UpdateCornerColorsUBO()
    {
        if (!_colorUboMemory.HasValue)
            return;
        
        var ubo = CornerColorsUBO.FromCorners(_topLeft, _topRight, _bottomLeft, _bottomRight);
        var uboBytes = ubo.AsBytes();
        bufferManager.UpdateUniformBuffer(_colorUboMemory.Value, uboBytes);
        
    }

    /// <summary>
    /// Property change callbacks - update UBO when colors change
    /// </summary>
    partial void OnTopLeftChanged(Vector4D<float> oldValue)
    {
        if (IsActive()) UpdateCornerColorsUBO();
    }

    partial void OnTopRightChanged(Vector4D<float> oldValue)
    {
        if (IsActive()) UpdateCornerColorsUBO();
    }

    partial void OnBottomLeftChanged(Vector4D<float> oldValue)
    {
        if (IsActive()) UpdateCornerColorsUBO();
    }

    partial void OnBottomRightChanged(Vector4D<float> oldValue)
    {
        if (IsActive()) UpdateCornerColorsUBO();
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_colorDescriptorSet.HasValue)
            yield break;

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Main,
            Pipeline = _pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            DescriptorSet = _colorDescriptorSet.Value
        };
    }

    protected override void OnDeactivate()
    {
        // Clean up UBO resources
        if (_colorUboBuffer.HasValue && _colorUboMemory.HasValue)
        {
            bufferManager.DestroyBuffer(_colorUboBuffer.Value, _colorUboMemory.Value);
            _colorUboBuffer = null;
            _colorUboMemory = null;
            
        }
        
        // Descriptor sets are freed automatically when the pool is reset
        if (_colorDescriptorSet.HasValue)
        {
            _colorDescriptorSet = null;
        }
        
        if (_geometry != null)
        {
            resources.Geometry.Release(GeometryDefinitions.UniformColorQuad);
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
