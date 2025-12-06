using System;
using System.Collections;
using System.Collections.Generic;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Data;

namespace TestApp.Examples;

// Example Source Component
public partial class HealthComponent : RuntimeComponent
{
    [ComponentProperty]
    private float _currentHealth = 100f;

    [ComponentProperty]
    private float _maxHealth = 100f;
}

// Example Target Component
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    private float _percent;

    [ComponentProperty]
    private string _label = "";
}

// Helper to simulate generated PropertyBindings
public class ManualBindings : PropertyBindings
{
    private readonly List<(string, PropertyBinding)> _bindings = new();
    
    public void Add(string name, PropertyBinding binding)
    {
        _bindings.Add((name, binding));
    }

    public override IEnumerator<(string propertyName, PropertyBinding binding)> GetEnumerator()
    {
        return _bindings.GetEnumerator();
    }
}

// Example Container demonstrating bindings
public partial class PropertyBindingExample : RuntimeComponent
{
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);

        if (ContentManager == null) return;

        // 1. Setup Child Components
        // Note: In a real application, you would use generated templates like:
        // var healthComp = ContentManager.CreateInstance(new HealthComponentTemplate { Name = "PlayerHealth" });
        
        var healthComp = ContentManager.Create<HealthComponent>();
        if (healthComp is HealthComponent hc)
        {
            hc.SetName("PlayerHealth");
            AddChild(hc);
        }

        // 2. Define Bindings for HealthBar
        var healthBarBindings = new ManualBindings();
        
        // Example A: Sibling Binding (One-Way)
        healthBarBindings.Add(
            "Percent", // Note: In generated code this would be strongly typed
            Binding.FromSibling<HealthComponent>(s => s.CurrentHealth)
        );

        // Example B: Named Object Lookup
        healthBarBindings.Add(
            "Label",
            Binding.FromNamedObject<HealthComponent>("PlayerHealth", s => s.CurrentHealth)
                   .WithConverter(new HealthStringConverter())
        );

        var healthBar = ContentManager.Create<HealthBar>(new Template 
        { 
            Bindings = healthBarBindings 
        });
        
        if (healthBar is HealthBar hb)
        {
            hb.SetName("HUD_HealthBar");
            AddChild(hb);
        }
        
        // Example C: Two-Way Binding
        var sliderBindings = new ManualBindings();
        sliderBindings.Add(
            "Percent",
            Binding.TwoWay<HealthComponent>(s => s.CurrentHealth)
        );
        
        var debugSlider = ContentManager.Create<HealthBar>(new Template
        {
            Bindings = sliderBindings
        });
        
        if (debugSlider is HealthBar ds)
        {
            ds.SetName("DebugSlider");
            AddChild(ds);
        }
    }
}

public class HealthStringConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        return $"HP: {value}";
    }
}
