using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGenerators;

/// <summary>
/// Incremental source generator that creates Template records and OnLoad methods
/// from ComponentProperty and TemplateProperty attributes on component classes.
/// 
/// For each component class with [ComponentProperty] or [TemplateProperty] fields, generates:
/// 1. {ComponentName}Template record with properties matching the fields
/// 2. OnLoad method override that assigns template properties to fields
/// 3. Optional partial OnLoad hook for custom initialization logic
/// 
/// ComponentProperty: Full public property with deferred updates and animation support
/// TemplateProperty: Template-only property, assigned once in OnLoad (no public API)
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
            GenerateLoadOverload(spc, componentInfo);
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
        // We generate template records for abstract classes as well so the
        // inheritance chain of template properties is preserved. Abstract
        // templates will be emitted with internal visibility and a null
        // ComponentType so they can't be used to instantiate components.

        // Get ComponentProperty fields declared directly on this class (not inherited)
        var properties = GetComponentProperties(classSymbol);
        
        // Get all template properties from the entire hierarchy (for Load overload)
        var allTemplateProperties = GetAllTemplateProperties(classSymbol);

            // Get the base class template target (for inheritance)
            var baseTemplateType = GetBaseTemplateType(classSymbol, out var baseTemplateSkipTypeName);
        
        // Generate template for all IComponent classes to support inheritance,
        // even if they don't have their own ComponentProperty fields
        // (they may inherit properties from base classes)

        // Determine the default concrete component type to use in the generated Template.ComponentType
        // If the class itself is non-abstract use it, otherwise walk up the base types to find the
        // nearest non-abstract concrete type (e.g., DrawableElement -> Element).
        string? componentTypeExpression = null;
        var chosenType = classSymbol;
        if (chosenType.IsAbstract)
        {
            var search = chosenType.BaseType;
            while (search != null && search.SpecialType != SpecialType.System_Object)
            {
                if (!search.IsAbstract)
                {
                    chosenType = search;
                    break;
                }
                search = search.BaseType;
            }
        }

        if (!chosenType.IsAbstract)
        {
            // Use global-qualified type name to avoid namespace resolution issues in generated code
            componentTypeExpression = $"global::{chosenType.ToDisplayString()}";
        }

        return new ComponentClassInfo
        {
            ClassName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Properties = properties,
            AllTemplateProperties = allTemplateProperties,
            BaseTemplateType = baseTemplateType,
                BaseTemplateSkipTypeName = baseTemplateSkipTypeName,
            ComponentTypeExpression = componentTypeExpression,
            IsAbstract = classSymbol.IsAbstract
        };
    }

    /// <summary>
    /// Gets ComponentProperty and TemplateProperty fields and methods declared directly on this class (not inherited).
    /// </summary>
    private static List<PropertyInfo> GetComponentProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        // Get fields declared directly on this class
        foreach (var field in classSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            // Skip fields not declared on this specific type
            if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, classSymbol)) 
                continue;

            // Only process fields with TemplateProperty attribute
            var templateAttr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "TemplatePropertyAttribute");

            if (templateAttr == null) continue;

            // Get the syntax node to access the initializer
            var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;

            // Get property name from TemplateProperty.Name if specified, otherwise derive from field name
            string propertyName = GetPropertyNameFromField(field.Name);
            if (templateAttr != null)
            {
                var nameArg = templateAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Name");
                if (nameArg.Value.Value is string customName && !string.IsNullOrEmpty(customName))
                {
                    propertyName = customName;
                }
            }

            properties.Add(new PropertyInfo
            {
                FieldName = field.Name,
                PropertyName = propertyName,
                Type = field.Type.ToDisplayString(),
                DefaultValue = GetDefaultValueExpression(field, syntax),
                IsMethod = false
            });
        }

        // Get methods (partial or regular) declared directly on this class
        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            // Skip methods not declared on this specific type
            if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, classSymbol))
                continue;

            var templateAttr = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "TemplatePropertyAttribute");

            if (templateAttr == null) continue;

            // Method must have exactly one parameter
            if (method.Parameters.Length != 1)
                continue;

            var parameter = method.Parameters[0];
            var propertyType = parameter.Type.ToDisplayString();

            // Get property name from TemplateProperty.Name attribute, or derive from method name
            string propertyName = method.Name.StartsWith("Set") ? method.Name.Substring(3) : method.Name;
            var nameArg = templateAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Name");
            if (nameArg.Value.Value is string customName && !string.IsNullOrEmpty(customName))
            {
                propertyName = customName;
            }

            // For methods, we use nullable types with default null in templates
            string nullableType = propertyType.EndsWith("?") ? propertyType : propertyType + "?";

            properties.Add(new PropertyInfo
            {
                FieldName = string.Empty, // Not used for methods
                PropertyName = propertyName,
                Type = nullableType,
                DefaultValue = "null",
                IsMethod = true,
                MethodName = method.Name
            });
        }

        return properties;
    }

    /// <summary>
    /// Gets all ComponentProperty and TemplateProperty fields and methods from the entire inheritance hierarchy.
    /// Used for generating the Load overload with all available template properties.
    /// </summary>
    private static List<PropertyInfo> GetAllTemplateProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();
        var seenProperties = new HashSet<string>(); // Track property names to avoid duplicates

        var currentType = classSymbol;
        
        // Walk up the inheritance chain
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Get all fields with TemplateProperty attribute from this class
            foreach (var field in currentType.GetMembers().OfType<IFieldSymbol>())
            {
                // Only process fields declared on this specific type
                if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, currentType))
                    continue;

                var templateAttr = field.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "TemplatePropertyAttribute");

                if (templateAttr == null) continue;

                // Get property name from TemplateProperty.Name if specified, otherwise derive from field name
                string propertyName = GetPropertyNameFromField(field.Name);
                if (templateAttr != null)
                {
                    var nameArg = templateAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Name");
                    if (nameArg.Value.Value is string customName && !string.IsNullOrEmpty(customName))
                    {
                        propertyName = customName;
                    }
                }
                
                // Skip if we've already seen this property name (child classes override parent properties)
                if (seenProperties.Contains(propertyName)) continue;
                seenProperties.Add(propertyName);

                var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;

                properties.Add(new PropertyInfo
                {
                    FieldName = field.Name,
                    PropertyName = propertyName,
                    Type = field.Type.ToDisplayString(),
                    DefaultValue = GetDefaultValueExpression(field, syntax),
                    DeclaringTypeName = field.ContainingType.ToDisplayString(),
                    IsMethod = false
                });
            }

            // Get all methods (partial or regular) with TemplateProperty attribute from this class
            foreach (var method in currentType.GetMembers().OfType<IMethodSymbol>())
            {
                // Only process methods declared on this specific type
                if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, currentType))
                    continue;

                var templateAttr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "TemplatePropertyAttribute");

                if (templateAttr == null) continue;

                // Method must have exactly one parameter
                if (method.Parameters.Length != 1)
                    continue;

                var parameter = method.Parameters[0];
                var propertyType = parameter.Type.ToDisplayString();

                // Get property name from TemplateProperty.Name attribute, or derive from method name
                string propertyName = method.Name.StartsWith("Set") ? method.Name.Substring(3) : method.Name;
                var nameArg = templateAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Name");
                if (nameArg.Value.Value is string customName && !string.IsNullOrEmpty(customName))
                {
                    propertyName = customName;
                }

                // Skip if we've already seen this property name
                if (seenProperties.Contains(propertyName)) continue;
                seenProperties.Add(propertyName);

                // For methods, we use nullable types with default null in templates
                string nullableType = propertyType.EndsWith("?") ? propertyType : propertyType + "?";

                properties.Add(new PropertyInfo
                {
                    FieldName = string.Empty,
                    PropertyName = propertyName,
                    Type = nullableType,
                    DefaultValue = "null",
                    DeclaringTypeName = method.ContainingType.ToDisplayString(),
                    IsMethod = true,
                    MethodName = method.Name
                });
            }

            currentType = currentType.BaseType;
        }

        // Reverse to put base class properties first
        properties.Reverse();
        
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
        if (baseType != null && baseType.SpecialType != SpecialType.System_Object && DerivesFromInterface(baseType, "IComponent"))
        {
            return $"{baseType.ContainingNamespace.ToDisplayString()}.{baseType.Name}Template";
        }

        // Fallback to root Template
        return "Nexus.GameEngine.Components.Template";
    }

    /// <summary>
    /// Gets the base template type string and optionally the declaring type name to skip
    /// when emitting properties for derived templates.
    /// </summary>
    private static string GetBaseTemplateType(INamedTypeSymbol classSymbol, out string? baseTemplateSkipTypeName)
    {
        var baseType = classSymbol.BaseType;
        baseTemplateSkipTypeName = null;
        baseTemplateSkipTypeName = null;
        if (baseType != null && baseType.SpecialType != SpecialType.System_Object && DerivesFromInterface(baseType, "IComponent"))
        {
            baseTemplateSkipTypeName = baseType.ToDisplayString();
            return $"{baseType.ContainingNamespace.ToDisplayString()}.{baseType.Name}Template";
        }

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
        sb.AppendLine($"/// Properties correspond to [ComponentProperty] and [TemplateProperty] fields declared on this class.");
        sb.AppendLine($"/// Inherits from base class template to preserve property inheritance hierarchy.");
        sb.AppendLine($"/// </summary>");
    // Templates are public to preserve accessibility across inheritance chains.
    sb.AppendLine($"public record {info.ClassName}Template : {info.BaseTemplateType}");
        sb.AppendLine("{");
        
        // Add ComponentType property override
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Gets the component type for factory instantiation.");
    sb.AppendLine($"    /// If the component is abstract, this will return null to avoid attempting to instantiate an abstract type at runtime.");
        sb.AppendLine($"    /// </summary>");
        if (info.IsAbstract)
        {
            sb.AppendLine($"    public override Type? ComponentType => null;");
        }
        else if (!string.IsNullOrEmpty(info.ComponentTypeExpression))
        {
            sb.AppendLine($"    public override Type? ComponentType => typeof({info.ComponentTypeExpression});");
        }
        else
        {
            sb.AppendLine($"    public override Type? ComponentType => null;");
        }
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
        sb.AppendLine($"    /// Called after all template properties have been applied.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    partial void OnLoad({info.ClassName}Template template);");
        sb.AppendLine();

        // Only generate Load delegation if this class has template properties
        // Otherwise just implement direct assignment logic
        if (info.AllTemplateProperties.Count > 0)
        {
            // Generate OnLoad method that delegates to Load(...)
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Loads component from template. Auto-generated override.");
            sb.AppendLine($"    /// Delegates to Load(...) method with individual template properties.");
            sb.AppendLine($"    /// This ensures consistent behavior whether using templates or direct Load() calls.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    protected override void OnLoad(Nexus.GameEngine.Components.Template? componentTemplate)");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (componentTemplate is {info.ClassName}Template template)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Call Load(...) with all template properties");
            sb.Append("            Load(");
            
            // Build parameter list from all template properties
            var parameters = new List<string>();
            foreach (var prop in info.AllTemplateProperties)
            {
                parameters.Add($"{ToCamelCase(prop.PropertyName)}: template.{prop.PropertyName}");
            }
            
            sb.Append(string.Join(", ", parameters));
            sb.AppendLine(");");
            
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            // Fall back to base implementation for other template types");
            sb.AppendLine("            base.OnLoad(componentTemplate);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        else
        {
            // No template properties - just call base and OnLoad hook
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Loads component from template. Auto-generated override.");
            sb.AppendLine($"    /// Calls base implementation and OnLoad hook.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    protected override void OnLoad(Nexus.GameEngine.Components.Template? componentTemplate)");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (componentTemplate is {info.ClassName}Template template)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnLoad(componentTemplate);");
            sb.AppendLine("            OnLoad(template);");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnLoad(componentTemplate);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        context.AddSource($"{info.ClassName}.OnLoad.g.cs", sb.ToString());
    }

    /// <summary>
    /// Generates a convenient Load overload with optional parameters for all template properties.
    /// This method handles both template creation AND application of properties to backing fields.
    /// Includes properties from the entire inheritance hierarchy.
    /// </summary>
    private static void GenerateLoadOverload(SourceProductionContext context, ComponentClassInfo info)
    {
        // Only generate a convenience Load overload when THIS class declares at least
        // one TemplateProperty. If the class doesn't introduce any new template
        // properties (it only inherits them), generating an overload will duplicate
        // a base-generated overload and cause CS0108 hiding warnings in derived
        // types (common in TestApp). Using info.Properties (declared on the class)
        // avoids that duplication.
        if (info.Properties == null || info.Properties.Count == 0)
            return;

        var sb = new StringBuilder();

        // Add file header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {info.ClassName}");
        sb.AppendLine("{");
        
        // Generate Load overload with optional parameters
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Convenience method to load component with individual property values.");
        sb.AppendLine($"    /// Applies template properties directly to backing fields and calls OnLoad hook.");
        sb.AppendLine($"    /// Useful for testing and simple initialization scenarios.");
        sb.AppendLine($"    /// Includes all properties from the inheritance hierarchy.");
        sb.AppendLine($"    /// </summary>");
        
        // Build parameter list using ALL template properties (including inherited)
        sb.Append($"    public void Load(");
        
        var parameters = new List<string>();
        foreach (var prop in info.AllTemplateProperties)
        {
            // For nullable types, don't add another '?'
            var paramType = prop.Type;
            if (!paramType.EndsWith("?"))
            {
                paramType = paramType + "?";
            }
            parameters.Add($"{paramType} {ToCamelCase(prop.PropertyName)} = null");
        }
        
        sb.Append(string.Join(", ", parameters));
        sb.AppendLine(")");
        
        sb.AppendLine("    {");
        
        // Create template for OnLoad hook
        sb.AppendLine($"        var template = new {info.ClassName}Template");
        sb.AppendLine("        {");
        
        // Assign all properties to template (for OnLoad hook)
        var propList = info.AllTemplateProperties.ToList();
        for (int i = 0; i < propList.Count; i++)
        {
            var prop = propList[i];
            var paramName = ToCamelCase(prop.PropertyName);
            var comma = i < propList.Count - 1 ? "," : "";
            sb.AppendLine($"            {prop.PropertyName} = {paramName} ?? {prop.DefaultValue}{comma}");
        }
        
        sb.AppendLine("        };");
        sb.AppendLine();
        
        // Call base.Load if this isn't the root to handle base class properties
        if (info.BaseTemplateType != "Nexus.GameEngine.Components.Template")
        {
            var baseParameters = new List<string>();
            foreach (var prop in info.AllTemplateProperties)
            {
                // Skip properties declared on this class
                if (info.Properties.Any(p => p.PropertyName == prop.PropertyName))
                    continue;
                    
                baseParameters.Add($"{ToCamelCase(prop.PropertyName)}: {ToCamelCase(prop.PropertyName)}");
            }
            
            if (baseParameters.Count > 0)
            {
                sb.AppendLine("        // Call base Load to handle inherited properties");
                sb.Append("        base.Load(");
                sb.Append(string.Join(", ", baseParameters));
                sb.AppendLine(");");
                sb.AppendLine();
            }
        }
        
        // Apply properties declared on this class
        sb.AppendLine("        // Apply properties declared on this class");
        foreach (var prop in info.Properties)
        {
            var paramName = ToCamelCase(prop.PropertyName);
            
            if (prop.IsMethod)
            {
                // For methods, only call if the value was provided (HasValue for nullable types)
                sb.AppendLine($"        if ({paramName}.HasValue)");
                sb.AppendLine($"            {prop.MethodName}({paramName}.Value);");
            }
            else
            {
                // For fields, assign if provided, otherwise use default
                sb.AppendLine($"        {prop.FieldName} = {paramName} ?? {prop.DefaultValue};");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("        // Call OnLoad hook for custom initialization");
        sb.AppendLine("        OnLoad(template);");
        
        sb.AppendLine("    }");

        sb.AppendLine("}");

        context.AddSource($"{info.ClassName}.LoadOverload.g.cs", sb.ToString());
    }

    /// <summary>
    /// Converts PascalCase to camelCase.
    /// </summary>
    private static string ToCamelCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase) || pascalCase.Length == 0)
            return pascalCase;
        
        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Information about a component class with ComponentProperty fields.
    /// </summary>
    private class ComponentClassInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<PropertyInfo> Properties { get; set; } = new();
        public List<PropertyInfo> AllTemplateProperties { get; set; } = new();
        public string BaseTemplateType { get; set; } = "Template";
        /// <summary>
        /// Optional C# expression to use for the generated Template.ComponentType value.
        /// If null, the generated property will return null (used for abstract/base types).
        /// Example: "global::Nexus.GameEngine.GUI.Element"
        /// </summary>
        public string? ComponentTypeExpression { get; set; } = null;
    /// <summary>
    /// Whether the component type is abstract. Used to emit internal templates
    /// with null ComponentType so abstract templates cannot be instantiated.
    /// </summary>
    public bool IsAbstract { get; set; } = false;
        /// <summary>
        /// Full name of the base type whose template will be used as the base for this template.
        /// Properties declared on this type should be excluded from the generated template to
        /// avoid duplication.
        /// </summary>
        public string? BaseTemplateSkipTypeName { get; set; } = null;
    }

    /// <summary>
    /// Information about a ComponentProperty or TemplateProperty field or method.
    /// </summary>
    private class PropertyInfo
    {
        public string FieldName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        // Full name of the type that declares this field (used to exclude properties
        // that are provided by the chosen base template when generating derived templates)
        public string DeclaringTypeName { get; set; } = string.Empty;
        // True if this is a partial method with [TemplateProperty], false if it's a field
        public bool IsMethod { get; set; } = false;
        // Method name for partial methods (used in OnLoad generation)
        public string MethodName { get; set; } = string.Empty;
    }
}
