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
            var text = texto.Attribute("Text")!.Value;

            // Ignorar TextObjects que contenham chamadas de função (FormatDateTime, IIF, etc.)
            if (text.Contains('(')) continue;

            var match = ExprRegex.Match(text);
            if (!match.Success) continue;

            var caminho = match.Groups[1].Value.Trim();
            if (!schema.TryGetValue(caminho, out var dataType)) continue;

            var format  = texto.Attribute("Format")?.Value ?? "";
            var pattern = texto.Attribute("Format.Pattern")?.Value ?? "";
            var nome    = texto.Attribute("Name")?.Value ?? "TextObject";

            var isDecimal = dataType == "System.Decimal" ||
                            (dataType.Contains("Decimal") && dataType.Contains("Nullable"));

            var isDateTime = dataType == "System.DateTime" ||
                             (dataType.Contains("DateTime") && dataType.Contains("Nullable"));

            if (isDecimal)
            {
                if (string.IsNullOrEmpty(format))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Format-1",
                        Severity = Severity.Warning,
                        ComponentName = nome,
                        Message = $"Campo Decimal '[Dados.{caminho}]' sem Format definido.",
                        Detail = "Recomendado: Format=\"Currency\" Format.DecimalDigits=\"2\""
                    });
                }
                else if (format == "Date" || format == "Time" || format == "Boolean")
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Format-1",
                        Severity = Severity.Warning,
                        ComponentName = nome,
                        Message = $"Campo Decimal '[Dados.{caminho}]' com Format=\"{format}\" incorreto.",
                        Detail = $"Format=\"{format}\" não é adequado para Decimal. " +
                                 "Recomendado: Format=\"Currency\""
                    });
                }
            }

            if (isDateTime)
            {
                if (string.IsNullOrEmpty(format))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Format-1",
                        Severity = Severity.Warning,
                        ComponentName = nome,
                        Message = $"Campo DateTime '[Dados.{caminho}]' sem Format definido.",
                        Detail = "Recomendado: Format=\"Date\" Format.Pattern=\"dd/MM/yyyy\""
                    });
                }
                else if (format == "Date" && string.IsNullOrEmpty(pattern))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Format-1",
                        Severity = Severity.Warning,
                        ComponentName = nome,
                        Message = $"Campo DateTime '[Dados.{caminho}]' tem Format=\"Date\" " +
                                  "mas Format.Pattern não definido.",
                        Detail = "O FastReport pode exibir em formato inesperado. " +
                                 "Recomendado: Format.Pattern=\"dd/MM/yyyy\""
                    });
                }
                else if (format == "Currency" || format == "Number" || format == "Boolean")
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Format-1",
                        Severity = Severity.Warning,
                        ComponentName = nome,
                        Message = $"Campo DateTime '[Dados.{caminho}]' com Format=\"{format}\" incorreto.",
                        Detail = $"Format=\"{format}\" não é adequado para DateTime. " +
                                 "Recomendado: Format=\"Date\" Format.Pattern=\"dd/MM/yyyy\""
                    });
                }
            }
        }

        return results;
    }

    private static Dictionary<string, string> ConstruirMapaDeTipos(XDocument doc)
    {
        var mapa = new Dictionary<string, string>(StringComparer.Ordinal);

        var dictionary = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Dictionary");
        if (dictionary == null) return mapa;

        // Mesmo padrão do Expr1Rule: iniciar dos filhos de cada fonte raiz para
        // que as chaves não incluam o prefixo "Dados." da expressão.
        foreach (var topSource in dictionary.Elements())
            ConstruirRecursivo(topSource, "", mapa);

        return mapa;
    }

    private static void ConstruirRecursivo(XElement elemento,
        string caminhoAtual, Dictionary<string, string> mapa)
    {
        foreach (var filho in elemento.Elements())
        {
            // BusinessObjectDataSource usa Alias nas expressões [Dados.X.Y];
            // Column não tem Alias — cai para Name naturalmente.
            var nome = filho.Attribute("Alias")?.Value;
            if (string.IsNullOrEmpty(nome))
                nome = filho.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(nome)) continue;

            var caminho = string.IsNullOrEmpty(caminhoAtual)
                ? nome
                : $"{caminhoAtual}.{nome}";

            var dataType = filho.Attribute("DataType")?.Value ?? "";

            if (filho.Name.LocalName == "Column" &&
                !string.IsNullOrEmpty(dataType) &&
                dataType != "null")
            {
                mapa[caminho] = dataType;
            }

            ConstruirRecursivo(filho, caminho, mapa);
        }
    }
}
