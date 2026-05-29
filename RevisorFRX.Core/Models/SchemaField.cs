namespace RevisorFRX.Core.Models;

public class SchemaField
{
    public string EntityAlias { get; init; } = "";
    public string FieldName { get; init; } = "";
    public string DataType { get; init; } = "";

    public string FullPath => string.IsNullOrEmpty(EntityAlias)
        ? $"[Dados.{FieldName}]"
        : $"[Dados.{EntityAlias}.{FieldName}]";

    public string DisplayType => DataType switch
    {
        "null"            => "⚠ null (objeto)",
        "System.String"   => "String",
        "System.Decimal"  => "Decimal",
        "System.Double"   => "Double",
        "System.Single"   => "Single",
        "System.Int32"    => "Int32",
        "System.Int64"    => "Int64",
        "System.Int16"    => "Int16",
        "System.DateTime" => "DateTime",
        "System.Boolean"  => "Boolean",
        "System.Guid"     => "Guid",
        "" or null        => "(sem tipo)",
        _ => DataType.StartsWith("System.", StringComparison.Ordinal)
             ? DataType["System.".Length..]
             : DataType
    };

    public bool IsNonScalar => DataType == "null";

    public List<string> UsedInComponents { get; init; } = [];

    public bool IsUsed => UsedInComponents.Count > 0;
}
