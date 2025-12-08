using Xunit;
using Nexus.GameEngine.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.GameEngine.Components;

namespace Tests.SourceGenerators;

public class TemplateGeneratorTests
{
    private string RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.Component).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.TemplatePropertyAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Nexus.GameEngine.Components.IConfigurable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TemplateGenerator();
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
