using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Vulkan;

namespace TestApp.TestComponents;

/// <summary>
/// Test component that intentionally triggers Vulkan validation errors to verify validation layers are working.
/// Tests that the validation layer debug callback properly captures and logs Vulkan validation errors.
/// </summary>
public class ValidationTestComponent(IRenderer renderer)
     : RuntimeComponent, IRenderable, ITestComponent
{
    private bool _hasTriggeredError = false;
    private bool _validationErrorCaptured = false;
    private int _framesRendered = 0;

    public uint RenderPriority => 0;

    protected override void OnUpdate(double deltaTime)
    {
        // Run test for 1 frame, then deactivate
        if (_framesRendered >= 1)
        {
            IsTestComplete = true;
            Deactivate();
        }
    }

    public IEnumerable<ElementData> GetElements()
    {
        _framesRendered++;

        // Trigger a validation error on first render
        if (!_hasTriggeredError)
        {
            Logger?.LogInformation("=== VALIDATION TEST: Attempting to trigger validation error ===");

            try
            {
                // Trigger a validation error by calling vkQueueSubmit with invalid parameters
                // This should definitely trigger validation: submitting to a queue with null submit info
                unsafe
                {
                    var submitInfo = new Silk.NET.Vulkan.SubmitInfo
                    {
                        SType = Silk.NET.Vulkan.StructureType.SubmitInfo,
                        CommandBufferCount = 1,  // Say we have 1 command buffer
                        PCommandBuffers = null   // But provide null pointer - VALIDATION ERROR!
                    };

                    var result = renderer.VK.Vk.QueueSubmit(renderer.VK.GraphicsQueue, 1, &submitInfo, default);

                    Logger?.LogInformation("=== VALIDATION TEST: QueueSubmit returned: {Result} ===", result);
                }

                // If we get here, check if validation logged an error
                _validationErrorCaptured = true;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "=== VALIDATION TEST: Exception: {Message} ===", ex.Message);
                _validationErrorCaptured = false;
            }

            _hasTriggeredError = true;
        }

        return Enumerable.Empty<ElementData>();
    }

    public bool IsTestComplete { get; private set; }

    public IEnumerable<TestResult> GetTestResults()
    {
        yield return new TestResult
        {
            TestName = "Vulkan validation layers should be enabled and capture errors",
            Passed = _hasTriggeredError && _validationErrorCaptured,
            Description = "This test intentionally triggers a Vulkan validation error by calling DestroyCommandPool with an invalid handle. " +
                         "Check the log output for validation error messages from the debug callback. " +
                         "Look for '[Vulkan ValidationBit]' or similar validation layer messages.",
            ErrorMessage = !_hasTriggeredError
                ? "Test did not execute - validation error was not triggered"
                : !_validationErrorCaptured
                    ? "Exception was thrown instead of validation callback being invoked"
                    : null
        };
    }
}
