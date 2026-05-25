using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Ref3Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value ?? "";

        var definedMethods = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(scriptText))
        {
            var root = CSharpSyntaxTree.ParseText(scriptText).GetCompilationUnitRoot();
            definedMethods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Select(m => m.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);
        }

        var eventosInvalidos = doc.Descendants()
            .SelectMany(e => e.Attributes()
                .Where(a => a.Name.LocalName.EndsWith("Event") && !string.IsNullOrEmpty(a.Value))
                .Select(a => new
                {
                    ComponentName = e.Attribute("Name")?.Value ?? e.Name.LocalName,
                    EventAttr     = a.Name.LocalName,
                    Metodo        = a.Value
                }))
            .Where(e => !definedMethods.Contains(e.Metodo))
            .ToList();

        var contagemPorMetodo = eventosInvalidos
            .GroupBy(e => e.Metodo, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var evento in eventosInvalidos)
        {
            var total = contagemPorMetodo[evento.Metodo];
            var detail = total > 1
                ? $"Atributo: {evento.EventAttr} = \"{evento.Metodo}\" | {total} componentes referenciam este método ausente"
                : $"Atributo: {evento.EventAttr} = \"{evento.Metodo}\"";

            results.Add(new RuleResult
            {
                RuleCode      = "Ref-3",
                Severity      = Severity.Info,
                ComponentName = evento.ComponentName,
                Message       = $"Evento '{evento.EventAttr}' referencia método '{evento.Metodo}' não encontrado no ScriptText.",
                Detail        = detail
            });
        }

        return results;
    }
}
