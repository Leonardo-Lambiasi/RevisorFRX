using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public partial class Code4Rule
{
    private static readonly Regex CnpjContextRegex = CnpjPattern();
    private static readonly Regex DigitOnlyRegex = DigitsOnlyPattern();
    private static readonly Regex CnpjMaskRegex = CnpjMaskPattern();
    private static readonly Regex LengthCheckRegex = LengthCheckPattern();

    [GeneratedRegex(@"c(?:npj|gc|pf)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CnpjPattern();

    [GeneratedRegex(@"\\d\{14\}|\[0-9\]\{14\}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DigitsOnlyPattern();

    [GeneratedRegex(@"##\.###\.###/####-##", RegexOptions.Compiled)]
    private static partial Regex CnpjMaskPattern();

    [GeneratedRegex(@"\.(Length|Count)\s*([=!<>]=?)\s*14\b", RegexOptions.Compiled)]
    private static partial Regex LengthCheckPattern();

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        VerificarScriptText(doc, results);
        VerificarTextObjects(doc, results);
        VerificarDictionary(doc, results);

        return results;
    }

    private static void VerificarScriptText(XDocument doc, List<RuleResult> results)
    {
        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;

        if (string.IsNullOrWhiteSpace(scriptText)) return;

        if (!CnpjContextRegex.IsMatch(scriptText)) return;

        if (DigitOnlyRegex.IsMatch(scriptText))
        {
            results.Add(new RuleResult
            {
                RuleCode = "Code-4",
                Severity = Severity.Warning,
                ComponentName = "ScriptText",
                Message = "CNPJ passará a aceitar caracteres alfanuméricos.",
                Detail = "Expressão regular com \\d{14} ou [0-9]{14} encontrada no ScriptText — " +
                         "vai rejeitar CNPJs alfanuméricos. Use validação por caracteres válidos " +
                         "em vez de apenas dígitos."
            });
        }

        if (LengthCheckRegex.IsMatch(scriptText))
        {
            results.Add(new RuleResult
            {
                RuleCode = "Code-4",
                Severity = Severity.Warning,
                ComponentName = "ScriptText",
                Message = "Verificação de Length/Count == 14 em CNPJ vai falhar com caracteres alfanuméricos.",
                Detail = "O CNPJ pode conter letras — validações que contam exatamente 14 caracteres " +
                         "precisam ser revisadas para aceitar caracteres alfanuméricos."
            });
        }
    }

    private static void VerificarTextObjects(XDocument doc, List<RuleResult> results)
    {
        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject");

        foreach (var texto in textos)
        {
            var text = texto.Attribute("Text")?.Value;
            if (string.IsNullOrEmpty(text)) continue;

            if (!CnpjContextRegex.IsMatch(text)) continue;

            var nome = texto.Attribute("Name")?.Value ?? "TextObject";
            var format = texto.Attribute("Format")?.Value;

            if (CnpjMaskRegex.IsMatch(text) || (format != null && CnpjMaskRegex.IsMatch(format)))
            {
                results.Add(new RuleResult
                {
                    RuleCode = "Code-4",
                    Severity = Severity.Warning,
                    ComponentName = nome,
                    Message = $"Máscara de CNPJ '{format ?? text}' não suporta caracteres alfanuméricos.",
                    Detail = "A máscara ##.###.###/####-## assume apenas dígitos. " +
                             "Revise a formatação para aceitar caracteres alfanuméricos."
                });
            }
        }
    }

    private static void VerificarDictionary(XDocument doc, List<RuleResult> results)
    {
        var camposCnpj = doc.Descendants()
            .Where(e => e.Name.LocalName == "Column")
            .Where(e => CnpjContextRegex.IsMatch(e.Attribute("Name")?.Value ?? ""));

        foreach (var campo in camposCnpj)
        {
            var dataType = campo.Attribute("DataType")?.Value ?? "";
            if (dataType.Contains("Int") || dataType.Contains("Long") || dataType.Contains("Decimal"))
            {
                var nome = campo.Attribute("Name")!.Value;
                var caminho = BuildCaminho(campo);
                results.Add(new RuleResult
                {
                    RuleCode = "Code-4",
                    Severity = Severity.Warning,
                    ComponentName = nome,
                    Message = $"Campo '{caminho}' é numérico ({dataType}) — CNPJ alfanumérico não será aceito.",
                    Detail = "O campo deve ser String para suportar o novo formato com caracteres alfanuméricos."
                });
            }
        }
    }

    private static string BuildCaminho(XElement column)
    {
        var parts = new List<string>();
        var current = column;
        var depth = 0;
        while (current != null && depth < 100)
        {
            var nome = current.Attribute("Alias")?.Value
                    ?? current.Attribute("Name")?.Value
                    ?? "";
            if (!string.IsNullOrEmpty(nome))
                parts.Insert(0, nome);
            current = current.Parent;
            depth++;
        }
        return string.Join(".", parts);
    }
}
