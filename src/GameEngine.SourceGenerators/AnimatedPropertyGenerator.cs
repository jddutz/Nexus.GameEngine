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

            // Check if type is a collection (array or implements IEnumerable<T>)
            var isCollection = IsCollectionType(field.Type);

            properties.Add(new PropertyInfo
            {
                Name = propertyName,
                FieldName = field.Name,
                Type = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TypeSymbol = field.Type,
                IsCollection = isCollection,
                HasAnimationAttribute = true,
                Duration = GetAttributeValue<float>(animationAttr, "Duration", 0f),
                Interpolation = GetAttributeValue<int>(animationAttr, "Interpolation", 0)
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

        // For value types, we can't use nullable to track initialization, so use a separate bool flag
        // For reference types, we could use nullable, but for consistency use the flag approach for all types
        sb.AppendLine($"    private bool {targetFieldName}__initialized;");
        sb.AppendLine($"    private {prop.Type} {targetFieldName}__value = default!;");
        sb.AppendLine($"    private {prop.Type} {targetFieldName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {targetFieldName}__initialized ? {targetFieldName}__value : {fieldName};");
        sb.AppendLine($"        set {{ {targetFieldName}__value = value; {targetFieldName}__initialized = true; }}");
        sb.AppendLine("    }");

        if (prop.HasAnimationAttribute && prop.Duration > 0)
        {
            var animFieldName = $"{fieldName}Animation";
            
            // Check if this is an array property that needs special handling
            if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Generate inline animation tracking fields (no ArrayPropertyAnimation class needed)
                sb.AppendLine($"    // Inline animation state for {prop.Name} (array property)");
                sb.AppendLine($"    private class {animFieldName}State");
                sb.AppendLine("    {");
                sb.AppendLine($"        public bool IsAnimating {{ get; set; }}");
                sb.AppendLine($"        public float Duration {{ get; set; }} = {prop.Duration}f;");
                sb.AppendLine("    }");
                sb.AppendLine($"    private {animFieldName}State {animFieldName} = new();");
                sb.AppendLine($"    private {elementType}[] {animFieldName}_startValue = default!;");
                sb.AppendLine($"    private {elementType}[] {animFieldName}_endValue = default!;");
                sb.AppendLine($"    private double {animFieldName}_elapsed;");
            }
            else
            {
                sb.AppendLine($"    private global::Nexus.GameEngine.Components.PropertyAnimation<{prop.Type}> {animFieldName} = new()");
                sb.AppendLine("    {");
                sb.AppendLine($"        Duration = {prop.Duration}f,");
                sb.AppendLine($"        Interpolation = (global::Nexus.GameEngine.Animation.InterpolationMode){prop.Interpolation}");
                sb.AppendLine("    };");
            }
        }

        sb.AppendLine();
        
        // Generate read-only property
        sb.AppendLine($"    public {prop.Type} {prop.Name}");
        sb.AppendLine("    {");
        sb.AppendLine($"        get => {fieldName};");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Generate Set method with optional duration and interpolation parameters
        // Use -1 as sentinel value for "use attribute default"
        var defaultInterpolation = prop.Interpolation;
        sb.AppendLine($"    public void Set{prop.Name}({prop.Type} value, float duration = -1f, global::Nexus.GameEngine.Animation.InterpolationMode interpolation = (global::Nexus.GameEngine.Animation.InterpolationMode)(-1))");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default.Equals({targetFieldName}, value))");
        sb.AppendLine("            return;");
        sb.AppendLine();
        sb.AppendLine($"        {targetFieldName} = value;");
        sb.AppendLine();
        sb.AppendLine($"        // Use attribute defaults if not specified (-1 sentinel)");
        sb.AppendLine($"        if (duration < 0f)");
        sb.AppendLine($"            duration = {prop.Duration}f;");
        sb.AppendLine($"        if ((int)interpolation < 0)");
        sb.AppendLine($"            interpolation = (global::Nexus.GameEngine.Animation.InterpolationMode){prop.Interpolation};");

        if (prop.HasAnimationAttribute && prop.Duration > 0)
        {
            var animFieldName = $"{fieldName}Animation";
            
            sb.AppendLine();
            sb.AppendLine($"        // Update animation settings if duration > 0");
            sb.AppendLine($"        if (duration > 0f)");
            sb.AppendLine("        {");
            sb.AppendLine($"            {animFieldName}.Duration = duration;");
            sb.AppendLine($"            {animFieldName}.Interpolation = interpolation;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Start inline array animation (only if component is active)
                sb.AppendLine($"        // Only animate if component is active and duration > 0; otherwise apply immediately");
                sb.AppendLine($"        if (IsActive && duration > 0f)");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Deep copy the current array to avoid reference issues during animation");
                sb.AppendLine($"            if ({fieldName} != null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                {animFieldName}_startValue = new {elementType}[{fieldName}.Length];");
                sb.AppendLine($"                global::System.Array.Copy({fieldName}, {animFieldName}_startValue, {fieldName}.Length);");
                sb.AppendLine("            }");
                sb.AppendLine($"            else");
                sb.AppendLine("            {");
                sb.AppendLine($"                {animFieldName}_startValue = {targetFieldName};");
                sb.AppendLine("            }");
                sb.AppendLine($"            {animFieldName}_endValue = {targetFieldName};");
                sb.AppendLine($"            {animFieldName}_elapsed = 0.0;");
                sb.AppendLine($"            {animFieldName}.IsAnimating = true;");
                sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                sb.AppendLine("        }");
                sb.AppendLine("        else");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Apply immediately during configuration or when duration = 0");
                sb.AppendLine($"            {fieldName} = {targetFieldName};");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        if (IsActive && duration > 0f)");
                sb.AppendLine("        {");
                sb.AppendLine($"            {animFieldName}.StartAnimation({fieldName}, {targetFieldName}, 0.0); // TODO: Use TimeProvider");
                sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                sb.AppendLine("        }");
                sb.AppendLine("        else");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Apply immediately during configuration or when duration = 0");
                sb.AppendLine($"            {fieldName} = {targetFieldName};");
                sb.AppendLine("        }");
            }
        }
        else
        {
            // Property without animation - always instant
            sb.AppendLine($"        OnPropertyAnimationStarted(nameof({prop.Name}));");
            sb.AppendLine($"        {fieldName} = {targetFieldName};");
            sb.AppendLine($"        NotifyPropertyChanged(nameof({prop.Name}));");
            sb.AppendLine($"        OnPropertyAnimationEnded(nameof({prop.Name}));");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateUpdateAnimationsMethod(StringBuilder sb, List<PropertyInfo> properties)
    {
        // Generate partial method declarations for property change callbacks
        foreach (var prop in properties)
        {
            sb.AppendLine($"    // Optional callback for {prop.Name} changes");
            
            // For reference types (including arrays), mark oldValue as nullable since it can be null during initialization
            var paramType = prop.TypeSymbol?.IsValueType == false ? $"{prop.Type}?" : prop.Type;
            sb.AppendLine($"    partial void On{prop.Name}Changed({paramType} oldValue);");
            sb.AppendLine();
        }

        // Generate override of UpdateAnimations method to support inheritance
        sb.AppendLine("    // Generated animation update override");
        sb.AppendLine("    protected override void UpdateAnimations(double deltaTime)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.UpdateAnimations(deltaTime);");
        sb.AppendLine();

        foreach (var prop in properties)
        {
            var fieldName = prop.FieldName;
            var targetFieldName = $"{fieldName}Target";

            if (prop.HasAnimationAttribute && prop.Duration > 0)
            {
                // Animated property
                var animFieldName = $"{fieldName}Animation";
                
                if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
                {
                    // Animated array property - inline interpolation for zero-overhead
                    var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var elementTypeName = arrayType.ElementType.Name;
                    
                    sb.AppendLine($"        if ({animFieldName}.IsAnimating)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            {animFieldName}_elapsed += deltaTime;");
                    sb.AppendLine($"            if ({animFieldName}_elapsed >= {animFieldName}.Duration)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                // Animation complete - snap to end value");
                    sb.AppendLine($"                var oldValue = {fieldName};");
                    sb.AppendLine($"                {fieldName} = {animFieldName}_endValue;");
                    sb.AppendLine($"                {animFieldName}.IsAnimating = false;");
                    sb.AppendLine($"                NotifyPropertyChanged(nameof({prop.Name}));");
                    sb.AppendLine($"                On{prop.Name}Changed(oldValue!);");
                    sb.AppendLine($"                OnPropertyAnimationEnded(nameof({prop.Name}));");
                    sb.AppendLine("            }");
                    sb.AppendLine("            else");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                // Interpolate array elements");
                    sb.AppendLine($"                var oldValue = {fieldName};");
                    sb.AppendLine($"                float t = (float)({animFieldName}_elapsed / {animFieldName}.Duration);");
                    sb.AppendLine($"                // Apply easing (simplified - just linear for now)");
                    sb.AppendLine($"                // TODO: Add easing function support");
                    sb.AppendLine();
                    sb.AppendLine($"                // Allocate new array and interpolate each element");
                    sb.AppendLine($"                var newArray = new {elementType}[{animFieldName}_startValue.Length];");
                    sb.AppendLine($"                for (int i = 0; i < newArray.Length; i++)");
                    sb.AppendLine("                {");
                    
                    // Generate optimized interpolation code for the element type
                    var interpCode = GenerateInterpolationCode(
                        arrayType.ElementType,
                        $"{animFieldName}_startValue[i]",
                        $"{animFieldName}_endValue[i]",
                        "t");
                    
                    sb.AppendLine($"                    newArray[i] = {interpCode};");
                    sb.AppendLine("                }");
                    sb.AppendLine();
                    sb.AppendLine($"                {fieldName} = newArray;");
                    sb.AppendLine($"                NotifyPropertyChanged(nameof({prop.Name}));");
                    sb.AppendLine($"                On{prop.Name}Changed(oldValue!);");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
                else
                {
                    // Non-array animated property
                    sb.AppendLine($"        if ({animFieldName}.IsAnimating)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var oldValue = {fieldName};");
                    sb.AppendLine($"            {fieldName} = {animFieldName}.Update(deltaTime);");
                    sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                    // For non-array types, only use ! if reference type
                    var callbackArg = (prop.TypeSymbol?.IsValueType == false) ? "oldValue!" : "oldValue";
                    sb.AppendLine($"            On{prop.Name}Changed({callbackArg});");
                    sb.AppendLine();
                    sb.AppendLine($"            if (!{animFieldName}.IsAnimating)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                OnPropertyAnimationEnded(nameof({prop.Name}));");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
            }
            else
            {
                // Instant update property
                if (prop.TypeSymbol is IArrayTypeSymbol arrayType)
                {
                    // Arrays - use optimized array comparison (no LINQ overhead)
                    var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    
                    sb.AppendLine($"        // Array property - using optimized element-wise comparison");
                    sb.AppendLine($"        if ({targetFieldName} == null || {fieldName} == null ||");
                    sb.AppendLine($"            {targetFieldName}.Length != {fieldName}.Length ||");
                    sb.AppendLine($"            !ArraysEqual({targetFieldName}, {fieldName}))");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var oldValue = {fieldName};");
                    sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                    sb.AppendLine($"            // Deep copy array to avoid reference sharing");
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
                        sb.AppendLine($"            {fieldName} = new {elementType}[{targetFieldName}?.Length ?? 0];");
                        sb.AppendLine($"            if ({targetFieldName} != null)");
                        sb.AppendLine($"                global::System.Array.Copy({targetFieldName}!, {fieldName}, {targetFieldName}.Length);");
                    }
                    sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                    sb.AppendLine($"            On{prop.Name}Changed(oldValue!);");
                    sb.AppendLine($"            OnPropertyAnimationEnded(nameof({prop.Name}));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    
                    // Generate array comparison helper method (inline for performance)
                    if (!properties.Any(p => p != prop && p.TypeSymbol is IArrayTypeSymbol at && 
                        SymbolEqualityComparer.Default.Equals(at.ElementType, arrayType.ElementType)))
                    {
                        // Only generate once per element type
                        sb.AppendLine($"        // Helper for array comparison of {elementType}");
                        sb.AppendLine($"        static bool ArraysEqual({prop.Type} a, {prop.Type} b)");
                        sb.AppendLine("        {");
                        sb.AppendLine("            if (a == null || b == null) return a == b;");
                        sb.AppendLine("            if (a.Length != b.Length) return false;");
                        sb.AppendLine("            for (int i = 0; i < a.Length; i++)");
                        sb.AppendLine($"                if (!global::System.Collections.Generic.EqualityComparer<{elementType}>.Default.Equals(a[i], b[i]))");
                        sb.AppendLine("                    return false;");
                        sb.AppendLine("            return true;");
                        sb.AppendLine("        }");
                    }
                }
                else if (prop.IsCollection)
                {
                    // Other collections - use SequenceEqual
                    sb.AppendLine($"        // Collection property - using SequenceEqual for value comparison");
                    sb.AppendLine($"        if ({targetFieldName} == null || {fieldName} == null ||");
                    sb.AppendLine($"            !global::System.Linq.Enumerable.SequenceEqual({targetFieldName}, {fieldName}))");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var oldValue = {fieldName};");
                    sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                    sb.AppendLine($"            {fieldName} = {targetFieldName};");
                    sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                    sb.AppendLine($"            On{prop.Name}Changed(oldValue!);");
                    sb.AppendLine($"            OnPropertyAnimationEnded(nameof({prop.Name}));");
                    sb.AppendLine("        }");
                }
                else
                {
                    // Non-collection property - use default equality comparer
                    sb.AppendLine($"        if (!global::System.Collections.Generic.EqualityComparer<{prop.Type}>.Default.Equals({targetFieldName}, {fieldName}))");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var oldValue = {fieldName};");
                    sb.AppendLine($"            OnPropertyAnimationStarted(nameof({prop.Name}));");
                    sb.AppendLine($"            {fieldName} = {targetFieldName};");
                    sb.AppendLine($"            NotifyPropertyChanged(nameof({prop.Name}));");
                    var callbackArg = (prop.TypeSymbol?.IsValueType == false) ? "oldValue!" : "oldValue";
                    sb.AppendLine($"            On{prop.Name}Changed({callbackArg});");
                    sb.AppendLine($"            OnPropertyAnimationEnded(nameof({prop.Name}));");
                    sb.AppendLine("        }");
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("    }");
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
        public bool HasAnimationAttribute { get; set; }
        public float Duration { get; set; }
        public int Interpolation { get; set; }
    }
}
