using System.Xml.Linq;
using RevisorFRX.Core.Models;
using RevisorFRX.Core.Rules;

namespace RevisorFRX.Core.Services;

public class FrxAnalyzer
{
    public List<RuleResult> Analyze(string frxContent)
    {
        var results = new List<RuleResult>();
        var doc = XDocument.Parse(frxContent);

        results.AddRange(new Ref3Rule().Check(doc));
        results.AddRange(new Ref2Rule().Check(doc));
        results.AddRange(new Ref1Rule().Check(doc));
        results.AddRange(new Code2Rule().Check(doc));
        results.AddRange(new Code3Rule().Check(doc));
        results.AddRange(new Format1Rule().Check(doc));
        results.AddRange(new Expr1Rule().Check(doc));
        results.AddRange(new Code4Rule().Check(doc));

        return results.OrderBy(r => r.Severity).ToList();
    }
}
