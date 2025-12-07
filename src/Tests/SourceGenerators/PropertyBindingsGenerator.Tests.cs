using Xunit;
using Nexus.GameEngine.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.GameEngine.Components;

namespace Tests.SourceGenerators;

public class PropertyBindingsGeneratorTests
{
    [Fact]
    public void ShouldGeneratePropertyBindingsClass()
    {
        // Arrange
        var source = @"
using Nexus.GameEngine.Components;

namespace TestNamespace
{
    // Abstract to avoid implementing interface members
    public abstract partial class TestComponent : IConfigurable
    {
    }
}";
        
        // Act
        var output = RunGenerator(source);

        // Assert
        Assert.Contains("public partial class TestComponentPropertyBindings : Nexus.GameEngine.Components.PropertyBindings", output);
    }

    [Fact]
    public void ShouldInheritFromBaseBindings_WhenBaseIsConfigurable()
    {
        // Arrange
        var source = @"
using Nexus.GameEngine.Components;

namespace TestNamespace
{
    public partial class BaseComponent : Component {}

    public partial class DerivedComponent : BaseComponent {}
}";
        
        // Act
        var output = RunGenerator(source);

        // Assert
        // We expect DerivedComponentPropertyBindings to inherit from BaseComponentPropertyBindings
        // Note: The generator might output fully qualified names.
        Assert.Contains("public partial class DerivedComponentPropertyBindings : TestNamespace.BaseComponentPropertyBindings", output);
    }

    [Fact]
    public void ShouldGenerateBindingProperties_ForComponentProperties()
    {
        // Arrange
        var source = @"
using Nexus.GameEngine.Components;

namespace TestNamespace
{
    public partial class TestComponent : Component
    {
        [ComponentProperty]
        private int _health;

        [ComponentProperty]
        private string _name;
    }
}";
        
        // Act
        var output = RunGenerator(source);

        // Assert
        Assert.Contains("public IPropertyBinding? Health { get; init; }", output);
        Assert.Contains("public IPropertyBinding? Name { get; init; }", output);
    }

    [Fact]
    public void ShouldGenerateGetEnumerator_YieldingConfiguredBindings()
    {
        // Arrange
        var source = @"
using Nexus.GameEngine.Components;

namespace TestNamespace
{
    public partial class TestComponent : Component
    {
        [ComponentProperty]
        private int _score;
    }
}";
        
        // Act
        var output = RunGenerator(source);

        // Assert
        Assert.Contains("if (Score != null) yield return (\"Score\", Score);", output);
    }

    private string RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.Component).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.IConfigurable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.PropertyBindings).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new PropertyBindingsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        
        if (runResult.Results.Length == 0 || runResult.Results[0].GeneratedSources.Length == 0)
        {
            return string.Empty;
        }

        // Concatenate all generated sources
        return string.Join("\n", runResult.Results[0].GeneratedSources.Select(s => s.SourceText.ToString()));
    }
}
