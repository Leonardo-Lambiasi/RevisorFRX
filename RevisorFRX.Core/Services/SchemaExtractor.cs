using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Services;

public static class SchemaExtractor
{
    public static List<SchemaField> Extract(XDocument doc, SchemaExtractorOptions? options = null)
    {
        options ??= SchemaExtractorOptions.Default;
        var fields = new List<SchemaField>();

        var dictionary = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Dictionary");
        if (dictionary == null) return fields;

        foreach (var topSource in dictionary.Elements())
            ExtractRecursive(topSource, "", rootLevel: true, fields, options);

        EnrichWithUsage(doc, fields);
        return fields;
    }

    private static void ExtractRecursive(
        XElement element, string entityPath, bool rootLevel, List<SchemaField> fields,
        SchemaExtractorOptions options)
    {
        // Mirrors SchemaBuilder: prefer Alias, fallback to Name
        var alias = element.Attribute("Alias")?.Value;
        if (string.IsNullOrEmpty(alias))
            alias = element.Attribute("Name")?.Value;
        if (string.IsNullOrEmpty(alias)) return;

        if (element.Name.LocalName == "Column")
        {
            var columnPath = string.IsNullOrEmpty(entityPath) ? alias : $"{entityPath}.{alias}";
            if (!DeveFiltrar(columnPath, options))
            {
                var dataType = element.Attribute("DataType")?.Value ?? "";
                fields.Add(new SchemaField
                {
                    EntityAlias = entityPath,
                    FieldName = alias,
                    DataType = dataType,
                    UsedInComponents = []
                });
            }
            // Mirrors SchemaBuilder: continue into children so nested columns
            // (e.g. Column DataType="null" with sub-columns) are also extracted.
            foreach (var child in element.Elements())
                ExtractRecursive(child, columnPath, rootLevel: false, fields, options);
            return;
        }

        // Root-level sources (e.g. the "Dados" wrapper) are excluded from EntityAlias
        var newPath = rootLevel
            ? ""
            : string.IsNullOrEmpty(entityPath) ? alias : $"{entityPath}.{alias}";

        foreach (var child in element.Elements())
            ExtractRecursive(child, newPath, rootLevel: false, fields, options);
    }

    private static bool DeveFiltrar(string entityPath, SchemaExtractorOptions options)
    {
        // Filtro 1: profundidade
        // "A" = 1, "A.B" = 2, "A.B.C" = 3, "A.B.C.D" = 4
        if (options.MaxDepth < int.MaxValue)
        {
            var profundidade = entityPath.Count(c => c == '.') + 1;
            if (profundidade > options.MaxDepth)
                return true;
        }

        // Filtro 2: segmentos excluídos — verifica segmento exato, não substring
        // "ValidationResult" NÃO filtra "ValidationResultCode"
        var partes = entityPath.Split('.');
        foreach (var segmento in options.ExcludedSegments)
        {
            if (partes.Any(p => p.Equals(segmento, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static void EnrichWithUsage(XDocument doc, List<SchemaField> fields)
    {
        var textObjects = doc.Descendants()
            .Where(e => e.Name.LocalName == "TextObject")
            .ToList();

        foreach (var field in fields)
        {
            var pattern = string.IsNullOrEmpty(field.EntityAlias)
                ? $"Dados.{field.FieldName}"
                : $"Dados.{field.EntityAlias}.{field.FieldName}";

            foreach (var textObj in textObjects)
            {
                var text = textObj.Attribute("Text")?.Value;
                if (text == null) continue;

                // Exact boundary check: the pattern must be followed by ']' (or '.' for
                // entity-level fields that are parent paths of sub-columns). Using plain
                // Contains would produce false positives when a field name is a prefix of
                // another (e.g. "Aceite" matching "[Dados.X.AceiteDoDocumento]").
                var from = 0;
                while (from < text.Length)
                {
                    var pos = text.IndexOf(pattern, from, StringComparison.Ordinal);
                    if (pos < 0) break;

                    var end = pos + pattern.Length;
                    var terminator = end < text.Length ? text[end] : '\0';

                    if (terminator == ']' || (string.IsNullOrEmpty(field.EntityAlias) && terminator == '.'))
                    {
                        var name = textObj.Attribute("Name")?.Value;
                        if (!string.IsNullOrEmpty(name))
                            field.UsedInComponents.Add(name);
                        break;
                    }

                    from = pos + 1;
                }
            }
        }
    }
}
