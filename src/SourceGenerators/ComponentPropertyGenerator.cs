using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nexus.SourceGenerators;

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
            
            // Read optional NotifyChange named argument from attribute
            var notifyChange = GetAttributeValue<bool>(animationAttr, "NotifyChange", false);

            properties.Add(new PropertyInfo
            {
                Name = propertyName,
                FieldName = field.Name,
                Type = field.Type.ToDisplayString(displayFormat),
                TypeSymbol = field.Type,
                IsCollection = isCollection,
                // Duration and Interpolation are now runtime parameters, not attribute properties
                DefaultValue = GetFieldDefaultValue(field),
                BeforeChange = beforeChange,
                NotifyChange = notifyChange
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
        var stateFieldName = $"{fieldName}State";

        sb.AppendLine($"    // Generated property: {prop.Name} (from field {fieldName})");

        // State field to store the pending value and interpolator
        sb.AppendLine($"    private global::Nexus.GameEngine.Components.ComponentPropertyUpdater<{prop.Type}> {stateFieldName};");
        
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
        sb.AppendLine($"        get => {stateFieldName}.GetTarget({fieldName});");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Generate Set method with optional interpolation function
        sb.AppendLine($"    public void Set{prop.Name}({prop.Type} value, global::Nexus.GameEngine.Components.InterpolationFunction<{prop.Type}>? interpolator = null)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var oldValue = {fieldName};");
        sb.AppendLine($"        if ({stateFieldName}.Set(ref {fieldName}, value, interpolator))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (interpolator == null)");
        sb.AppendLine($"            {{");
        var callbackArgSet = (prop.TypeSymbol?.IsValueType == false) ? "oldValue!" : "oldValue";
        sb.AppendLine($"                On{prop.Name}Changed({callbackArgSet});");
        if (prop.NotifyChange)
        {
            sb.AppendLine($"                {prop.Name}Changed?.Invoke(this, new global::Nexus.GameEngine.Events.PropertyChangedEventArgs<{prop.Type}>({callbackArgSet}, value));");
        }
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                _isDirty = true;");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        }}");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate SetCurrent method
        sb.AppendLine($"    public void SetCurrent{prop.Name}({prop.Type} value, bool setTarget = false)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var oldValue = {fieldName};");
        sb.AppendLine($"        {fieldName} = value;");
        sb.AppendLine($"        if (setTarget)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {stateFieldName}.Set(ref {fieldName}, value, null);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        if (!global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default.Equals(oldValue, value))");
        sb.AppendLine("        {");
        var callbackArg = (prop.TypeSymbol?.IsValueType == false) ? "oldValue!" : "oldValue";
        sb.AppendLine($"            On{prop.Name}Changed({callbackArg});");
        if (prop.NotifyChange)
        {
            sb.AppendLine($"            {prop.Name}Changed?.Invoke(this, new global::Nexus.GameEngine.Events.PropertyChangedEventArgs<{prop.Type}>({callbackArg}, value));");
        }
        sb.AppendLine("        }");
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

            if (prop.NotifyChange)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Event raised when the {prop.Name} property changes.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public event global::System.EventHandler<global::Nexus.GameEngine.Events.PropertyChangedEventArgs<{prop.Type}>>? {prop.Name}Changed;");
                sb.AppendLine();
            }
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
            var stateFieldName = $"{fieldName}State";

            sb.AppendLine($"        // Apply updates for {prop.Name}");
            sb.AppendLine($"        var {fieldName}Old = {fieldName};");
            sb.AppendLine($"        if ({stateFieldName}.Apply(ref {fieldName}, (float)deltaTime))");
            sb.AppendLine("        {");
            var callbackArg = (prop.TypeSymbol?.IsValueType == false) ? $"{fieldName}Old!" : $"{fieldName}Old";
            sb.AppendLine($"            On{prop.Name}Changed({callbackArg});");
            if (prop.NotifyChange)
            {
                sb.AppendLine($"            {prop.Name}Changed?.Invoke(this, new global::Nexus.GameEngine.Events.PropertyChangedEventArgs<{prop.Type}>({callbackArg}, {fieldName}));");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Clear dirty flag after processing
        if (properties.Count > 0)
        {
            sb.AppendLine("        // Check if any properties still have pending updates");
            var checks = properties.Select(p => $"{p.FieldName}State.HasPendingUpdate");
            sb.AppendLine($"        _isDirty = {string.Join(" || ", checks)};");
        }

        sb.AppendLine("    }");
        
        // No helper methods needed anymore (ArraysEqual, etc. are handled by EqualityComparer.Default in the struct or user logic)
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
        // Whether to generate PropertyChanged notification for this property
        public bool NotifyChange { get; set; }
    }
}
