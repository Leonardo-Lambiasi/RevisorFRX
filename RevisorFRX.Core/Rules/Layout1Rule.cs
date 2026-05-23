using System.Globalization;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Layout1Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var growingElements = doc.Descendants()
            .Where(e => e.Attribute("CanGrow")?.Value.ToLowerInvariant() == "true");

        foreach (var element in growingElements)
        {
            var topStr = element.Attribute("Top")?.Value;
            if (!float.TryParse(topStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var elementTop))
                continue;

            var band = element.Parent;
            if (band == null) continue;

            var siblings = band.Elements()
                .Where(s => s != element);

            foreach (var sibling in siblings)
            {
                var sibTopStr = sibling.Attribute("Top")?.Value;
                if (!float.TryParse(sibTopStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var sibTop))
                    continue;

                if (sibTop > elementTop && sibling.Attribute("ShiftMode")?.Value != "Shift")
                {
                    var componentName = element.Attribute("Name")?.Value ?? element.Name.LocalName;
                    var siblingName = sibling.Attribute("Name")?.Value ?? sibling.Name.LocalName;
                    results.Add(new RuleResult
                    {
                        RuleCode = "Layout-1",
                        Severity = Severity.Warning,
                        ComponentName = componentName,
                        Message = $"'{componentName}' tem CanGrow=true mas '{siblingName}' abaixo não tem ShiftMode=Shift.",
                        Detail = $"Top={elementTop}; '{siblingName}' Top={sibTop}, ShiftMode={sibling.Attribute("ShiftMode")?.Value ?? "(ausente)"}"
                    });
                }
            }
        }

        return results;
    }
}
