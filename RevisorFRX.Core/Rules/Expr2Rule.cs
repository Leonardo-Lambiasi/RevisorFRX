using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Expr2Rule
{
    private static readonly Regex ExprRegex =
        new(@"\[Dados\.([^\]\[()]+)\]", RegexOptions.Compiled);

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();
        var typeMap = SchemaBuilder.BuildTypeMap(doc);

        var camposNulos = typeMap
            .Where(kv => string.IsNullOrEmpty(kv.Value))
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (camposNulos.Count == 0) return results;

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject"
                     && e.Attribute("Text") != null);

        foreach (var texto in textos)
        {
            var textValue = texto.Attribute("Text")!.Value;
            var matches = ExprRegex.Matches(textValue);

            foreach (Match match in matches)
            {
                if (match.Index > 0 && textValue[match.Index - 1] == '[') continue;

                var caminho = match.Groups[1].Value.Replace(" ", "");

                if (caminho.Contains('(') || caminho.Contains(',')) continue;

                if (camposNulos.Contains(caminho))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Expr-2",
                        Severity = Severity.Warning,
                        ComponentName = texto.Attribute("Name")?.Value ?? "TextObject",
                        Message = $"Campo '[Dados.{caminho}]' tem DataType nulo e não pode ser exibido como texto.",
                        Detail = $"O campo '{caminho}' é um objeto não escalar (DataType=\"null\"). " +
                                 $"Referencie uma propriedade filha dele, ex: '[Dados.{caminho}.Propriedade]'."
                    });
                }
            }
        }

        return results;
    }
}
