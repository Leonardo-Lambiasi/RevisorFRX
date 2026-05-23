using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref1Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var allNames = new HashSet<string>(
            doc.Descendants()
               .Select(e => e.Attribute("Name")?.Value)
               .Where(v => !string.IsNullOrEmpty(v))!,
            StringComparer.Ordinal);

        var elementsWithMaster = doc.Descendants()
            .Where(e => e.Attribute("MasterComponent") != null);

        foreach (var element in elementsWithMaster)
        {
            var masterValue = element.Attribute("MasterComponent")!.Value;
            if (!string.IsNullOrEmpty(masterValue) && !allNames.Contains(masterValue))
            {
                var componentName = element.Attribute("Name")?.Value ?? element.Name.LocalName;
                results.Add(new RuleResult
                {
                    RuleCode = "Ref-1",
                    Severity = Severity.Error,
                    ComponentName = componentName,
                    Message = $"MasterComponent '{masterValue}' não encontrado no relatório.",
                    Detail = $"Elemento: {element.Name.LocalName}, MasterComponent=\"{masterValue}\""
                });
            }
        }

        return results;
    }
}
