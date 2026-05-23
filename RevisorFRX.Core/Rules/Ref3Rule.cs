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
            var tree = CSharpSyntaxTree.ParseText(scriptText);
            var root = tree.GetCompilationUnitRoot();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                definedMethods.Add(method.Identifier.Text);
        }

        var eventAttrs = doc.Descendants()
            .SelectMany(e => e.Attributes())
            .Where(a => a.Name.LocalName.EndsWith("Event") && !string.IsNullOrEmpty(a.Value));

        foreach (var attr in eventAttrs)
        {
            var methodName = attr.Value;
            if (!definedMethods.Contains(methodName))
            {
                var componentName = ((XElement)attr.Parent!).Attribute("Name")?.Value ?? attr.Parent!.Name.LocalName;
                results.Add(new RuleResult
                {
                    RuleCode = "Ref-3",
                    Severity = Severity.Error,
                    ComponentName = componentName,
                    Message = $"Evento '{attr.Name.LocalName}' referencia método '{methodName}' não encontrado no ScriptText.",
                    Detail = $"Atributo: {attr.Name.LocalName} = \"{methodName}\""
                });
            }
        }

        return results;
    }
}
