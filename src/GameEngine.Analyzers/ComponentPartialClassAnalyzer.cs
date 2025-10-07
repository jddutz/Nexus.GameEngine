using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nexus.GameEngine.Analyzers;

/// <summary>
/// Analyzer NX1001: Ensures classes implementing IRuntimeComponent are declared partial.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComponentPartialClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NX1001";

    private static readonly LocalizableString Title = "Class implementing IRuntimeComponent must be declared partial";
    private static readonly LocalizableString MessageFormat = "Class '{0}' implements IRuntimeComponent but is not declared partial. Source generation requires partial classes.";
    private static readonly LocalizableString Description = "Classes implementing IRuntimeComponent must be declared partial to allow source generation of animated property implementations.";
    private const string Category = "Design";

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
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (classSymbol is null) return;

        // Check if class implements IRuntimeComponent
        var implementsInterface = classSymbol.AllInterfaces
            .Any(i => i.Name == "IRuntimeComponent");

        if (!implementsInterface) return;

        // Check if class is partial
        var isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!isPartial)
        {
            var diagnostic = Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(), classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
