using Silk.NET.Maths;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Uniform Buffer Object structure for gradient definitions.
/// Must match GLSL layout (std140) in linear_gradient.frag and radial_gradient.frag.
/// Layout:
///   - vec4[32] colors  = 512 bytes (32 × 16 bytes)
///   - float[32] positions = 128 bytes (32 × 4 bytes)
///   - int stopCount = 4 bytes
///   - padding = 12 bytes (for 16-byte alignment)
/// Total size: 656 bytes
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct GradientUBO
{
    /// <summary>
    /// Maximum number of gradient stops (must match shader)
    /// </summary>
    public const int MaxStops = 32;
    
    /// <summary>
    /// Colors at each gradient stop (RGBA)
    /// </summary>
    public fixed float Colors[MaxStops * 4];  // 32 vec4s = 512 bytes
    
    /// <summary>
    /// Normalized positions of each stop (0.0 to 1.0)
    /// </summary>
    public fixed float Positions[MaxStops];  // 32 floats = 128 bytes
    
    /// <summary>
    /// Number of active stops (2 to 32)
    /// </summary>
    public int StopCount;  // 4 bytes
    
    /// <summary>
    /// Padding to maintain std140 alignment (16-byte alignment requirement)
    /// </summary>
    private fixed int _padding[3];  // 12 bytes
    
    /// <summary>
    /// Gets the size of the UBO structure in bytes
    /// </summary>
    public static uint SizeInBytes => (uint)Unsafe.SizeOf<GradientUBO>();
    
    /// <summary>
    /// Creates a GradientUBO from a GradientDefinition
    /// </summary>
    public static GradientUBO FromGradientDefinition(GUI.GradientDefinition gradient)
    {
        if (gradient.Stops.Length > MaxStops)
        {
            throw new ArgumentException(
                $"Gradient has {gradient.Stops.Length} stops, but maximum is {MaxStops}");
        }
        
        var ubo = new GradientUBO
        {
            StopCount = gradient.Stops.Length
        };
        
        // Copy colors and positions
        for (int i = 0; i < gradient.Stops.Length; i++)
        {
            var stop = gradient.Stops[i];
            
            // Copy color (vec4)
            ubo.Colors[i * 4 + 0] = stop.Color.X;
            ubo.Colors[i * 4 + 1] = stop.Color.Y;
            ubo.Colors[i * 4 + 2] = stop.Color.Z;
            ubo.Colors[i * 4 + 3] = stop.Color.W;
            
            // Copy position
            ubo.Positions[i] = stop.Position;
        }
        
        return ubo;
    }
    
    /// <summary>
    /// Gets the UBO data as a byte span for buffer upload
    /// </summary>
    public ReadOnlySpan<byte> AsBytes()
    {
        ReadOnlySpan<GradientUBO> span = new ReadOnlySpan<GradientUBO>(ref Unsafe.AsRef(in this));
        return MemoryMarshal.AsBytes(span);
    }
}
