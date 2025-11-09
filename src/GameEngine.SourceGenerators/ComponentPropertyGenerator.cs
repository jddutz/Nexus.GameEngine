using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nexus.GameEngine.SourceGenerators;

/// <summary>
/// Incremental source generator that creates component property implementations
/// for fields decorated with [ComponentProperty] attribute in classes deriving from Entity.
/// </summary>
[Generator]
public class ComponentPropertyGenerator : IIncrementalGenerator
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

        // Check if class derives from Entity
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is null) return null;

        var derivesFromComponentBase = InheritsFromComponentBase(classSymbol);

        return derivesFromComponentBase ? classDecl : null;
    }

    private static bool InheritsFromComponentBase(INamedTypeSymbol classSymbol)
    {
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.Name == "Entity")
                return true;
            current = current.BaseType;
        }
        return false;
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

            // Get property name from ComponentProperty.Name if specified, otherwise derive from field name
            var propertyName = GetPropertyNameFromField(field.Name);
            var nameArg = animationAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Name");
            if (nameArg.Value.Value is string customName && !string.IsNullOrEmpty(customName))
            {
                propertyName = customName;
            }

            // Check if type is a collection (array or implements IEnumerable<T>)
            var isCollection = IsCollectionType(field.Type);

            // Create display format that includes nullable annotations
            var displayFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            // Read optional BeforeChange named argument from attribute
            var beforeChange = GetAttributeValue<string?>(animationAttr, "BeforeChange", null);

            properties.Add(new PropertyInfo
            {
                Name = propertyName,
                FieldName = field.Name,
                Type = field.Type.ToDisplayString(displayFormat),
                TypeSymbol = field.Type,
                IsCollection = isCollection,
                // Duration and Interpolation are now runtime parameters, not attribute properties
                DefaultValue = GetFieldDefaultValue(field),
                BeforeChange = beforeChange
            });
        }

        return properties;
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        // Check if it's an array
        if (type is IArrayTypeSymbol)
            return true;

        // Check if it implements IEnumerable<T> (but not string)
        if (type.SpecialType == SpecialType.System_String)
            return false;

        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");
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

    private static string GetFieldDefaultValue(IFieldSymbol field)
    {
        // Try to get the initializer value from syntax
        var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is VariableDeclaratorSyntax variableDeclarator && 
            variableDeclarator.Initializer != null)
        {
            return variableDeclarator.Initializer.Value.ToString();
        }
        
        // Return type-appropriate default
        if (field.Type.IsValueType)
            return $"default({field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
        
        return "default!";
    }

    private static T GetAttributeValue<T>(AttributeData? attribute, string propertyName, T defaultValue)
    {
        if (attribute == null) return defaultValue;

        var namedArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == propertyName);
        if (namedArg.Value.Value != null)
        {
            // Try direct cast first
            if (namedArg.Value.Value is T value)
                return value;
            
            // Try conversion for numeric types (handles constant evaluation)
            if (typeof(T) == typeof(float))
            {
                try
                {
                    // Handle various numeric types that constants might resolve to
                    var rawValue = namedArg.Value.Value;
                    if (rawValue is double d)
                        return (T)(object)(float)d;
                    if (rawValue is int i)
                        return (T)(object)(float)i;
                    if (rawValue is long l)
                        return (T)(object)(float)l;
                }
                catch
                {
                    // Fall through to default
                }
            }
        }

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

        // Generate cached property name strings to avoid allocations
        GeneratePropertyNameConstants(sb, properties);

        // Generate backing fields and property implementations
        foreach (var prop in properties)
        {
            GenerateProperty(sb, prop);
        }

        // Generate ApplyUpdates method
        GenerateApplyUpdatesMethod(sb, properties);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GeneratePropertyNameConstants(StringBuilder sb, List<PropertyInfo> properties)
    {
        if (properties.Count == 0) return;
        
        sb.AppendLine("    // Cached property name strings to avoid allocations");
        foreach (var prop in properties)
        {
            sb.AppendLine($"    private const string {prop.Name}PropertyName = \"{prop.Name}\";");
        }
        sb.AppendLine();
        
        // Add component-level dirty flag for performance
        if (properties.Count > 0)
        {
            sb.AppendLine("    // Component-level dirty flag to track if any properties need processing (performance optimization)");
            sb.AppendLine("    private bool _isDirty;");
            sb.AppendLine();
        }
    }

    private static void GenerateProperty(StringBuilder sb, PropertyInfo prop)
    {
        var fieldName = prop.FieldName; // Use the existing field
        var targetFieldName = $"{fieldName}Target";

        sb.AppendLine($"    // Generated property: {prop.Name} (from field {fieldName})");

        // Target field to store the pending value for next frame update
        sb.AppendLine($"    private bool {targetFieldName}__hasUpdate;");
        sb.AppendLine($"    private {prop.Type} {targetFieldName} = default!;");
        
        // Cache EqualityComparer to avoid property access overhead
        sb.AppendLine($"    private static readonly global::System.Collections.Generic.EqualityComparer<{prop.Type}> {fieldName}__comparer = global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default;");

        sb.AppendLine();
        
        // Generate read-only property that returns current value from backing field
        var propertyType = prop.Type;
        sb.AppendLine($"    public {propertyType} {prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {fieldName};");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Generate Target{PropertyName} property to access pending value
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Gets the target value for {prop.Name} that will be applied on the next ApplyUpdates call.");
        sb.AppendLine($"    /// If no update is pending, returns the current value.");
        sb.AppendLine($"    /// Use this in layout calculations to query the element's target size before deferred updates are applied.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public {propertyType} Target{prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {targetFieldName}__hasUpdate ? {targetFieldName} : {fieldName};");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Generate Set method with optional duration and interpolation parameters
        // Default is 0f duration (instant on next frame) and Step interpolation
        sb.AppendLine($"    public void Set{prop.Name}({prop.Type} value, float duration = 0f, global::Nexus.GameEngine.Components.InterpolationMode interpolation = global::Nexus.GameEngine.Components.InterpolationMode.Step)");
        sb.AppendLine("    {");
        // If a BeforeChange hook was specified, call it so callers can modify value/duration/interpolation
        if (!string.IsNullOrEmpty(prop.BeforeChange))
        {
            sb.AppendLine($"        {prop.BeforeChange}(ref value, ref duration, ref interpolation);");
            sb.AppendLine();
        }
        sb.AppendLine($"        if ({targetFieldName}__hasUpdate && {fieldName}__comparer.Equals({targetFieldName}, value))");
        sb.AppendLine("            return;");
        sb.AppendLine();
        sb.AppendLine($"        {targetFieldName} = value;");
        sb.AppendLine($"        {targetFieldName}__hasUpdate = true;");
        sb.AppendLine($"        _isDirty = true;");
        sb.AppendLine();
        sb.AppendLine($"        // TODO: Store duration and interpolation for animated updates");
        sb.AppendLine($"        // For now, all updates are instant (applied on next ApplyUpdates call)");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateApplyUpdatesMethod(StringBuilder sb, List<PropertyInfo> properties)
    {
        // Generate partial method declarations for property change callbacks
        foreach (var prop in properties)
        {
            sb.AppendLine($"    // Optional callback for {prop.Name} changes");
            
            // For reference types (including arrays), mark oldValue as nullable if not already nullable
            var paramType = prop.Type;
            if (prop.TypeSymbol?.IsValueType == false && !prop.Type.Contains("?"))
            {
                paramType = $"{prop.Type}?";
            }
            sb.AppendLine($"    partial void On{prop.Name}Changed({paramType} oldValue);");
            sb.AppendLine();
        }

        // Generate override of ApplyUpdates method to support inheritance
        sb.AppendLine("    // Generated deferred property update override");
        sb.AppendLine("    public override void ApplyUpdates(double deltaTime)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.ApplyUpdates(deltaTime);");
        sb.AppendLine();
        
        // Generate early-exit check for performance using single _isDirty flag
        if (properties.Count > 0)
        {
            sb.AppendLine("        // Early exit if no properties need processing (performance optimization)");
            sb.AppendLine("        if (!_isDirty)");
            sb.AppendLine("            return;");
            sb.AppendLine();
        }

        foreach (var prop in properties)
        {
            var fieldName = prop.FieldName;
            var targetFieldName = $"{fieldName}Target";

            // All properties are now instant update (animation is TODO for future)
            if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
            {
                // Arrays - use optimized array comparison (no LINQ overhead)
                var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                sb.AppendLine($"        // Array property - using optimized element-wise comparison");
                sb.AppendLine($"        if ({targetFieldName}__hasUpdate && ({targetFieldName} == null || {fieldName} == null ||");
                sb.AppendLine($"            {targetFieldName}.Length != {fieldName}.Length ||");
                sb.AppendLine($"            !ArraysEqual_{prop.Name}({targetFieldName}, {fieldName})))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var oldValue = {fieldName};");
                // Check if field type allows null (contains ?)
                var allowsNull = prop.Type.Contains("?");
                if (allowsNull)
                {
                    sb.AppendLine($"            {fieldName} = {targetFieldName} != null ? new {elementType}[{targetFieldName}.Length] : null;");
                    sb.AppendLine($"            if ({targetFieldName} != null && {fieldName} != null)");
                    sb.AppendLine($"                global::System.Array.Copy({targetFieldName}, {fieldName}, {targetFieldName}.Length);");
                }
                else
                {
                    sb.AppendLine($"            {fieldName} = {targetFieldName} ?? [];");
                }
                sb.AppendLine($"            {targetFieldName}__hasUpdate = false;");
                sb.AppendLine($"            On{prop.Name}Changed(oldValue!);");
                sb.AppendLine("        }");
            }
            else if (prop.IsCollection)
            {
                // Other collections - use SequenceEqual
                sb.AppendLine($"        // Collection property - using SequenceEqual for value comparison");
                sb.AppendLine($"        if ({targetFieldName}__hasUpdate && ({targetFieldName} == null || {fieldName} == null ||");
                sb.AppendLine($"            !global::System.Linq.Enumerable.SequenceEqual({targetFieldName}, {fieldName})))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var oldValue = {fieldName};");
                sb.AppendLine($"            {fieldName} = {targetFieldName}!;");
                sb.AppendLine($"            {targetFieldName}__hasUpdate = false;");
                sb.AppendLine($"            On{prop.Name}Changed(oldValue!);");
                sb.AppendLine("        }");
            }
            else
            {
                // Non-collection property - use default equality comparer
                sb.AppendLine($"        if ({targetFieldName}__hasUpdate && !{fieldName}__comparer.Equals({targetFieldName}, {fieldName}))");
                sb.AppendLine("        {");
                sb.AppendLine($"            var oldValue = {fieldName};");
                sb.AppendLine($"            {fieldName} = {targetFieldName};");
                sb.AppendLine($"            {targetFieldName}__hasUpdate = false;");
                var callbackArg = (prop.TypeSymbol?.IsValueType == false) ? "oldValue!" : "oldValue";
                sb.AppendLine($"            On{prop.Name}Changed({callbackArg});");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
        }

        // Clear dirty flag after processing
        if (properties.Count > 0)
        {
            sb.AppendLine("        // All properties processed, clear dirty flag");
            sb.AppendLine("        _isDirty = false;");
        }

        sb.AppendLine("    }");
        
        // Generate array comparison helper methods for each unique array element type
        var arrayProps = properties.Where(p => p.TypeSymbol is IArrayTypeSymbol).ToList();
        var uniqueArrayTypes = new HashSet<string>();
        
        foreach (var prop in arrayProps)
        {
            if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var methodName = $"ArraysEqual_{prop.Name}";
                
                if (uniqueArrayTypes.Add(elementType))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    // Helper for array comparison of {elementType}");
                    sb.AppendLine($"    private static bool {methodName}({prop.Type} a, {prop.Type} b)");
                    sb.AppendLine("    {");
                    sb.AppendLine("        if (a == null || b == null) return a == b;");
                    sb.AppendLine("        if (a.Length != b.Length) return false;");
                    sb.AppendLine("        for (int i = 0; i < a.Length; i++)");
                    sb.AppendLine($"            if (!global::System.Collections.Generic.EqualityComparer<{elementType}>.Default.Equals(a[i], b[i]))");
                    sb.AppendLine("                return false;");
                    sb.AppendLine("        return true;");
                    sb.AppendLine("    }");
                }
            }
        }
    }

    /// <summary>
    /// Generates optimized interpolation code for a specific type.
    /// Returns code that interpolates between 'start' and 'end' with parameter 't'.
    /// </summary>
    private static string GenerateInterpolationCode(ITypeSymbol type, string startExpr, string endExpr, string tExpr)
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Check for basic numeric types
        if (type.SpecialType == SpecialType.System_Single) // float
            return $"{startExpr} + ({endExpr} - {startExpr}) * {tExpr}";
        
        if (type.SpecialType == SpecialType.System_Double) // double
            return $"{startExpr} + ({endExpr} - {startExpr}) * {tExpr}";
        
        if (type.SpecialType == SpecialType.System_Int32) // int
            return $"(int)({startExpr} + ({endExpr} - {startExpr}) * {tExpr})";
        
        // Check for Silk.NET types (Vector, Matrix, Quaternion, etc.)
        if (type.ContainingNamespace?.ToDisplayString() == "Silk.NET.Maths")
        {
            var simpleTypeName = type.Name;
            
            // Quaternion needs special handling - use SLERP instead of linear interpolation
            if (simpleTypeName.StartsWith("Quaternion"))
            {
                return $"global::Silk.NET.Maths.Quaternion.Slerp({startExpr}, {endExpr}, {tExpr})";
            }
            
            // Vector and Matrix types support operator overloads for linear interpolation
            if (simpleTypeName.StartsWith("Vector") || simpleTypeName.StartsWith("Matrix"))
            {
                // Silk.NET types support operator+ and operator*
                return $"{startExpr} + ({endExpr} - {startExpr}) * {tExpr}";
            }
        }
        
        // Check if type implements IInterpolatable<T>
        var implementsInterpolatable = false;
        if (type is INamedTypeSymbol namedType)
        {
            implementsInterpolatable = namedType.AllInterfaces
                .Any(i => i.Name == "IInterpolatable" && i.IsGenericType);
        }
        
        if (implementsInterpolatable)
        {
            return $"{startExpr}.Interpolate({endExpr}, {tExpr}, Interpolation)";
        }
        
        // Fallback: return end value (step interpolation)
        return endExpr;
    }

    private class PropertyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public ITypeSymbol? TypeSymbol { get; set; }
        public bool IsCollection { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        // Optional name of a generated hook to call before queuing the change
        public string? BeforeChange { get; set; }
    }
}
