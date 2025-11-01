using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GameEngine.SourceGenerators;

/// <summary>
/// Incremental source generator that creates Template records and OnLoad methods
/// from ComponentProperty attributes on component classes.
/// 
/// For each component class with [ComponentProperty] fields, generates:
/// 1. {ComponentName}Template record with properties matching the fields
/// 2. OnLoad method override that assigns template properties to fields
/// 3. Optional partial OnLoad hook for custom initialization logic
/// </summary>
[Generator]
public class TemplateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register syntax provider to find component classes with ComponentProperty attributes
        var componentClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsComponentClass(node),
                transform: static (ctx, _) => GetComponentClassInfo(ctx))
            .Where(static info => info != null);

        // Generate template records and OnLoad methods
        context.RegisterSourceOutput(componentClasses, static (spc, componentInfo) =>
        {
            if (componentInfo == null) return;
            
            GenerateTemplate(spc, componentInfo);
            GenerateOnLoadMethod(spc, componentInfo);
        });
    }

    /// <summary>
    /// Checks if the syntax node is a class declaration.
    /// </summary>
    private static bool IsComponentClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl && classDecl.Modifiers.Any(m => m.ValueText == "partial");
    }

    /// <summary>
    /// Extracts component class information including ComponentProperty fields.
    /// </summary>
    private static ComponentClassInfo? GetComponentClassInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null) return null;

        // Check if class derives from IComponent (directly or indirectly)
        if (!DerivesFromInterface(classSymbol, "IComponent")) return null;

        // Get ComponentProperty fields declared directly on this class (not inherited)
        var properties = GetComponentProperties(classSymbol);

        // Find the base class that also has generated templates (for inheritance)
        var baseTemplateType = GetBaseTemplateType(classSymbol);
        
        // Generate template for all IComponent classes to support inheritance,
        // even if they don't have their own ComponentProperty fields
        // (they may inherit properties from base classes)

        return new ComponentClassInfo
        {
            ClassName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Properties = properties,
            BaseTemplateType = baseTemplateType
        };
    }

    /// <summary>
    /// Gets ComponentProperty fields declared directly on this class (not inherited).
    /// </summary>
    private static List<PropertyInfo> GetComponentProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        // Only get fields declared directly on this class
        foreach (var field in classSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            // Skip fields not declared on this specific type
            if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, classSymbol)) 
                continue;

            var attr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ComponentPropertyAttribute");

            if (attr == null) continue;

            // Get the syntax node to access the initializer
            var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;

            properties.Add(new PropertyInfo
            {
                FieldName = field.Name,
                PropertyName = GetPropertyNameFromField(field.Name),
                Type = field.Type.ToDisplayString(),
                DefaultValue = GetDefaultValueExpression(field, syntax)
            });
        }

        return properties;
    }

    /// <summary>
    /// Converts field name (e.g., "_position") to property name (e.g., "Position").
    /// </summary>
    private static string GetPropertyNameFromField(string fieldName)
    {
        var name = fieldName.TrimStart('_');
        if (name.Length == 0) return fieldName;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Gets the default value expression for a field from its initializer.
    /// First checks if the field has an explicit initializer in source code.
    /// Falls back to type-based defaults if no initializer is found.
    /// </summary>
    private static string GetDefaultValueExpression(IFieldSymbol field, VariableDeclaratorSyntax? syntax)
    {
        // First, check if there's an explicit initializer in the source code
        if (syntax?.Initializer?.Value != null)
        {
            // Return the initializer expression exactly as written in source
            return syntax.Initializer.Value.ToString();
        }

        // Fall back to type-based defaults
        var typeName = field.Type.ToDisplayString();
        
        // Handle arrays - use [] syntax for non-nullable arrays
        if (field.Type is IArrayTypeSymbol)
            return "[]";
        
        // Check for generic collection types first (before checking their type arguments)
        if (field.Type is INamedTypeSymbol fieldNamedType)
        {
            var typeNameWithoutNamespace = fieldNamedType.Name;
            
            // Handle Dictionary, List, HashSet, etc.
            if (typeNameWithoutNamespace == "Dictionary" || 
                typeNameWithoutNamespace == "List" || 
                typeNameWithoutNamespace == "HashSet" ||
                typeNameWithoutNamespace == "Queue" ||
                typeNameWithoutNamespace == "Stack")
            {
                return "new()";
            }
        }
        
        // Handle common default values for primitives and math types
        if (typeName == "Silk.NET.Maths.Vector3D<float>")
            return "Vector3D<float>.Zero";
        if (typeName == "Silk.NET.Maths.Quaternion<float>")
            return "Quaternion<float>.Identity";
        if (typeName == "Silk.NET.Maths.Vector4D<float>")
            return "Vector4D<float>.Zero";
        if (typeName == "Silk.NET.Maths.Vector2D<float>")
            return "Vector2D<float>.Zero";
        if (typeName == "float")
            return "0f";
        if (typeName == "int")
            return "0";
        if (typeName == "bool")
            return "false";
        if (typeName == "string")
            return "string.Empty";
        
        // For reference types (classes, records, interfaces)
        if (field.Type.IsReferenceType && !field.Type.IsValueType)
        {
            // Check if type has a parameterless constructor (including records)
            // Records always have synthesized parameterless constructors even with required properties
            if (field.Type is INamedTypeSymbol namedType)
            {
                // For records or types with parameterless constructors, use new()
                if (namedType.IsRecord || 
                    namedType.Constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
                {
                    return "new()";
                }
            }
            
            // Otherwise use null! for reference types without suitable constructors
            return "null!";
        }
        
        // For value types (structs), use default
        return $"default({typeName})";
    }

    /// <summary>
    /// Checks if a type derives from a specific interface.
    /// </summary>
    private static bool DerivesFromInterface(INamedTypeSymbol type, string interfaceName)
    {
        return type.AllInterfaces.Any(i => i.Name == interfaceName);
    }

    /// <summary>
    /// Gets the base template type by finding the immediate base class that has ComponentProperty fields.
    /// Returns the base class's template name, or "Nexus.GameEngine.Components.Template" if no such base exists.
    /// </summary>
    private static string GetBaseTemplateType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        
        // Walk up the inheritance chain to find the first base class with ComponentProperty fields
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            // Check if this base class has any ComponentProperty fields
            var hasComponentProperties = baseType.GetMembers()
                .OfType<IFieldSymbol>()
                .Any(f => SymbolEqualityComparer.Default.Equals(f.ContainingType, baseType) &&
                         f.GetAttributes().Any(a => a.AttributeClass?.Name == "ComponentPropertyAttribute"));
            
            if (hasComponentProperties)
            {
                // Found a base class with ComponentProperty fields - it will have a generated template
                return $"{baseType.ContainingNamespace.ToDisplayString()}.{baseType.Name}Template";
            }
            
            baseType = baseType.BaseType;
        }
        
        // No base class with ComponentProperty fields found - inherit from base Template
        return "Nexus.GameEngine.Components.Template";
    }

    /// <summary>
    /// Generates the template record for a component class.
    /// </summary>
    private static void GenerateTemplate(SourceProductionContext context, ComponentClassInfo info)
    {
        var sb = new StringBuilder();

        // Add file header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        
        // Add necessary using statements for common types
        sb.AppendLine("using Silk.NET.Maths;");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        
        // Generate template record as a separate top-level class
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Auto-generated template for {info.ClassName} component.");
        sb.AppendLine($"/// Properties correspond to [ComponentProperty] fields declared on this class.");
        sb.AppendLine($"/// Inherits from base class template to preserve property inheritance hierarchy.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public record {info.ClassName}Template : {info.BaseTemplateType}");
        sb.AppendLine("{");
        
        // Add ComponentType property override
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Gets the component type for factory instantiation.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public override Type? ComponentType => typeof({info.ClassName});");
        sb.AppendLine();

        // Add property for each ComponentProperty field
        foreach (var prop in info.Properties)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Template property for {prop.PropertyName}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public {prop.Type} {prop.PropertyName} {{ get; set; }} = {prop.DefaultValue};");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        context.AddSource($"{info.ClassName}.Template.g.cs", sb.ToString());
    }

    /// <summary>
    /// Generates the OnLoad method override for a component class.
    /// </summary>
    private static void GenerateOnLoadMethod(SourceProductionContext context, ComponentClassInfo info)
    {
        var sb = new StringBuilder();

        // Add file header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {info.ClassName}");
        sb.AppendLine("{");
        
        // Add partial method declaration for custom OnLoad hook
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Optional partial method for custom template loading logic.");
        sb.AppendLine($"    /// Implement this method in your component class to perform custom initialization.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    partial void OnLoad({info.ClassName}Template template);");
        sb.AppendLine();

        // Generate OnLoad method override that casts to specific template type
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Loads component from template. Auto-generated override.");
        sb.AppendLine($"    /// Assigns values directly to backing fields, bypassing the deferred update system.");
        sb.AppendLine($"    /// Target properties will automatically return backing field values until explicitly set.");
        sb.AppendLine($"    /// This ensures immediate configuration without going through animations.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    protected override void OnLoad(Nexus.GameEngine.Components.Template? componentTemplate)");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (componentTemplate is {info.ClassName}Template template)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Call base.OnLoad first to handle any base class properties");
        sb.AppendLine("            base.OnLoad(componentTemplate);");
        sb.AppendLine();
        sb.AppendLine("            // Assign directly to backing fields (target properties auto-return these until explicitly set)");
        
        foreach (var prop in info.Properties)
        {
            sb.AppendLine($"            {prop.FieldName} = template.{prop.PropertyName};");
        }

        sb.AppendLine();
        sb.AppendLine("            // Call partial method hook if implemented");
        sb.AppendLine("            OnLoad(template);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Fall back to base implementation for other template types");
        sb.AppendLine("            base.OnLoad(componentTemplate);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        context.AddSource($"{info.ClassName}.OnLoad.g.cs", sb.ToString());
    }

    /// <summary>
    /// Information about a component class with ComponentProperty fields.
    /// </summary>
    private class ComponentClassInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<PropertyInfo> Properties { get; set; } = new();
        public string BaseTemplateType { get; set; } = "Template";
    }

    /// <summary>
    /// Information about a ComponentProperty field.
    /// </summary>
    private class PropertyInfo
    {
        public string FieldName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
    }
}
