using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nexus.GameEngine.SourceGenerators
{
    [Generator]
    public class LoadMethodGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)
                .Collect();

            context.RegisterSourceOutput(classDeclarations, static (spc, source) => Execute(spc, source));
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax cds && 
                cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        static ClassToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (symbol == null) return null;
            if (!ImplementsIConfigurable(symbol)) return null;

            var properties = new List<PropertyInfo>();

            foreach (var member in symbol.GetMembers())
            {
                var attribute = member.GetAttributes()
                    .FirstOrDefault(ad => ad.AttributeClass?.Name == "TemplatePropertyAttribute");

                if (attribute != null)
                {
                    string? name = attribute.NamedArguments
                        .FirstOrDefault(kvp => kvp.Key == "Name").Value.Value as string;

                    ITypeSymbol? type = null;
                    string? setterName = null;
                    bool isField = false;
                    string fieldName = member.Name;

                    if (member is IFieldSymbol field)
                    {
                        type = field.Type;
                        isField = true;
                        if (name == null)
                        {
                            name = field.Name.TrimStart('_');
                            if (name.Length > 0)
                                name = char.ToUpper(name[0]) + name.Substring(1);
                        }
                        // For fields, we just set the field directly. No setter needed.
                        setterName = null;
                    }
                    else if (member is IMethodSymbol method)
                    {
                        if (method.Parameters.Length == 1)
                        {
                            type = method.Parameters[0].Type;
                            if (name == null)
                            {
                                name = method.Name;
                                if (name.StartsWith("Set"))
                                    name = name.Substring(3);
                            }
                            setterName = method.Name;
                        }
                    }

                    if (name != null && type != null)
                    {
                        properties.Add(new PropertyInfo(
                            name, 
                            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            setterName,
                            fieldName,
                            isField,
                            type.IsValueType
                        ));
                    }
                }
            }

            if (properties.Count == 0) return null;

            var templateName = $"{symbol.Name}Template";

            return new ClassToGenerate(
                symbol.Name,
                symbol.ContainingNamespace.ToDisplayString(),
                templateName,
                properties
            );
        }

        static bool ImplementsIConfigurable(INamedTypeSymbol symbol)
        {
            return symbol.AllInterfaces.Any(i => i.Name == "IConfigurable");
        }

        static void Execute(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<ClassToGenerate?> classesToGenerate)
        {
            var processedNames = new HashSet<string>();

            foreach (var classToGenerate in classesToGenerate)
            {
                if (classToGenerate == null) continue;

                var fullName = $"{classToGenerate.Namespace}.{classToGenerate.Name}";
                if (processedNames.Contains(fullName)) continue;
                processedNames.Add(fullName);

                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine("using System;");
                sb.AppendLine("using Nexus.GameEngine.Components;");
                sb.AppendLine();
                sb.AppendLine($"namespace {classToGenerate.Namespace}");
                sb.AppendLine("{");
                sb.AppendLine($"    public partial class {classToGenerate.Name}");
                sb.AppendLine("    {");

                // Check if root component
                bool isRoot = classToGenerate.Name == "Component" && classToGenerate.Namespace == "Nexus.GameEngine.Components";

                // Generate Configure override
                if (isRoot)
                {
                    sb.AppendLine($"        protected virtual void Configure(Template template)");
                    sb.AppendLine("        {");
                }
                else
                {
                    sb.AppendLine($"        protected override void Configure(Template template)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            base.Configure(template);");
                }

                sb.AppendLine($"            if (template is {classToGenerate.TemplateName} t)");
                sb.AppendLine("            {");
                
                foreach (var prop in classToGenerate.Properties)
                {
                    var propName = prop.Name;
                    
                    if (prop.IsValueType)
                    {
                        sb.AppendLine($"                if (t.{propName}.HasValue)");
                        sb.AppendLine("                {");
                        var valueAccess = $"t.{propName}.Value";
                        if (prop.SetterName != null)
                        {
                            sb.AppendLine($"                    {prop.SetterName}({valueAccess});");
                        }
                        else
                        {
                            sb.AppendLine($"                    {prop.FieldName} = {valueAccess};");
                        }
                        sb.AppendLine("                }");
                    }
                    else
                    {
                        sb.AppendLine($"                if (t.{propName} != null)");
                        sb.AppendLine("                {");
                        var valueAccess = $"t.{propName}!";
                        if (prop.SetterName != null)
                        {
                            sb.AppendLine($"                    {prop.SetterName}({valueAccess});");
                        }
                        else
                        {
                            sb.AppendLine($"                    {prop.FieldName} = {valueAccess};");
                        }
                        sb.AppendLine("                }");
                    }
                }
                
                sb.AppendLine("            }");
                sb.AppendLine("        }");

                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource($"{classToGenerate.Name}.Load.g.cs", sb.ToString());
            }
        }

        record ClassToGenerate(
            string Name,
            string Namespace,
            string TemplateName,
            List<PropertyInfo> Properties
        );

        record PropertyInfo(
            string Name,
            string Type,
            string? SetterName,
            string FieldName,
            bool IsField,
            bool IsValueType
        );
    }
}
