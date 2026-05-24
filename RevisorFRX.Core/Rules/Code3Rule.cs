using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Code3Rule
{
    private static readonly string[] MetodosObrigatorios =
    {
        "AplicarMascaraDeDocumento",
        "ExtrairCaracteresNumericos",
        "AplicarMascaraDeCNPJ",
        "AplicarMascaraDeCPF"
    };

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;

        if (string.IsNullOrWhiteSpace(scriptText))
        {
            results.Add(new RuleResult
            {
                RuleCode = "Code-3",
                Severity = Severity.Error,
                ComponentName = "ScriptText",
                Message = "ScriptText ausente ou vazio.",
                Detail = "O relatório deve conter os métodos utilitários do template padrão."
            });
            return results;
        }

        var root = CSharpSyntaxTree.ParseText(scriptText).GetCompilationUnitRoot();
        var metodosDeclarados = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.ValueText)
            .ToHashSet();

        foreach (var metodo in MetodosObrigatorios)
        {
            if (!metodosDeclarados.Contains(metodo))
            {
                results.Add(new RuleResult
                {
                    RuleCode = "Code-3",
                    Severity = Severity.Error,
                    ComponentName = "ScriptText",
                    Message = $"Método obrigatório ausente: '{metodo}'.",
                    Detail = "Método faz parte do template padrão de relatórios. Copie-o do template base."
                });
            }
        }

        return results;
    }
}
