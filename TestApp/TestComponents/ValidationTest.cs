using System.Runtime.Serialization;
using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Vulkan;

namespace TestApp.TestComponents;

/// <summary>
/// Unit test component that verifies validation layers are working by triggering specific validation scenarios
/// and checking that expected messages are logged. Uses a state machine approach: OnUpdate (Arrange), GetDrawCommands (Act), GetTestResults (Assert).
/// </summary>
[TestRunnerIgnore(reason: "Validation tests all passed but continue output false errors via console log")]
public partial class ValidationTest(IGraphicsContext context)
    : RuntimeComponent, IDrawable, ITestComponent
{
    private record TestData
    {
        public string Name = "";
        public string Regex = "";
        public bool Passed = false;
    }

    private int frameCount = 0;
    
    /// <summary>
    /// Whether this component should be rendered.
    /// When false, GetDrawCommands will not be called and component is skipped during rendering.
    /// Generated property: IsVisible (read-only), SetIsVisible(...) method for updates.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Fast, Interpolation = InterpolationMode.Step)]
    private bool _isVisible = true;


    private static readonly TestData[] testData = [
        new() {
            Name = "Validation layer should detect zero-size buffer creation",
            Regex = @"\[ERROR\]\s+VULKAN\s+\[.*\]\s+vkCreateBuffer\(\).*size.*zero"
        },
        new() {
            Name = "Validation layer should detect invalid queue family index",
            Regex = @"\[ERROR\]\s+VULKAN\s+\[.*\]\s+vkCreateCommandPool\(\).*queueFamilyIndex"
        },
        new() {
            Name = "Validation layer should detect double destruction of command pool",
            Regex = @"\[ERROR\]\s+VULKAN\s+\[.*\]\s+vkDestroyCommandPool\(\).*Invalid.*VkCommandPool"
        }
    ];

    protected override void OnActivate()
    {
        foreach (var test in testData)
        {
            TestLogger.Capture(test.Regex);
        }
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (!IsActive)
            yield break;

        switch (frameCount)
        {
            case 0:
                ActZeroSizeBuffer();
                break;
            case 1:
                ActInvalidQueueFamily();
                break;
            case 2:
                ActDoubleDestroy();
                break;
            default:
                // No more tests to run
                break;
        }

        frameCount++;
        
        if (frameCount >= 3) Deactivate();
    }

    private unsafe void ActZeroSizeBuffer()
    {
        try
        {
            // Create a buffer with zero size - this should trigger validation layer warning
            var bufferCreateInfo = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Size = 0, // Zero size should trigger validation warning
                Usage = BufferUsageFlags.VertexBufferBit,
                SharingMode = SharingMode.Exclusive
            };

            Silk.NET.Vulkan.Buffer buffer;
            var result = context.VulkanApi.CreateBuffer(context.Device, &bufferCreateInfo, null, &buffer);

            if (result == Result.Success)
            {
                // Clean up the buffer if it was created
                context.VulkanApi.DestroyBuffer(context.Device, buffer, null);
            }
        }
        catch { }
    }

    private unsafe void ActInvalidQueueFamily()
    {
        try
        {
            // Try to create a command pool with an invalid queue family index
            var poolCreateInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.TransientBit,
                QueueFamilyIndex = 9999 // Invalid queue family index should trigger validation error
            };

            CommandPool commandPool;
            var result = context.VulkanApi.CreateCommandPool(context.Device, &poolCreateInfo, null, &commandPool);

            if (result == Result.Success)
            {
                // Clean up if somehow created
                context.VulkanApi.DestroyCommandPool(context.Device, commandPool, null);
            }
        }
        catch { }
    }

    private unsafe void ActDoubleDestroy()
    {
        try
        {
            // Create a command pool first
            var poolCreateInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.TransientBit,
                QueueFamilyIndex = 0 // Use a valid queue family (graphics queue family)
            };

            CommandPool commandPool;
            var result = context.VulkanApi.CreateCommandPool(context.Device, &poolCreateInfo, null, &commandPool);

            if (result == Result.Success)
            {
                // Destroy it once (valid)
                context.VulkanApi.DestroyCommandPool(context.Device, commandPool, null);

                // Destroy it again (invalid) - this should trigger validation error
                context.VulkanApi.DestroyCommandPool(context.Device, commandPool, null);
            }
        }
        catch { }
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        foreach (var test in testData)
        {
            var results = TestLogger.StopCapture(test.Regex).ToList();
            test.Passed = results.Count > 0;
            
            yield return new()
            {
                TestName = test.Name,
                Description = "Testing VkValidationLayer implementation",
                ExpectedResult = "Log output results > 0",
                ActualResult = results.Count.ToString(),
                Passed = test.Passed
            };
        }
    }
}
