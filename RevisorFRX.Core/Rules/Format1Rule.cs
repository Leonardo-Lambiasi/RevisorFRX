using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Format1Rule
{
    private static readonly Regex ExprRegex =
        new(@"\[Dados\.([^\]\[()]+)\]", RegexOptions.Compiled);

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var schema = ConstruirMapaDeTipos(doc);
        if (schema.Count == 0) return results;

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject"
                     && e.Attribute("Text") != null);

        foreach (var texto in textos)
        {
            var text    = texto.Attribute("Text")!.Value;
            var format = texto.Attribute("Format")?.Value ?? "";
            var nome   = texto.Attribute("Name")?.Value ?? "TextObject";

            // Expressões matemáticas ([[Dados.A] + [Dados.B]]) não têm formatação
            // por campo — o Format do TextObject se aplica ao resultado da conta inteira.
            if (text.Contains("[[")) continue;

            var matches = ExprRegex.Matches(text);
            if (matches.Count == 0) continue;

            // TextObjects com conteúdo misto (texto estático + campos) não são
            // formatáveis por campo — Format age sobre o valor completo já resolvido.
            // Nesses casos o dev deve usar funções de formato na própria expressão.
            if (!string.IsNullOrWhiteSpace(ExprRegex.Replace(text, ""))) continue;

            var problemasDecimal  = new List<string>();
            var problemasDateTime = new List<string>();

            foreach (Match match in matches)
            {
                if (match.Index > 0 && text[match.Index - 1] == '(') continue;

                var caminho = match.Groups[1].Value.Replace(" ", "");
                if (!schema.TryGetValue(caminho, out var dataType)) continue;

                var isDecimal = dataType == "System.Decimal" ||
                                (dataType.Contains("Decimal") && dataType.Contains("Nullable"));

                var isDateTime = dataType == "System.DateTime" ||
                                 (dataType.Contains("DateTime") && dataType.Contains("Nullable"));

                if (isDecimal)
                {
                    if (string.IsNullOrEmpty(format))
                        problemasDecimal.Add($"'{caminho}' sem Format");
                    else if (format == "Date" || format == "Time" || format == "Boolean")
                        problemasDecimal.Add($"'{caminho}' com Format=\"{format}\" incorreto");
                }

                if (isDateTime)
                {
                    if (string.IsNullOrEmpty(format))
                        problemasDateTime.Add($"'{caminho}' sem Format");
                    else if (format == "Currency" || format == "Number" || format == "Boolean")
                        problemasDateTime.Add($"'{caminho}' com Format=\"{format}\" incorreto");
                }
            }

            if (problemasDecimal.Count > 0)
            {
                var qtd = problemasDecimal.Count;
                results.Add(new RuleResult
                {
                    RuleCode = "Format-1",
                    Severity = Severity.Warning,
                    ComponentName = nome,
                    Message = "Formatação de Decimal incorreta ou ausente.",
                    Detail = qtd == 1
                        ? $"{problemasDecimal[0]} — Recomendado: Format=\"Currency\" Format.DecimalDigits=\"2\""
                        : $"{qtd} campos Decimal com problema de formatação: {string.Join("; ", problemasDecimal)}"
                });
            }

            if (problemasDateTime.Count > 0)
            {
                var qtd = problemasDateTime.Count;
                results.Add(new RuleResult
                {
                    RuleCode = "Format-1",
                    Severity = Severity.Warning,
                    ComponentName = nome,
                    Message = "Formatação de DateTime incorreta ou ausente.",
                    Detail = qtd == 1
                        ? $"{problemasDateTime[0]} — Recomendado: Format=\"Date\""
                        : $"{qtd} campos DateTime com problema de formatação: {string.Join("; ", problemasDateTime)}"
                });
            }
        }

        return results;
    }

    private static Dictionary<string, string> ConstruirMapaDeTipos(XDocument doc)
        => SchemaBuilder.BuildTypeMap(doc)
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
}
