using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Components;
using static Tests.Components.RuntimeComponentTestHelpers;

namespace Tests.Components;

/// <summary>
/// Tests for basic RuntimeComponent properties, initialization, and INotifyPropertyChanged behavior
/// </summary>
public class RuntimeComponentBasicTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IComponentFactory> _mockFactory;
    private readonly RuntimeComponent _component;

    public RuntimeComponentBasicTests()
    {
        _mockLogger = CreateMockLogger();
        _mockFactory = CreateMockFactory();
        _component = new RuntimeComponent()
        {
            Logger = _mockLogger.Object
        };
    }

    #region Basic Properties Tests

    [Fact]
    public void Id_DefaultValue_IsNone()
    {
        // Act & Assert
        Assert.Equal(ComponentId.None, _component.Id);
    }

    [Fact]
    public void Id_SetValue_UpdatesProperty()
    {
        // Arrange
        var newId = new ComponentId();

        // Act
        _component.Id = newId;

        // Assert
        Assert.Equal(newId, _component.Id);
    }

    [Fact]
    public void Id_SetSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _component.PropertyChanged += (_, _) => eventRaised = true;
        var currentId = _component.Id;

        // Act
        _component.Id = currentId;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void Name_DefaultValue_IsEmpty()
    {
        // Act & Assert
        Assert.Equal(string.Empty, _component.Name);
    }

    [Fact]
    public void Name_SetValue_UpdatesProperty()
    {
        // Arrange
        const string testName = "TestComponent";

        // Act
        _component.Name = testName;

        // Assert
        Assert.Equal(testName, _component.Name);
    }

    [Fact]
    public void Name_SetSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        const string testName = "TestComponent";
        _component.Name = testName;
        var eventRaised = false;
        _component.PropertyChanged += (_, _) => eventRaised = true;

        // Act
        _component.Name = testName;

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void IsEnabled_DefaultValue_IsTrue()
    {
        // Act & Assert
        Assert.True(_component.IsEnabled);
    }

    [Fact]
    public void IsEnabled_SetValue_UpdatesProperty()
    {
        // Act
        _component.IsEnabled = false;

        // Assert
        Assert.False(_component.IsEnabled);
    }

    [Fact]
    public void IsEnabled_SetSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var eventRaised = false;
        _component.PropertyChanged += (_, _) => eventRaised = true;

        // Act
        _component.IsEnabled = true; // Same as default

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void IsConfigured_DefaultValue_IsTrue()
    {
        // Act & Assert
        Assert.True(_component.IsConfigured);
    }

    [Fact]
    public void IsConfigured_SetValue_UpdatesProperty()
    {
        // Act
        _component.IsConfigured = false;

        // Assert
        Assert.False(_component.IsConfigured);
    }

    [Fact]
    public void ComponentFactory_DefaultValue_IsNull()
    {
        // Act & Assert
        Assert.Null(_component.ComponentFactory);
    }

    [Fact]
    public void ComponentFactory_SetValue_UpdatesProperty()
    {
        // Act
        _component.ComponentFactory = _mockFactory.Object;

        // Assert
        Assert.Equal(_mockFactory.Object, _component.ComponentFactory);
    }

    #endregion

    #region INotifyPropertyChanged Tests

    [Fact]
    public void PropertyChanged_SettingId_RaisesEvent()
    {
        // Arrange
        string? propertyName = null;
        _component.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        _component.Id = new ComponentId();

        // Assert
        Assert.Equal(nameof(_component.Id), propertyName);
    }

    [Fact]
    public void PropertyChanged_SettingName_RaisesEvent()
    {
        // Arrange
        string? propertyName = null;
        _component.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        _component.Name = "NewName";

        // Assert
        Assert.Equal(nameof(_component.Name), propertyName);
    }

    [Fact]
    public void PropertyChanged_SettingIsEnabled_RaisesEvent()
    {
        // Arrange
        string? propertyName = null;
        _component.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        _component.IsEnabled = false;

        // Assert
        Assert.Equal(nameof(_component.IsEnabled), propertyName);
    }

    [Fact]
    public void PropertyChanged_SettingComponentFactory_RaisesEvent()
    {
        // Arrange
        string? propertyName = null;
        _component.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        _component.ComponentFactory = _mockFactory.Object;

        // Assert
        Assert.Equal(nameof(_component.ComponentFactory), propertyName);
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void ComponentFactory_Property_CanBeSet()
    {
        // Act
        _component.ComponentFactory = _mockFactory.Object;

        // Assert
        Assert.Equal(_mockFactory.Object, _component.ComponentFactory);
    }

    [Fact]
    public void ComponentFactory_Property_RaisesPropertyChangedEvent()
    {
        // Arrange
        string? propertyName = null;
        _component.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        _component.ComponentFactory = _mockFactory.Object;

        // Assert
        Assert.Equal(nameof(_component.ComponentFactory), propertyName);
    }

    #endregion
}