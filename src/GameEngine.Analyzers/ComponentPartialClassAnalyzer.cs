using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nexus.GameEngine.Analyzers;

/// <summary>
/// Analyzer NX1001: Ensures classes deriving from Entity are declared partial.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ComponentPartialClassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NX1001";

    private static readonly LocalizableString Title = "Class deriving from Entity must be declared partial";
    private static readonly LocalizableString MessageFormat = "Class '{0}' derives from Entity but is not declared partial. Source generation requires partial classes.";
    private static readonly LocalizableString Description = "Classes deriving from Entity must be declared partial to allow source generation of animated property implementations.";
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

        // Check if class derives from Entity
        var derivesFromComponentBase = InheritsFromComponentBase(classSymbol);

        if (!derivesFromComponentBase) return;

        // Check if class is partial
        var isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!isPartial)
        {
            var diagnostic = Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(), classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
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
}
