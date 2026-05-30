namespace RevisorFRX.Core.Rules;

/// <summary>
/// Configuração da regra Fix-1 carregada de hard1-config.json.
/// Se o arquivo não existir, os valores padrão são usados.
/// </summary>
public class Fix1Config
{
    public Fix1Deteccao Deteccao { get; init; } = new();
    public List<string> PalavrasServentia { get; init; } =
    [
        "Tabelionato", "Cartório", "Ofício", "Registro",
        "TPSP", "TPSC", "TJSP"
    ];
    public List<string> WhitelistTextos { get; init; } =
    [
        "Certifico e dou fé",
        "1º Via", "2º Via", "3º Via",
        "Via do Cliente", "Via do Cartório"
    ];

    /// <summary>
    /// Carrega o JSON de configPath. Se não existir ou falhar,
    /// retorna instância com valores padrão silenciosamente.
    /// </summary>
    public static Fix1Config Carregar(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return new Fix1Config();

            var json = File.ReadAllText(configPath, System.Text.Encoding.UTF8);
            return System.Text.Json.JsonSerializer.Deserialize<Fix1Config>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    // Necessário para mapear snake_case do JSON (ex: "data_literal")
                    // para PascalCase C# (ex: "DataLiteral"). Não remova — sem isso
                    // os campos do JSON são ignorados silenciosamente (defaults ficam).
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                }) ?? new Fix1Config();
        }
        catch
        {
            return new Fix1Config();
        }
    }
}

public class Fix1Deteccao
{
    public bool Cnpj            { get; init; } = true;
    public bool Cpf             { get; init; } = true;
    public bool Cep             { get; init; } = true;
    public bool Telefone        { get; init; } = true;
    public bool DataLiteral     { get; init; } = true;
    public bool ValorMonetario  { get; init; } = true;
    public bool OrdinalCartorio { get; init; } = true;
    public bool AgenciaBancaria { get; init; } = true;
    public bool ImagemEmbutida  { get; init; } = true;
}
