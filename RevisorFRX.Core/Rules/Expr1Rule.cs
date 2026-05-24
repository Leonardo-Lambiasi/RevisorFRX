using System.Text.RegularExpressions;
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class Expr1Rule
{
    // Captura [Dados.Algo] ou [Dados.Algo.Sub.Campo] mas não [Dados.Func(...)]
    // O grupo 1 contém o caminho sem "Dados."
    private static readonly Regex ExprRegex =
        new(@"\[Dados\.([^\]\[()]+)\]", RegexOptions.Compiled);

    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();

        var schema = ConstruirSchema(doc);
        if (schema.Count == 0) return results;

        var textos = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject"
                     && e.Attribute("Text") != null);

        foreach (var texto in textos)
        {
            var textValue = texto.Attribute("Text")!.Value;
            var matches = ExprRegex.Matches(textValue);

            foreach (Match match in matches)
            {
                // Expressões matemáticas usam [[Dados.X]+[Dados.Y]] — o match imediatamente
                // após o '[' externo está dentro de uma operação composta; ignorar.
                if (match.Index > 0 && textValue[match.Index - 1] == '[') continue;

                var caminho = match.Groups[1].Value.Trim();

                // Filtro defensivo extra: se sobrou parêntese ou vírgula é função
                if (caminho.Contains('(') || caminho.Contains(',')) continue;

                if (!schema.Contains(caminho))
                {
                    results.Add(new RuleResult
                    {
                        RuleCode = "Expr-1",
                        Severity = Severity.Warning,
                        ComponentName = texto.Attribute("Name")?.Value ?? "TextObject",
                        Message = $"Campo '[Dados.{caminho}]' não encontrado no schema do Dictionary.",
                        Detail = $"Expressão: {match.Value}"
                    });
                }
            }
        }

        return results;
    }

    private static HashSet<string> ConstruirSchema(XDocument doc)
    {
        var schema = new HashSet<string>(StringComparer.Ordinal);

        var dictionary = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Dictionary");
        if (dictionary == null) return schema;

        // Expressões usam [Dados.X.Y] — "Dados" é o nome do BusinessObjectDataSource
        // raiz. Iniciamos a partir dos filhos de cada fonte raiz para que as chaves
        // do schema não incluam o prefixo "Dados." e correspondam ao que a regex captura.
        foreach (var topSource in dictionary.Elements())
            ConstruirSchemaRecursivo(topSource, "", schema);

        return schema;
    }

    private static void ConstruirSchemaRecursivo(XElement elemento,
        string caminhoAtual, HashSet<string> schema)
    {
        foreach (var filho in elemento.Elements())
        {
            // BusinessObjectDataSource usa Alias nas expressões [Dados.X.Y];
            // Column não tem Alias — cai para Name naturalmente.
            var nome = filho.Attribute("Alias")?.Value;
            if (string.IsNullOrEmpty(nome))
                nome = filho.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(nome)) continue;

            var caminho = string.IsNullOrEmpty(caminhoAtual)
                ? nome
                : $"{caminhoAtual}.{nome}";

            if (filho.Name.LocalName == "Column")
                schema.Add(caminho);

            ConstruirSchemaRecursivo(filho, caminho, schema);
        }
    }
}
