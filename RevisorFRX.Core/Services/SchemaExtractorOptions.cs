namespace RevisorFRX.Core.Services;

/// <summary>
/// Controla quais campos o SchemaExtractor inclui na extração.
/// Os valores padrão eliminam ~62% dos campos que são artefatos de
/// infraestrutura (EF Core) e nunca aparecem em expressões FastReport.
/// </summary>
public class SchemaExtractorOptions
{
    /// <summary>
    /// Profundidade máxima de entidades extraídas, contada pelo número
    /// de segmentos separados por ponto no caminho da entidade.
    /// Ex: "Apontamento.Atos.Ato.TipoDeAto" tem profundidade 4.
    /// Padrão: 5 — validado com arquivos reais; campos escalares legítimos
    /// (ValorTotal, Data, CEP) aparecem em profundidade 5 em relatórios de
    /// intimação. Profundidade 6+ são artefatos de infraestrutura (EF Core).
    /// Use int.MaxValue para desabilitar o filtro.
    /// </summary>
    public int MaxDepth { get; init; } = 5;

    /// <summary>
    /// Segmentos de nome de entidade que, se presentes em qualquer parte
    /// do caminho, fazem o campo ser ignorado.
    /// Padrão: ["ValidationResult"] — artefato interno do EF Core.
    /// </summary>
    public IReadOnlyList<string> ExcludedSegments { get; init; } =
        ["ValidationResult"];

    /// <summary>
    /// Opções sem nenhum filtro — extrai absolutamente tudo.
    /// Use apenas para diagnóstico ou testes.
    /// </summary>
    public static SchemaExtractorOptions Unrestricted => new()
    {
        MaxDepth = int.MaxValue,
        ExcludedSegments = []
    };

    /// <summary>
    /// Opções padrão — filtro equilibrado para uso em produção.
    /// </summary>
    public static SchemaExtractorOptions Default => new();
}
