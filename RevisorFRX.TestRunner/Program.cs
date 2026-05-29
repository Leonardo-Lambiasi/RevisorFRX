using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;

var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
var baseDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ArquivoFRXTeste");

if (cmdArgs.Length == 0)
{
    // Modo padrão: analisa arquivos fixos
    var arquivos = new (string Label, string Nome)[]
    {
        ("ARQUIVO ORIGINAL",             "Títulos Fora da Comarca.frx"),
        ("TESTE COMPLETO (com casos injetados)", "teste_completo.frx"),
        ("TESTE CODE-4",                 "teste_code4.frx"),
        ("BOLETO DE PAGAMENTO",          "Boleto de Pagamento (12).frx"),
        ("CERTIDÃO DE CANCELAMENTO",     "Certidão de Cancelamento de Protesto (6).frx"),
        ("INTIMAÇÃO",                    "Intimação (1).frx"),
    };
    foreach (var (label, nome) in arquivos)
    {
        var path = Path.Combine(baseDir, nome);
        if (!File.Exists(path))
        {
            Console.WriteLine($"\n  AVISO: Arquivo não encontrado — {path}");
            continue;
        }
        Analisar(label, path);
    }
}
else if (cmdArgs[0] is "--relatorio" or "-r")
{
    var dir = cmdArgs.Length > 1 ? cmdArgs[1] : baseDir;
    GerarRelatorio(dir);
    return;
}
else if (cmdArgs[0] is "--batch" or "-b")
{
    var dir = cmdArgs.Length > 1 ? cmdArgs[1] : baseDir;
    ProcessarLote(dir);
    return;
}
else if (cmdArgs[0] is "--file" or "-f" && cmdArgs.Length > 1)
{
    Analisar(Path.GetFileName(cmdArgs[1]), cmdArgs[1]);
    return;
}
else
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  dotnet run                  Analisa arquivos fixos");
    Console.WriteLine("  dotnet run -- --relatorio    Gera relatório markdown");
    Console.WriteLine("  dotnet run -- --batch <dir>  Analisa todos .frx de uma pasta");
    Console.WriteLine("  dotnet run -- --file <path>  Analisa um arquivo específico");
    return;
}

// ── helpers ──

static void Analisar(string label, string path)
{
    Console.WriteLine($"\n{new string('=', 72)}");
    Console.WriteLine($"  {label}");
    Console.WriteLine(new string('=', 72));

    var content = File.ReadAllText(path);
    var analyzer = new FrxAnalyzer();
    var results = analyzer.Analyze(content);

    Console.WriteLine($"Total : {results.Count}");
    Console.WriteLine($"Erros : {results.Count(r => r.Severity == Severity.Error)}");
    Console.WriteLine($"Avisos: {results.Count(r => r.Severity == Severity.Warning)}");
    Console.WriteLine($"Infos : {results.Count(r => r.Severity == Severity.Info)}");
    Console.WriteLine();

    foreach (var r in results)
        Console.WriteLine($"  [{r.RuleCode}] {r.Severity,-8} | {r.ComponentName,-22} | {r.Message}");
}

static void ProcessarLote(string dir)
{
    var arquivos = Directory.GetFiles(dir, "*.frx");
    Console.WriteLine($"\nProcessando {arquivos.Length} arquivo(s) em: {dir}\n");

    foreach (var path in arquivos.OrderBy(f => f))
    {
        var label = Path.GetFileName(path);
        try
        {
            Analisar(label, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  ERRO: {label} — {ex.Message}");
        }
    }
}

static void GerarRelatorio(string dir)
{
    var arquivos = Directory.GetFiles(dir, "*.frx");
    Console.WriteLine($"\n# Relatório de Análise FastReport\n");
    Console.WriteLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n");
    Console.WriteLine($"Pasta: `{dir}`\n");
    Console.WriteLine($"Total de arquivos: {arquivos.Length}\n");
    Console.WriteLine("| Arquivo | Total | 🔴 Erros | 🟡 Avisos | 🔵 Infos |");
    Console.WriteLine("|---------|:-----:|:---------:|:---------:|:--------:|");

    var totals = new List<(string Nome, int Total, int Erros, int Avisos, int Infos, List<RuleResult> Resultados)>();

    foreach (var path in arquivos.OrderBy(f => f))
    {
        try
        {
            var content = File.ReadAllText(path);
            var results = new FrxAnalyzer().Analyze(content);
            var e = results.Count(r => r.Severity == Severity.Error);
            var w = results.Count(r => r.Severity == Severity.Warning);
            var i = results.Count(r => r.Severity == Severity.Info);
            var nome = Path.GetFileName(path);
            Console.WriteLine($"| {nome} | {results.Count} | {e} | {w} | {i} |");
            totals.Add((nome, results.Count, e, w, i, results));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"| {Path.GetFileName(path)} | ❌ Erro: {ex.Message} |");
        }
    }

    var totalErros = totals.Sum(t => t.Erros);
    var totalAvisos = totals.Sum(t => t.Avisos);
    var totalInfos = totals.Sum(t => t.Infos);
    Console.WriteLine($"| **Total** | **{totals.Sum(t => t.Total)}** | **{totalErros}** | **{totalAvisos}** | **{totalInfos}** |");

    if (totals.Any(t => t.Resultados.Count > 0))
    {
        Console.WriteLine("\n## Detalhamento por regra\n");
        Console.WriteLine("| Arquivo | Regra | Severidade | Componente | Mensagem |");
        Console.WriteLine("|---------|------|:----------:|------------|----------|");

        foreach (var (nome, _, _, _, _, resultados) in totals)
        {
            foreach (var r in resultados.OrderBy(r => r.Severity).ThenBy(r => r.RuleCode))
            {
                var sev = r.Severity switch
                {
                    Severity.Error => "🔴",
                    Severity.Warning => "🟡",
                    Severity.Info => "🔵",
                    _ => "⚪"
                };
                Console.WriteLine($"| {nome} | {r.RuleCode} | {sev} | {r.ComponentName} | {r.Message} |");
            }
        }
    }
}
