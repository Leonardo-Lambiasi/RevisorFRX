using System.Xml.Linq;
using RevisorFRX.Core.Models;
using RevisorFRX.Core.Rules;

namespace RevisorFRX.Core.Services;

public class FrxAnalyzer
{
    private static readonly Ref2Rule Ref2 = new();
    private static readonly Ref1Rule Ref1 = new();
    private static readonly Code2Rule Code2 = new();
    private static readonly Code3Rule Code3 = new();
    private static readonly Format1Rule Format1 = new();
    private static readonly Expr1Rule Expr1 = new();
    private static readonly Expr2Rule Expr2 = new();
    private static readonly Ref10Rule Ref10 = new();
    private static readonly Format6Rule Format6 = new();
    private static readonly Ref12Rule Ref12 = new();
    private static readonly Format7Rule Format7 = new();
    private static readonly Code4Rule Code4 = new();

    public List<RuleResult> Analyze(string frxContent, RuleConfig? config = null)
    {
        config ??= RuleConfig.AllEnabled();

        var results = new List<RuleResult>();
        var doc = XDocument.Parse(frxContent);

        if (config.IsEnabled("Ref-2"))
            results.AddRange(Ref2.Check(doc));
        if (config.IsEnabled("Ref-1"))
            results.AddRange(Ref1.Check(doc));
        if (config.IsEnabled("Code-2"))
            results.AddRange(Code2.Check(doc));
        if (config.IsEnabled("Code-3"))
            results.AddRange(Code3.Check(doc));
        if (config.IsEnabled("Format-1"))
            results.AddRange(Format1.Check(doc));
        if (config.IsEnabled("Expr-1"))
            results.AddRange(Expr1.Check(doc));
        if (config.IsEnabled("Expr-2"))
            results.AddRange(Expr2.Check(doc));
        if (config.IsEnabled("Ref-10"))
            results.AddRange(Ref10.Check(doc));
        if (config.IsEnabled("Format-6"))
            results.AddRange(Format6.Check(doc));
        if (config.IsEnabled("Ref-12"))
            results.AddRange(Ref12.Check(doc));
        if (config.IsEnabled("Format-7"))
            results.AddRange(Format7.Check(doc));
        if (config.IsEnabled("Code-4"))
            results.AddRange(Code4.Check(doc));

        return results.OrderBy(r => r.Severity).ToList();
    }
}
