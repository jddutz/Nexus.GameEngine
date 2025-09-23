using Nexus.GameEngine.Graphics.Rendering.Buffers;

namespace Tests.Graphics.Rendering.Lighting;

/// <summary>
/// Null implementation of IUniformBufferManager for testing
/// </summary>
public class NullUniformBufferManager : IUniformBufferManager
{
    private readonly UniformBlock _testBlock = new("TestBlock", 1024, 1u, 0);

    public bool IsDisposed => false;

    public IEnumerable<UniformBlock> ActiveBlocks => [_testBlock];

    public UniformBlock CreateBlock(string name, int size) => _testBlock;

    public UniformBlock? GetBlock(string name) => _testBlock;

    public void UpdateBlock(UniformBlock block, ReadOnlySpan<byte> data, int offset = 0)
    {
        // No-op for testing
    }

    public unsafe void UpdateBlockRaw(UniformBlock block, IntPtr dataPtr, int dataSize, int offset = 0)
    {
        // No-op for testing
    }

    public void BindBlock(UniformBlock block)
    {
        // No-op for testing
    }

    public void NextFrame()
    {
        // No-op for testing
    }

    public UniformBufferStatistics GetStatistics()
    {
        return new UniformBufferStatistics(1, 1024, 1, 0);
    }

    public void Dispose()
    {
        // No-op for testing
    }
}