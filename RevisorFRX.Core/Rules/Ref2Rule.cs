using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref2Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var declarados = doc.Descendants()
            .Where(e => e.Name.LocalName is "BusinessObjectDataSource" or "TableDataSource"
                     or "CsvDataSource" or "ViewDataSource" or "JsonDataSource")
            .Where(e => e.Attribute("Enabled")?.Value != "false")
            .Select(e => e.Attribute("Name")?.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToHashSet(StringComparer.Ordinal)!;

        // Ignorar elementos dentro do Dictionary e os próprios tipos de fonte de dados —
        // apenas componentes visuais (DataBand, etc.) devem ser verificados.
        var elementos = doc.Descendants()
            .Where(e => e.Attribute("DataSource") != null)
            .Where(e => e.Name.LocalName is not ("BusinessObjectDataSource" or "TableDataSource"
                         or "ViewDataSource" or "JsonDataSource" or "CsvDataSource" or "Column"))
            .Where(e => !e.Ancestors().Any(a => a.Name.LocalName == "Dictionary"));

        foreach (var el in elementos)
        {
            var valor = el.Attribute("DataSource")!.Value;
            if (string.IsNullOrEmpty(valor)) continue;
            if (!declarados.Contains(valor))
            {
                results.Add(new RuleResult
                {
                    RuleCode = "Ref-2",
                    Severity = Severity.Error,
                    ComponentName = el.Attribute("Name")?.Value ?? el.Name.LocalName,
                    Message = $"DataSource '{valor}' não encontrado no relatório.",
                    Detail = $"Atributo: DataSource = \"{valor}\""
                });
            }
        }

        return results;
    }
}
