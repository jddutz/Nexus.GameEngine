using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nexus.Analyzers;

/// <summary>
/// Analyzer NX1003: Validates interpolation mode compatibility with property type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InterpolationModeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NX1003";

    private static readonly LocalizableString Title = "Invalid interpolation mode for field type";
    private static readonly LocalizableString MessageFormat = "Field '{0}' of type '{1}' cannot use interpolation mode '{2}'. Use an appropriate interpolation mode for this type.";
    private static readonly LocalizableString Description = "Certain interpolation modes like Slerp are only valid for specific types (e.g., quaternions and unit vectors).";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
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
            var animationAttr = fieldSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ComponentPropertyAttribute");

            if (animationAttr is null) continue;

            // Get interpolation mode
            var interpolationArg = animationAttr.NamedArguments
                .FirstOrDefault(a => a.Key == "Interpolation");

            if (interpolationArg.Value.Value is int interpolationMode)
            {
                var fieldType = fieldSymbol.Type;

                // Check for Slerp on non-quaternion/vector types
                if (interpolationMode == 8) // Slerp
                {
                    var typeName = fieldType.Name;
                    if (typeName != "Quaternion" && !typeName.StartsWith("Vector"))
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            variable.GetLocation(),
                            fieldSymbol.Name,
                            fieldType.ToDisplayString(),
                            "Slerp");
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
