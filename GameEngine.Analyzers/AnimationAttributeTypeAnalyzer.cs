using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nexus.GameEngine.Analyzers;

/// <summary>
/// Analyzer NX1002: Validates that [ComponentProperty] attribute is only applied to animatable types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AnimationAttributeTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NX1002";

    private static readonly LocalizableString Title = "ComponentProperty attribute on non-animatable type";
    private static readonly LocalizableString MessageFormat = "Field '{0}' of type '{1}' cannot be animated. Only numeric, vector, matrix, and quaternion types support animation.";
    private static readonly LocalizableString Description = "The [ComponentProperty] attribute can only be applied to fields of animatable types (numeric primitives, vectors, matrices, quaternions, or types implementing IInterpolatable<T>).";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var fieldDecl = (FieldDeclarationSyntax)context.Node;

        foreach (var variable in fieldDecl.Declaration.Variables)
        {
            var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (fieldSymbol is null) continue;

            // Check if field has ComponentProperty attribute
            var hasAnimationAttr = fieldSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "ComponentPropertyAttribute");

            if (!hasAnimationAttr) continue;

            // Check if type is animatable
            var fieldType = fieldSymbol.Type;

            if (!IsAnimatableType(fieldType))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    variable.GetLocation(),
                    fieldSymbol.Name,
                    fieldType.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsAnimatableType(ITypeSymbol type)
    {
        // Check for numeric primitives
        switch (type.SpecialType)
        {
            case SpecialType.System_Single:   // float
            case SpecialType.System_Double:   // double
            case SpecialType.System_Int32:    // int
            case SpecialType.System_Int64:    // long
            case SpecialType.System_Int16:    // short
            case SpecialType.System_Byte:     // byte
            case SpecialType.System_SByte:    // sbyte
            case SpecialType.System_UInt32:   // uint
            case SpecialType.System_UInt64:   // ulong
            case SpecialType.System_UInt16:   // ushort
                return true;
        }

        var typeName = type.Name;

        // Check for common animatable types
        if (typeName.StartsWith("Vector") ||
            typeName.StartsWith("Matrix") ||
            typeName == "Quaternion" ||
            typeName == "Rectangle")
        {
            return true;
        }

        // Check if implements IInterpolatable<T>
        if (type is INamedTypeSymbol namedType)
        {
            var implementsInterpolatable = namedType.AllInterfaces
                .Any(i => i.Name == "IInterpolatable" && i.IsGenericType);

            if (implementsInterpolatable) return true;
        }

        // Reference types and bool cannot be animated
        return false;
    }
}
