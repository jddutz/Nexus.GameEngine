using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Events;
using System;
using System.Collections.Generic;

namespace IntegrationTests.PropertyBinding;

public class ContextBindingTests
{
    [Fact]
    public void ShouldBindToAncestorContext()
    {
        // Arrange
        // ThemeProvider (Context)
        //   - Container
        //     - Button (Target)
        
        var themeProvider = new ThemeProvider { PrimaryColor = "Blue" };
        var container = new ContainerComponent();
        var button = new ButtonComponent();
        
        // Build hierarchy
        themeProvider.AddChild(container);
        container.AddChild(button);
        
        // Setup template with binding
        var template = new Template
        {
            Bindings = new ManualBindings
            {
                Color = Nexus.GameEngine.Components.PropertyBinding.FromContext<ThemeProvider>()
                               .GetPropertyValue("PrimaryColor")
            }
        };
        
        // Act - Load & Activate
        themeProvider.Load(new Template());
        container.Load(new Template());
        button.Load(template);
        
        themeProvider.Activate();
        
        // Assert - Initial Sync
        Assert.Equal("Blue", button.Color);
        
        // Act - Update
        themeProvider.PrimaryColor = "Red";
        
        // Assert - Update Sync
        Assert.Equal("Red", button.Color);
    }

    public class ThemeProvider : RuntimeComponent
    {
        private string _primaryColor = "White";
        public string PrimaryColor
        {
            get => _primaryColor;
            set
            {
                string old = _primaryColor;
                _primaryColor = value;
                PrimaryColorChanged?.Invoke(this, new PropertyChangedEventArgs<string>(old, value));
            }
        }
        public event EventHandler<PropertyChangedEventArgs<string>>? PrimaryColorChanged;
    }

    public class ContainerComponent : RuntimeComponent { }

    public class ButtonComponent : RuntimeComponent
    {
        public string? Color { get; set; }
    }

    public class ManualBindings : PropertyBindings
    {
        public Nexus.GameEngine.Components.PropertyBinding? Color { get; init; }
        
        public override IEnumerator<(string propertyName, Nexus.GameEngine.Components.PropertyBinding binding)> GetEnumerator()
        {
            if (Color != null) yield return ("Color", Color);
        }
    }
}
