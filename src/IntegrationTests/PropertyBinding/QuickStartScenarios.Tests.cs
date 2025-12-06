using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Events;
using Xunit;
using System.Collections;

namespace IntegrationTests.PropertyBinding;

// --- Scenario 1 & 2 Components ---

public class PlayerCharacter : RuntimeComponent
{
    private float _health = 100f;
    public float Health 
    { 
        get => _health; 
        set 
        { 
            float old = _health;
            _health = value; 
            HealthChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value)); 
        } 
    }
    public event EventHandler<PropertyChangedEventArgs<float>>? HealthChanged;
}

public class HealthBar : RuntimeComponent
{
    public float CurrentHealth { get; set; }
}

public class TextDisplay : RuntimeComponent
{
    public string Text { get; set; } = "";
}

// --- Scenario 3 Components ---

public class AudioSettings : RuntimeComponent
{
    private float _masterVolume = 0.75f;
    public float MasterVolume 
    { 
        get => _masterVolume; 
        set 
        { 
            if (_masterVolume != value)
            {
                float old = _masterVolume;
                _masterVolume = value; 
                MasterVolumeChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value)); 
            }
        } 
    }
    public event EventHandler<PropertyChangedEventArgs<float>>? MasterVolumeChanged;
}

public class Slider : RuntimeComponent
{
    private float _value;
    public float Value 
    { 
        get => _value; 
        set 
        { 
            if (_value != value)
            {
                float old = _value;
                _value = value; 
                ValueChanged?.Invoke(this, new PropertyChangedEventArgs<float>(old, value)); 
            }
        } 
    }
    public event EventHandler<PropertyChangedEventArgs<float>>? ValueChanged;
}

public class PercentageConverter : Nexus.GameEngine.Data.IValueConverter, IBidirectionalConverter
{
    public object? Convert(object? value)
    {
        if (value is float f) return f * 100f;
        return 0f;
    }

    public object? ConvertBack(object? value)
    {
        if (value is float f) return f / 100f;
        return 0f;
    }
}

// --- Scenario 4 Components ---

public class ScoreCounter : RuntimeComponent
{
    private int _score = 0;
    public int Score 
    { 
        get => _score; 
        set 
        { 
            int old = _score;
            _score = value; 
            ScoreChanged?.Invoke(this, new PropertyChangedEventArgs<int>(old, value)); 
        } 
    }
    public event EventHandler<PropertyChangedEventArgs<int>>? ScoreChanged;
}

public class ScoreDisplay : RuntimeComponent
{
    public int DisplayValue { get; set; }
}

// --- Scenario 5 Components ---

public class ThemeContext : RuntimeComponent
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

public class ThemedButton : RuntimeComponent
{
    public string BackgroundColor { get; set; } = "";
}

public class ThemedPanel : RuntimeComponent
{
    public string BorderColor { get; set; } = "";
}

// --- Helper for Manual Bindings ---
public class ManualBindings : PropertyBindings
{
    private readonly List<(string, Nexus.GameEngine.Components.PropertyBinding)> _bindings = new();
    public void Add(string name, Nexus.GameEngine.Components.PropertyBinding binding) => _bindings.Add((name, binding));
    public override IEnumerator<(string propertyName, Nexus.GameEngine.Components.PropertyBinding binding)> GetEnumerator() => _bindings.GetEnumerator();
}

public class QuickStartScenariosTests
{
    [Fact]
    public void Scenario1_BasicParentChildBinding()
    {
        // Setup
        var bindings = new ManualBindings();
        bindings.Add("CurrentHealth", Binding.FromParent<PlayerCharacter>(p => p.Health));

        var player = new PlayerCharacter();
        var healthBar = new HealthBar();
        
        player.Load(new Template());
        player.AddChild(healthBar);
        
        healthBar.Load(new Template { Bindings = bindings });
        
        // Act: Activate to trigger bindings
        player.Activate();
        
        // Assert: Initial Sync
        Assert.Equal(100f, healthBar.CurrentHealth);
        
        // Act: Change Parent Health
        player.Health = 50f;
        
        // Assert: Child updates
        Assert.Equal(50f, healthBar.CurrentHealth);
    }

    [Fact]
    public void Scenario1_DisplayNumericValueAsText()
    {
        var bindings = new ManualBindings();
        bindings.Add("Text", Binding.FromParent<PlayerCharacter>(p => p.Health)
                                    .AsFormattedString("Health: {0:F1}"));

        var player = new PlayerCharacter();
        player.Health = 75.5f;
        
        var textDisplay = new TextDisplay();
        
        player.Load(new Template());
        player.AddChild(textDisplay);
        
        textDisplay.Load(new Template { Bindings = bindings });
        
        player.Activate();
        
        // Assert: Initial value formatted
        Assert.Equal("Health: 75.5", textDisplay.Text);
        
        // Act: Change value
        player.Health = 100f;
        
        // Assert: Updated value formatted
        Assert.Equal("Health: 100.0", textDisplay.Text);
    }

    [Fact]
    public void Scenario2_BindToNamedComponent()
    {
        var bindings = new ManualBindings();
        bindings.Add("CurrentHealth", Binding.FromNamedObject<PlayerCharacter>("Player", p => p.Health));

        var root = new RuntimeComponent();
        var player = new PlayerCharacter();
        var healthBar = new HealthBar();
        
        root.Load(new Template());
        root.AddChild(player);
        root.AddChild(healthBar);
        
        player.Load(new Template { Name = "Player" });
        player.Health = 80f;
        
        healthBar.Load(new Template { Bindings = bindings });
        
        root.Activate();
        
        // Assert: Initial sync
        Assert.Equal(80f, healthBar.CurrentHealth);
        
        // Act
        player.Health = 20f;
        
        // Assert
        Assert.Equal(20f, healthBar.CurrentHealth);
    }

    [Fact]
    public void Scenario3_TwoWayBinding()
    {
        var bindings = new ManualBindings();
        bindings.Add("Value", Binding.FromContext<AudioSettings>(s => s.MasterVolume)
                                     .TwoWay()
                                     .WithConverter(new PercentageConverter()));

        var settings = new AudioSettings();
        settings.MasterVolume = 0.75f;
        
        var slider = new Slider();
        
        settings.Load(new Template());
        settings.AddChild(slider);
        
        slider.Load(new Template { Bindings = bindings });
        
        settings.Activate();
        
        // Assert: Initial sync (0.75 -> 75)
        Assert.Equal(75f, slider.Value);
        
        // Act 1: Source updates Target
        settings.MasterVolume = 0.5f;
        Assert.Equal(50f, slider.Value);
        
        // Act 2: Target updates Source
        slider.Value = 25f;
        Assert.Equal(0.25f, settings.MasterVolume);
    }

    [Fact]
    public void Scenario4_BindBetweenSiblings()
    {
        var bindings = new ManualBindings();
        bindings.Add("DisplayValue", Binding.FromSibling<ScoreCounter>(s => s.Score));

        var container = new RuntimeComponent();
        var counter = new ScoreCounter();
        var display = new ScoreDisplay();
        
        container.Load(new Template());
        container.AddChild(counter);
        container.AddChild(display);
        
        counter.Load(new Template());
        counter.Score = 10;
        
        display.Load(new Template { Bindings = bindings });
        
        container.Activate();
        
        // Assert
        Assert.Equal(10, display.DisplayValue);
        
        // Act
        counter.Score = 50;
        
        // Assert
        Assert.Equal(50, display.DisplayValue);
    }

    [Fact]
    public void Scenario5_ContextBasedTheming()
    {
        var btnBindings = new ManualBindings();
        btnBindings.Add("BackgroundColor", Binding.FromContext<ThemeContext>(t => t.PrimaryColor));
        
        var panelBindings = new ManualBindings();
        panelBindings.Add("BorderColor", Binding.FromContext<ThemeContext>(t => t.PrimaryColor));

        var theme = new ThemeContext();
        theme.PrimaryColor = "Red";
        
        var button = new ThemedButton();
        var panel = new ThemedPanel();
        
        theme.Load(new Template());
        theme.AddChild(button);
        theme.AddChild(panel);
        
        button.Load(new Template { Bindings = btnBindings });
        panel.Load(new Template { Bindings = panelBindings });
        
        theme.Activate();
        
        // Assert
        Assert.Equal("Red", button.BackgroundColor);
        Assert.Equal("Red", panel.BorderColor);
        
        // Act
        theme.PrimaryColor = "Blue";
        
        // Assert
        Assert.Equal("Blue", button.BackgroundColor);
        Assert.Equal("Blue", panel.BorderColor);
    }
}
