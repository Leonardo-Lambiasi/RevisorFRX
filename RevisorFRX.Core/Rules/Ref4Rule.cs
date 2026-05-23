using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref4Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var hierarquia = ConstruirHierarquia(doc);

        var subreports = doc.Descendants()
            .Where(e => e.Name.LocalName == "SubreportObject"
                     && e.Attribute("PrintOnParent")?.Value == "true");

        foreach (var sub in subreports)
        {
            var paginaFilhaNome = sub.Attribute("ReportPage")?.Value;
            if (string.IsNullOrEmpty(paginaFilhaNome)) continue;

            var paginaFilha = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "ReportPage"
                                  && e.Attribute("Name")?.Value == paginaFilhaNome);

            if (paginaFilha == null)
            {
                results.Add(new RuleResult
                {
                    RuleCode = "Ref-4",
                    Severity = Severity.Error,
                    ComponentName = sub.Attribute("Name")?.Value ?? "SubreportObject",
                    Message = $"ReportPage '{paginaFilhaNome}' referenciada pelo subrelatório não existe no relatório.",
                    Detail = $"ReportPage=\"{paginaFilhaNome}\""
                });
                continue;
            }

            // Verificar todas as DataBands da página filha, não só a primeira
            var dataSourcesFilhos = paginaFilha
                .Descendants()
                .Where(e => e.Name.LocalName == "DataBand")
                .Select(e => e.Attribute("DataSource")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // SubreportObject pode estar em DataBand, GroupHeader ou PageHeader —
            // subir até encontrar a DataBand pai; null se não existir
            var bandaPai = sub.Ancestors()
                .FirstOrDefault(a => a.Name.LocalName == "DataBand");
            var dataSourcePai = bandaPai?.Attribute("DataSource")?.Value;

            if (string.IsNullOrEmpty(dataSourcePai)) continue;

            foreach (var dsFilho in dataSourcesFilhos)
            {
                if (string.IsNullOrEmpty(dsFilho)) continue;

                if (!EhDescendente(dsFilho!, dataSourcePai, hierarquia))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Ref-4",
                        Severity = Severity.Error,
                        ComponentName = sub.Attribute("Name")?.Value ?? "SubreportObject",
                        Message = $"PrintOnParent=true mas '{dsFilho}' não é descendente de '{dataSourcePai}' no schema.",
                        Detail = $"Página filha: {paginaFilhaNome} | DataSource pai: {dataSourcePai} | DataSource filho: {dsFilho}"
                    });
                }
            }
        }

        return results;
    }

    private static Dictionary<string, string> ConstruirHierarquia(XDocument doc)
    {
        var mapa = new Dictionary<string, string>(StringComparer.Ordinal);

        var fontes = doc.Descendants()
            .Where(e => e.Name.LocalName == "BusinessObjectDataSource"
                     || e.Name.LocalName == "TableDataSource");

        foreach (var fonte in fontes)
        {
            var nome = fonte.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(nome)) continue;

            // Subir pelo XML até encontrar outro DataSource (pai direto)
            var pai = fonte.Ancestors()
                .FirstOrDefault(a => a.Name.LocalName == "BusinessObjectDataSource"
                                  || a.Name.LocalName == "TableDataSource");
            var nomePai = pai?.Attribute("Name")?.Value;

            if (!string.IsNullOrEmpty(nomePai))
                mapa[nome] = nomePai;
        }

        return mapa;
    }

    private static bool EhDescendente(string filho, string pai,
        Dictionary<string, string> hierarquia, int maxDepth = 20)
    {
        var atual = filho;
        for (var i = 0; i < maxDepth; i++)
        {
            if (!hierarquia.TryGetValue(atual, out var proximoPai)) break;
            if (proximoPai == pai) return true;
            atual = proximoPai;
        }
        return false;
    }
}
