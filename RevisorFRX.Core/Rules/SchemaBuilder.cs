using System.Xml.Linq;

namespace RevisorFRX.Core.Rules;

internal static class SchemaBuilder
{
    // Returns all Column paths → DataType.
    // Columns without DataType or with DataType="null" are stored with empty string.
    internal static Dictionary<string, string> BuildTypeMap(XDocument doc)
    {
        var mapa = new Dictionary<string, string>(StringComparer.Ordinal);

        var dictionary = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Dictionary");
        if (dictionary == null) return mapa;

        foreach (var topSource in dictionary.Elements())
            PopulateRecursive(topSource, "", mapa);

        return mapa;
    }

    private static void PopulateRecursive(XElement elemento, string caminhoAtual,
        Dictionary<string, string> mapa)
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
            {
                var dataType = filho.Attribute("DataType")?.Value ?? "";
                mapa[caminho] = dataType == "null" ? "" : dataType;
            }

            PopulateRecursive(filho, caminho, mapa);
        }
    }
}
