using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref10Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject")
            .Select(e => new
            {
                Elemento = e,
                Nome = e.Attribute("Name")?.Value ?? "(sem nome)",
                Text = e.Attribute("Text")?.Value
            })
            .Where(e => e.Text != null);

        foreach (var t in textos)
        {
            var text = t.Text!;
            int abre = text.Count(c => c == '[');
            int fecha = text.Count(c => c == ']');

            if (abre != fecha)
            {
                results.Add(new RuleResult
                {
                    RuleCode = "Ref-10",
                    Severity = Severity.Warning,
                    ComponentName = t.Nome,
                    Message = "Colchetes desbalanceados no atributo Text.",
                    Detail = $"Text: \"{text}\" | Abertos: {abre} | Fechados: {fecha}"
                });
            }
        }

        return results;
    }
}
