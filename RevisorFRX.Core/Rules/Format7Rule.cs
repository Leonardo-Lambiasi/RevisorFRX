using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Format7Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var barcodes = doc.Descendants()
            .Where(e => e.Name.LocalName == "BarcodeObject");

        foreach (var barcode in barcodes)
        {
            var nome = barcode.Attribute("Name")?.Value ?? "(sem nome)";
            var calcCheckSum = barcode.Attribute("Barcode.CalcCheckSum")?.Value;

            if (string.IsNullOrEmpty(calcCheckSum))
                continue;

            if (string.Equals(calcCheckSum, "false", StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new RuleResult
            {
                RuleCode = "Format-7",
                Severity = Severity.Warning,
                ComponentName = nome,
                Message = $"Barcode com Barcode.CalcCheckSum='{calcCheckSum}' — deve ser false.",
                Detail = "Checksum habilitado pode gerar códigos de barras inválidos para leitura. " +
                         "Defina o atributo Barcode.CalcCheckSum=\"false\"."
            });
        }

        return results;
    }
}
