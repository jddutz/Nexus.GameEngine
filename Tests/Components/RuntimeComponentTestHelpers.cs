using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Components;

namespace Tests.Components;

/// <summary>
/// Shared test helpers and utilities for RuntimeComponent tests
/// </summary>
public static class RuntimeComponentTestHelpers
{
    public static Mock<ILogger> CreateMockLogger()
    {
        return new Mock<ILogger>();
    }

    public static Mock<IComponentFactory> CreateMockFactory()
    {
        return new Mock<IComponentFactory>();
    }

    public static RuntimeComponent CreateComponent(ILogger? logger = null, string name = "TestComponent")
    {
        var component = new RuntimeComponent()
        {
            Logger = logger ?? CreateMockLogger().Object,
            Name = name
        };
        return component;
    }

    public static Mock<IRuntimeComponent> CreateMockChild(string name = "MockChild")
    {
        var mockChild = new Mock<IRuntimeComponent>();
        mockChild.Setup(c => c.Name).Returns(name);
        mockChild.Setup(c => c.Id).Returns(new ComponentId());
        mockChild.Setup(c => c.IsEnabled).Returns(true);
        mockChild.Setup(c => c.IsActive).Returns(false);
        mockChild.Setup(c => c.IsValid).Returns(true);
        mockChild.Setup(c => c.ValidationErrors).Returns(new List<ValidationError>());
        mockChild.Setup(c => c.Children).Returns(new List<IRuntimeComponent>());
        return mockChild;
    }

    public static RuntimeComponent.Template CreateTemplate(string name = "TestTemplate", bool enabled = true, IComponentTemplate[]? subcomponents = null)
    {
        return new RuntimeComponent.Template
        {
            Name = name,
            Enabled = enabled,
            Subcomponents = subcomponents ?? []
        };
    }
}

/// <summary>
/// Testable RuntimeComponent that allows access to protected virtual methods
/// </summary>
public class TestableRuntimeComponent : RuntimeComponent
{
    public TestableRuntimeComponent(ILogger logger)
    {
        Logger = logger;
    }

    public Func<IEnumerable<ValidationError>>? OnValidateCallback { get; set; }
    public Action<IComponentTemplate>? OnConfigureCallback { get; set; }
    public Action? OnDeactivateCallback { get; set; }
    public Action? OnActivateCallback { get; set; }
    public Action<double>? OnUpdateCallback { get; set; }
    public Action? OnDisposeCallback { get; set; }

    protected override IEnumerable<ValidationError> OnValidate()
    {
        return OnValidateCallback?.Invoke() ?? [];
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        OnConfigureCallback?.Invoke(componentTemplate);
    }

    protected override void OnDeactivate()
    {
        OnDeactivateCallback?.Invoke();
    }

    protected override void OnActivate()
    {
        OnActivateCallback?.Invoke();
    }

    protected override void OnUpdate(double deltaTime)
    {
        OnUpdateCallback?.Invoke(deltaTime);
    }

    protected override void OnDispose()
    {
        OnDisposeCallback?.Invoke();
    }
}