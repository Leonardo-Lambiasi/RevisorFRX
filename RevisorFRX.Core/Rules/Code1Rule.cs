using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Code1Rule
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;

        if (string.IsNullOrWhiteSpace(scriptText))
            return results;

        var root = CSharpSyntaxTree.ParseText(scriptText).GetCompilationUnitRoot();

        var emptyCatches = root.DescendantNodes()
            .OfType<CatchClauseSyntax>()
            .Where(c => c.Block.Statements.Count == 0);

        foreach (var catchClause in emptyCatches)
        {
            var lineSpan = catchClause.GetLocation().GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1;
            results.Add(new RuleResult
            {
                RuleCode = "Code-1",
                Severity = Severity.Warning,
                ComponentName = "ScriptText",
                Message = "Bloco catch vazio detectado — exceções estão sendo silenciadas.",
                Detail = $"Linha {line} do ScriptText"
            });
        }

        return results;
    }
}
