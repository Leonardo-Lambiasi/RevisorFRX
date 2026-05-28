using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Format6Rule
{
    private static readonly Regex HtmlTagRegex = new(
        @"</?(b|i|u|font|span|div|p|br|strong|em|a|ul|ol|li|table|tr|td|th|h[1-6]|img|style|class)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject")
            .Select(e => new
            {
                Elemento = e,
                Nome = e.Attribute("Name")?.Value ?? "(sem nome)",
                Text = e.Attribute("Text")?.Value,
                HtmlTags = e.Attribute("TextRenderType")?.Value
            })
            .Where(e => e.Text != null && HtmlTagRegex.IsMatch(e.Text));

        foreach (var t in textos)
        {
            if (string.Equals(t.HtmlTags, "HtmlTags", StringComparison.OrdinalIgnoreCase))
                continue;

            var txt = t.Text!;
            var snippet = txt.Length > 120 ? txt[..120] + "..." : txt;

            results.Add(new RuleResult
            {
                RuleCode = "Format-6",
                Severity = Severity.Warning,
                ComponentName = t.Nome,
                Message = "Texto contém tags HTML mas TextRenderType não está definido como HtmlTags.",
                Detail = $"Text: \"{snippet}\""
            });
        }

        return results;
    }
}
