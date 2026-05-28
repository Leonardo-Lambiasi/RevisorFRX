using RevisorFRX.Core.Models;

namespace RevisorFRX.Core;

public static class RuleRegistry
{
    public static IReadOnlyList<RuleDefinition> GetAll() => _rules;

    private static readonly List<RuleDefinition> _rules =
    [
        new() { Code = "Ref-1",    Description = "MasterComponent inválido",                          DefaultSeverity = Severity.Error   },
        new() { Code = "Ref-2",    Description = "DataSource não declarado",                          DefaultSeverity = Severity.Error   },
        new() { Code = "Ref-10",   Description = "Colchetes desbalanceados no Text",                  DefaultSeverity = Severity.Warning },
        new() { Code = "Ref-12",   Description = "CanGrow inconsistente banda vs TextObject",         DefaultSeverity = Severity.Warning },
        new() { Code = "Code-2",   Description = "Cast direto em Row[] ou .Value sem HasValue",       DefaultSeverity = Severity.Error   },
        new() { Code = "Code-3",   Description = "Métodos obrigatórios ausentes no ScriptText",       DefaultSeverity = Severity.Error   },
        new() { Code = "Format-1", Description = "Formatação Decimal/DateTime incorreta ou ausente",  DefaultSeverity = Severity.Warning },
        new() { Code = "Format-6", Description = "Tags HTML sem TextRenderType=HtmlTags",             DefaultSeverity = Severity.Warning },
        new() { Code = "Format-7", Description = "Barcode sem Checksum=false",                       DefaultSeverity = Severity.Warning },
        new() { Code = "Expr-1",   Description = "Campo ausente no schema do Dictionary",            DefaultSeverity = Severity.Warning },
        new() { Code = "Expr-2",   Description = "Campo não escalar em expressão",                   DefaultSeverity = Severity.Warning },
        new() { Code = "Code-4",   Description = "CNPJ alfanumérico — padrões que assumem apenas dígitos", DefaultSeverity = Severity.Warning, DefaultEnabled = false },
    ];
}
