using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Code4Rule
{
    private static readonly HashSet<string> PropriedadesVigiadas =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Text",
            "Visible"
        };

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var scriptText = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ScriptText")?.Value;
        if (string.IsNullOrWhiteSpace(scriptText)) return results;

        var root = CSharpSyntaxTree.ParseText(scriptText).GetCompilationUnitRoot();

        var metodosAfterData = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText
                .EndsWith("_AfterData", StringComparison.OrdinalIgnoreCase));

        foreach (var metodo in metodosAfterData)
        {
            var nomeMetodo = metodo.Identifier.ValueText;
            var componenteEsperado = nomeMetodo
                .Substring(0, nomeMetodo.LastIndexOf("_AfterData",
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(componenteEsperado)) continue;

            var atribuicoes = metodo.Body?
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Where(a => a.Left is MemberAccessExpressionSyntax)
                ?? Enumerable.Empty<AssignmentExpressionSyntax>();

            var reportados = new HashSet<(string, string)>();

            foreach (var atribuicao in atribuicoes)
            {
                var memberAccess = (MemberAccessExpressionSyntax)atribuicao.Left;
                var propriedade = memberAccess.Name.Identifier.ValueText;
                var componente = memberAccess.Expression.ToString();

                if (!PropriedadesVigiadas.Contains(propriedade)) continue;

                if (string.Equals(componente, componenteEsperado,
                    StringComparison.OrdinalIgnoreCase)) continue;

                // ignorar "this.AlgumaCoisa" e expressões complexas
                if (componente.Contains(".") || componente.Contains("("))
                    continue;

                if (!reportados.Add((componente, propriedade))) continue;

                results.Add(new RuleResult
                {
                    RuleCode = "Code-4",
                    Severity = Severity.Info,
                    ComponentName = nomeMetodo,
                    Message = $"Método '{nomeMetodo}' modifica '{componente}.{propriedade}' " +
                              $"mas deveria modificar apenas '{componenteEsperado}.{propriedade}'.",
                    Detail = $"Componente esperado: {componenteEsperado} | " +
                             $"Componente modificado: {componente} | " +
                             $"Propriedade: {propriedade}"
                });
            }
        }

        return results;
    }
}
