using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref12Rule
{
    private static readonly HashSet<string> BandasQueCrescem =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DataBand",
            "GroupHeader",
            "ChildBand"
        };

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject");

        foreach (var texto in textos)
        {
            var canGrow = texto.Attribute("CanGrow")?.Value;
            var canShrink = texto.Attribute("CanShrink")?.Value;

            bool textoCresce = string.Equals(canGrow, "true", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(canShrink, "true", StringComparison.OrdinalIgnoreCase);

            if (!textoCresce) continue;

            var nome = texto.Attribute("Name")?.Value ?? "(sem nome)";

            foreach (var prop in new[] { "CanGrow", "CanShrink" })
            {
                var valorTexto = texto.Attribute(prop)?.Value;
                if (!string.Equals(valorTexto, "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Sobe na árvore até achar a banda mais próxima
                var banda = texto.Ancestors()
                    .FirstOrDefault(a => BandasQueCrescem.Contains(a.Name.LocalName));

                if (banda == null) continue;

                var valorBanda = banda.Attribute(prop)?.Value;
                if (string.Equals(valorBanda, "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                var nomeBanda = banda.Attribute("Name")?.Value ?? banda.Name.LocalName;

                results.Add(new RuleResult
                {
                    RuleCode = "Ref-12",
                    Severity = Severity.Warning,
                    ComponentName = nome,
                    Message = $"TextObject tem {prop}=\"true\" mas a banda '{nomeBanda}' não.",
                    Detail = $"O TextObject '{nome}' pode crescer/encolher, mas a banda '{nomeBanda}' " +
                             $"não acompanha — o texto pode sobrepor componentes abaixo."
                });
            }
        }

        return results;
    }
}
