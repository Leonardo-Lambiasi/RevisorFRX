using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public partial class Fix1Rule
{
    [GeneratedRegex(@"\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}", RegexOptions.Compiled)]
    private static partial Regex RegexCnpj();

    [GeneratedRegex(@"\b\d{3}\.\d{3}\.\d{3}-\d{2}\b", RegexOptions.Compiled)]
    private static partial Regex RegexCpf();

    [GeneratedRegex(@"\bCEP\s*:?\s*\d{5}-\d{3}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RegexCep();

    [GeneratedRegex(@"\(\d{2}\)\s*\d{4,5}-\d{4}", RegexOptions.Compiled)]
    private static partial Regex RegexTelefone();

    [GeneratedRegex(@"\b\d{2}/\d{2}/\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex RegexData();

    [GeneratedRegex(@"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}", RegexOptions.Compiled)]
    private static partial Regex RegexValor();

    [GeneratedRegex(@"\bAg[eê]ncia\s+\d+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RegexAgencia();

    // Cache do regex de ordinal — evita recompilação em batch com mesmo JSON
    private Regex? _cachedRegexOrdinal;
    private List<string>? _cachedPalavras;

    private Regex GetRegexOrdinal(Fix1Config config)
    {
        if (_cachedRegexOrdinal is not null &&
            _cachedPalavras is not null &&
            _cachedPalavras.SequenceEqual(config.PalavrasServentia))
            return _cachedRegexOrdinal;

        var keywords = string.Join("|", config.PalavrasServentia.Select(Regex.Escape));
        _cachedRegexOrdinal = new Regex(
            $@"\b\d+[oOºª°]\s+({keywords})\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _cachedPalavras = [..config.PalavrasServentia];
        return _cachedRegexOrdinal;
    }

    public List<RuleResult> Check(XDocument doc, Fix1Config config)
    {
        var results = new List<RuleResult>();
        results.AddRange(VerificarTextObjects(doc, config));
        results.AddRange(VerificarPictureObjects(doc, config));
        return results;
    }

    private List<RuleResult> VerificarTextObjects(XDocument doc, Fix1Config config)
    {
        var results = new List<RuleResult>();

        // Regex de ordinal: obtida do cache (recompila apenas se PalavrasServentia mudou)
        var regexOrdinal = config.Deteccao.OrdinalCartorio && config.PalavrasServentia.Count > 0
            ? GetRegexOrdinal(config)
            : null;

        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "TextObject"))
        {
            var text = el.Attribute("Text")?.Value;
            if (string.IsNullOrEmpty(text)) continue;

            // Expressão do schema: o campo principal vem do schema mesmo com texto misto
            if (text.Contains("[Dados.")) continue;

            // Whitelist: textos legais e labels conhecidos
            if (config.WhitelistTextos.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase)))
                continue;

            var name = el.Attribute("Name")?.Value ?? "TextObject";
            var det  = config.Deteccao;

            var m = det.Cnpj ? RegexCnpj().Match(text) : default;
            if (m is { Success: true })
                results.Add(Warn(name, "CNPJ fixo no layout — use [Dados.DadosDoCartorio.CNPJ]", m.Value));

            m = det.Cpf ? RegexCpf().Match(text) : default;
            if (m is { Success: true })
                results.Add(Warn(name, "CPF fixo no layout — use expressão do schema", m.Value));

            m = det.Cep ? RegexCep().Match(text) : default;
            if (m is { Success: true })
                results.Add(Warn(name, "CEP fixo no layout — use [Dados.DadosDoCartorio.Endereco.CEP]", m.Value));

            m = det.Telefone ? RegexTelefone().Match(text) : default;
            if (m is { Success: true })
                results.Add(Warn(name, "Telefone fixo no layout — use expressão do schema", m.Value));

            // DataLiteral: só flaga se não há nenhuma expressão no texto
            if (det.DataLiteral && !text.Contains('['))
            {
                m = RegexData().Match(text);
                if (m.Success)
                    results.Add(Warn(name, "Data fixa no layout — use expressão do schema", m.Value));
            }

            // ValorMonetario: texto longo provavelmente é boilerplate legal
            if (det.ValorMonetario && text.Length <= 120)
            {
                m = RegexValor().Match(text);
                if (m.Success)
                    results.Add(Warn(name, "Valor monetário fixo no layout — use expressão do schema", m.Value));
            }

            if (det.OrdinalCartorio && regexOrdinal != null)
            {
                m = regexOrdinal.Match(text);
                if (m.Success)
                    results.Add(Warn(name, "Nome de cartório fixo no layout — use [Dados.DadosDoCartorio.Nome]", m.Value));
            }

            m = det.AgenciaBancaria ? RegexAgencia().Match(text) : default;
            if (m is { Success: true })
                results.Add(Warn(name, "Agência bancária fixa no layout — use expressão do schema", m.Value));
        }

        return results;
    }

    private static List<RuleResult> VerificarPictureObjects(XDocument doc, Fix1Config config)
    {
        var results = new List<RuleResult>();
        if (!config.Deteccao.ImagemEmbutida) return results;

        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "PictureObject"))
        {
            var image = el.Attribute("Image")?.Value;
            if (string.IsNullOrEmpty(image)) continue;  // sem imagem embutida

            var dataColumn = el.Attribute("DataColumn")?.Value;
            if (!string.IsNullOrEmpty(dataColumn)) continue;  // vem do schema

            // Imagem gerada por código em runtime — não é hardcoded
            var hasEvent = el.Attributes()
                .Any(a => a.Name.LocalName.EndsWith("Event", StringComparison.Ordinal)
                       && !string.IsNullOrEmpty(a.Value));
            if (hasEvent) continue;

            var name = el.Attribute("Name")?.Value ?? "PictureObject";
            results.Add(new RuleResult
            {
                RuleCode      = "Fix-1",
                Severity      = Severity.Warning,
                ComponentName = name,
                Message       = "Imagem embutida no layout — pode ser logo/assinatura do cartório de origem",
                Detail        = $"PictureObject '{name}' tem imagem em base64 sem DataColumn"
            });
        }

        return results;
    }

    private static RuleResult Warn(string name, string message, string match) =>
        new()
        {
            RuleCode      = "Fix-1",
            Severity      = Severity.Warning,
            ComponentName = name,
            Message       = message,
            Detail        = match.Length > 80 ? match[..80] + "…" : match
        };
}
