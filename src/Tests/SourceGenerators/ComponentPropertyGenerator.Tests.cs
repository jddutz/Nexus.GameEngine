using Xunit;
using Nexus.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.GameEngine.Components;

namespace Tests.SourceGenerators;

public class ComponentPropertyGeneratorTests
{
    [Fact]
    public void ComponentPropertyAttribute_ShouldHaveNotifyChangeProperty()
    {
        var type = typeof(ComponentPropertyAttribute);
        var prop = type.GetProperty("NotifyChange");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ShouldGeneratePropertyChangedEvent_WhenNotifyChangeIsTrue()
    {
        // Arrange
        var source = @"
using Nexus.GameEngine.Components;

namespace TestNamespace
{
    public partial class TestComponent : RuntimeComponent
    {
        [ComponentProperty(NotifyChange = true)]
        private float _health;
    }
}";
        
        // Act
        var output = RunGenerator(source);

        // Assert
        Assert.Contains("public event global::System.EventHandler<global::Nexus.GameEngine.Events.PropertyChangedEventArgs<float>>? HealthChanged;", output);
        Assert.Contains("partial void OnHealthChanged(float oldValue);", output);
        Assert.Contains("OnHealthChanged(oldValue);", output);
        Assert.Contains("HealthChanged?.Invoke(this, new global::Nexus.GameEngine.Events.PropertyChangedEventArgs<float>(oldValue, value));", output);
    }

    private string RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.RuntimeComponent).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.ComponentPropertyAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ComponentPropertyGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        
        if (runResult.Results.Length == 0 || runResult.Results[0].GeneratedSources.Length == 0)
        {
            return string.Empty;
        }

        var generatedSource = runResult.Results[0].GeneratedSources[0].SourceText.ToString();

        return generatedSource;
    }
}
