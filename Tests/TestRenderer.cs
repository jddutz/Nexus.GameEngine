using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.Graphics.Rendering.Extensions;
using Silk.NET.OpenGL;

namespace Tests;

public class TestRenderer(GL gl) : IRenderer
{
    public readonly Dictionary<string, object> _sharedResources = [];
    private readonly Dictionary<object, DrawBatch> _batches = [];

    public GL GL { get; set; } = gl;
    public IRuntimeComponent? RootComponent { get; set; }
    public IReadOnlyList<ICamera> Cameras => [];

    public List<DrawBatch> Batches => [.. _batches.Values];

    public DrawBatch GetOrCreateBatch(object batchKey)
    {
        if (!_batches.TryGetValue(batchKey, out var batch))
        {
            batch = new DrawBatch { BatchKey = batchKey };
            _batches[batchKey] = batch;
        }
        return batch;
    }

    public RenderPassConfiguration[] Passes =>
    [
        new RenderPassConfiguration
        {
            Id = 0,
            Name = "Test",
            DirectRenderMode = true,
            DepthTestEnabled = true,
            BlendingMode = BlendingMode.None
        }
    ];

    public T GetSharedResource<T>(string name)
    {
        if (_sharedResources.TryGetValue(name, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return default(T)!;
    }

    public void SetSharedResource<T>(string name, T resource)
    {
        if (resource != null)
        {
            _sharedResources[name] = resource;
        }
    }

    public void RenderFrame()
    {
        // Simple test implementation - just clear the screen
        GL.ClearColor(0.2f, 0.3f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Apply render pass settings
        foreach (var pass in Passes)
        {
            // Configure depth testing
            if (pass.DepthTestEnabled)
            {
                GL.Enable(EnableCap.DepthTest);
            }
            else
            {
                GL.Disable(EnableCap.DepthTest);
            }

            // Configure blending
            GL.SetBlending(pass.BlendingMode);
        }
    }
}