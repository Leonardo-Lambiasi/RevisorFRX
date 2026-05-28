using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Code2Rule
{
    private static readonly HashSet<string> TiposString = new(StringComparer.Ordinal)
    {
        "string", "String", "System.String",
        "object", "Object", "System.Object"
    };

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;
        if (string.IsNullOrWhiteSpace(scriptText)) return results;

        var tree = CSharpSyntaxTree.ParseText(scriptText);
        var compilation = CSharpCompilation.Create("Script")
            .AddReferences(MetadataReference.CreateFromFile(
                typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetCompilationUnitRoot();

        var casts = root.DescendantNodes().OfType<CastExpressionSyntax>();

        foreach (var cast in casts)
        {
            var operando = cast.Expression.ToString();
            if (!operando.Contains("Row[")) continue;

            var tipoInfo = model.GetTypeInfo(cast.Type).Type;
            if (tipoInfo == null) continue;

            var tipoNome = cast.Type.ToString();
            if (TiposString.Contains(tipoNome)) continue;

            // Se for Nullable<T>, verifica se tem .Value sem HasValue
            if (tipoInfo.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                VerificarValueSemHasValue(cast, results);
                continue;
            }

            // Flag se for value type (qualquer um, não só os conhecidos)
            if (tipoInfo.IsValueType)
            {
                var metodo = cast.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();

                results.Add(new RuleResult
                {
                    RuleCode = "Code-2",
                    Severity = Severity.Error,
                    ComponentName = metodo?.Identifier.Text ?? "ScriptText",
                    Message = $"Cast direto ({tipoNome}) em acesso a Row[] sem verificação " +
                              $"de nulo — lança InvalidCastException se o campo for DBNull.",
                    Detail = $"Expressão: {cast}"
                });
            }
        }

        return results;
    }

    private static void VerificarValueSemHasValue(
        CastExpressionSyntax cast, List<RuleResult> results)
    {
        // Procura se esse cast é seguido de .Value sem verificação HasValue
        // Ex: ((DateTime?)Row["x"]).Value  ou  (Nullable<DateTime>)Row["x"]).Value
        var castStr = cast.ToString();

        // Anda para o nó pai — pode ser ParenthesizedExpressionSyntax
        // que está sendo acessado como .Value
        var parent = cast.Parent;
        while (parent is ParenthesizedExpressionSyntax)
            parent = parent.Parent;

        if (parent is MemberAccessExpressionSyntax member
            && member.Name.Identifier.ValueText == "Value")
        {
            var metodo = member.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            results.Add(new RuleResult
            {
                RuleCode = "Code-2",
                Severity = Severity.Error,
                ComponentName = metodo?.Identifier.Text ?? "ScriptText",
                Message = $"Acesso a '.Value' em Nullable de Row[] sem verificação " +
                          $"de HasValue — lança InvalidOperationException se o campo for nulo.",
                Detail = $"Expressão: {member}"
            });
        }
    }
}
