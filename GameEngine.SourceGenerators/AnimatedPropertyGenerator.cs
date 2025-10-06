using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nexus.GameEngine.SourceGenerators;

/// <summary>
/// Incremental source generator that creates animated property implementations
/// for auto-properties in classes implementing IRuntimeComponent.
/// </summary>
[Generator]
public class AnimatedPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter to classes that might need generation
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsClassDeclaration(node),
                transform: static (ctx, _) => GetClassDeclaration(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate source for each class
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) =>
            Execute(source.Left, source.Right!, spc));
    }

    private static bool IsClassDeclaration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { } classDecl
            && classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static ClassDeclarationSyntax? GetClassDeclaration(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Check if class implements IRuntimeComponent
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is null) return null;

        var implementsInterface = classSymbol.AllInterfaces
            .Any(i => i.Name == "IRuntimeComponent");

        return implementsInterface ? classDecl : null;
    }

    private static void Execute(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        foreach (var classDecl in classes)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is null) continue;

            // Get properties to generate
            var properties = GetPropertiesToGenerate(classSymbol);
            if (!properties.Any()) continue;

            // Generate source
            var source = GenerateSource(classSymbol, properties);
            var fileName = $"{classSymbol.Name}.g.cs";

            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static List<PropertyInfo> GetPropertiesToGenerate(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        // Look for fields with [AnimatedProperty] attribute
        foreach (var field in classSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            // Check if field has ComponentProperty attribute
            var animationAttr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ComponentPropertyAttribute");

            if (animationAttr == null) continue;

            // Check if field is declared on this class (not inherited)
            if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, classSymbol)) continue;

            // Generate property name from field name (remove _ prefix and capitalize)
            var propertyName = GetPropertyNameFromField(field.Name);

            properties.Add(new PropertyInfo
            {
                Name = propertyName,
                FieldName = field.Name,
                Type = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                HasAnimationAttribute = true,
                Duration = GetAttributeValue<float>(animationAttr, "Duration", 0f),
                Interpolation = GetAttributeValue<int>(animationAttr, "Interpolation", 0)
            });
        }

        return properties;
    }

    private static string GetPropertyNameFromField(string fieldName)
    {
        // Remove leading underscore if present
        if (fieldName.StartsWith("_"))
            fieldName = fieldName.Substring(1);

        // Capitalize first letter
        if (fieldName.Length > 0)
            return char.ToUpper(fieldName[0]) + fieldName.Substring(1);

        return fieldName;
    }

    private static T GetAttributeValue<T>(AttributeData? attribute, string propertyName, T defaultValue)
    {
        if (attribute == null) return defaultValue;

        var namedArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == propertyName);
        if (namedArg.Value.Value is T value)
            return value;

        return defaultValue;
    }

    private static string GenerateSource(INamedTypeSymbol classSymbol, List<PropertyInfo> properties)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"partial class {className}");
        sb.AppendLine("{");

        // Generate backing fields and property implementations
        foreach (var prop in properties)
        {
            GenerateProperty(sb, prop);
        }

        // Generate UpdateAnimations method
        GenerateUpdateAnimationsMethod(sb, properties);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateProperty(StringBuilder sb, PropertyInfo prop)
    {
        var fieldName = prop.FieldName; // Use the existing field
        var targetFieldName = $"{fieldName}Target";

        sb.AppendLine($"    // Generated property: {prop.Name} (from field {fieldName})");

        // Create target field to track pending changes - initialize with default! to satisfy nullable analysis
        sb.AppendLine($"    private {prop.Type} {targetFieldName} = default!;");

        if (prop.HasAnimationAttribute && prop.Duration > 0)
        {
            var animFieldName = $"{fieldName}Animation";
            sb.AppendLine($"    private global::Nexus.GameEngine.Components.PropertyAnimation<{prop.Type}> {animFieldName} = new()");
            sb.AppendLine("    {");
            sb.AppendLine($"        Duration = {prop.Duration}f,");
            sb.AppendLine($"        Interpolation = (global::Nexus.GameEngine.Animation.InterpolationMode){prop.Interpolation}");
            sb.AppendLine("    };");
        }

        sb.AppendLine();
        sb.AppendLine($"    public {prop.Type} {prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {fieldName};");
        sb.AppendLine("        set");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default.Equals({targetFieldName}, value))");
        sb.AppendLine("                return;");
        sb.AppendLine();
        sb.AppendLine($"            {targetFieldName} = value;");

        if (prop.HasAnimationAttribute && prop.Duration > 0)
        {
            var animFieldName = $"{fieldName}Animation";
            sb.AppendLine($"            {animFieldName}.StartAnimation({fieldName}, {targetFieldName}, 0.0); // TODO: Use TimeProvider");
            sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateUpdateAnimationsMethod(StringBuilder sb, List<PropertyInfo> properties)
    {
        // Generate partial method declarations for property change callbacks
        foreach (var prop in properties)
        {
            sb.AppendLine($"    // Optional callback for {prop.Name} changes");
            sb.AppendLine($"    partial void On{prop.Name}Changed({prop.Type} oldValue);");
            sb.AppendLine();
        }

        // Generate the declaration and implementation of UpdateAnimations for this partial class
        sb.AppendLine("    // Declaration of the partial method (required in the generated file)");
        sb.AppendLine("    partial void UpdateAnimations(double deltaTime);");
        sb.AppendLine();
        sb.AppendLine("    // Generated animation update implementation");
        sb.AppendLine("    partial void UpdateAnimations(double deltaTime)");
        sb.AppendLine("    {");

        foreach (var prop in properties)
        {
            var fieldName = prop.FieldName;
            var targetFieldName = $"{fieldName}Target";

            if (prop.HasAnimationAttribute && prop.Duration > 0)
            {
                // Animated property
                var animFieldName = $"{fieldName}Animation";
                sb.AppendLine($"        if ({animFieldName}.IsAnimating)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var oldValue = {fieldName};");
                sb.AppendLine($"            {fieldName} = {animFieldName}.Update(deltaTime);");
                sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                sb.AppendLine($"            On{prop.Name}Changed(oldValue);");
                sb.AppendLine();
                sb.AppendLine($"            if (!{animFieldName}.IsAnimating)");
                sb.AppendLine("            {");
                sb.AppendLine($"                OnPropertyAnimationEnded(nameof({prop.Name}));");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                // Instant update property
                sb.AppendLine($"        if (!global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default.Equals({targetFieldName}, {fieldName}))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var oldValue = {fieldName};");
                sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                sb.AppendLine($"            {fieldName} = {targetFieldName};");
                sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                sb.AppendLine($"            On{prop.Name}Changed(oldValue);");
                sb.AppendLine($"            OnPropertyAnimationEnded(nameof({prop.Name}));");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
        }

        sb.AppendLine("    }");
    }

    private class PropertyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool HasAnimationAttribute { get; set; }
        public float Duration { get; set; }
        public int Interpolation { get; set; }
    }
}
