using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Code2Rule
{
    private static readonly HashSet<string> TiposPerigosos = new(StringComparer.Ordinal)
    {
        "Boolean", "DateTime", "Int64", "Int32", "Decimal", "Double",
        "bool", "decimal", "double"
    };

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;
        if (string.IsNullOrWhiteSpace(scriptText)) return results;

        var root = CSharpSyntaxTree.ParseText(scriptText).GetCompilationUnitRoot();

        // Rastrear expressões já reportadas para evitar duplicatas quando
        // o mesmo CastExpression aparece como filho de outro nó pai
        var jaReportados = new HashSet<int>();

        var casts = root.DescendantNodes().OfType<CastExpressionSyntax>();

        foreach (var cast in casts)
        {
            var tipo = cast.Type.ToString();
            if (!TiposPerigosos.Contains(tipo)) continue;

            var operando = cast.Expression.ToString();
            // Cobre Row["Campo"] e Row[variavel]
            if (!operando.Contains("Row[")) continue;

            // Usar a posição no source como chave para deduplicar
            var spanStart = cast.SpanStart;
            if (!jaReportados.Add(spanStart)) continue;

            var metodo = cast.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            var lineSpan = cast.GetLocation().GetLineSpan();
            var linha = lineSpan.StartLinePosition.Line + 1;

            results.Add(new RuleResult
            {
                RuleCode = "Code-2",
                Severity = Severity.Error,
                ComponentName = metodo?.Identifier.Text ?? "ScriptText",
                Message = $"Cast direto ({tipo}) em acesso a Row[] sem verificação de nulo — lança InvalidCastException se o campo for DBNull.",
                Detail = $"Linha {linha}: {cast}"
            });
        }

        return results;
    }
}
