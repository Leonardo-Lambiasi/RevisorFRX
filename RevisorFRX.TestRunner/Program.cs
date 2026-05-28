using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;

var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
var baseDir = "/home/leonardo/Documentos/RevisorFRX/ArquivoFRXTeste";

if (cmdArgs.Length == 0)
{
    // Modo padrão: analisa arquivos fixos
    Analisar("ARQUIVO ORIGINAL", Path.Combine(baseDir, "Títulos Fora da Comarca.frx"));
    Analisar("TESTE COMPLETO (com casos injetados)", Path.Combine(baseDir, "teste_completo.frx"));
    Analisar("TESTE CODE-4", Path.Combine(baseDir, "teste_code4.frx"));
    Analisar("BOLETO DE PAGAMENTO", Path.Combine(baseDir, "Boleto de Pagamento (12).frx"));
    Analisar("CERTIDÃO DE CANCELAMENTO", Path.Combine(baseDir, "Certidão de Cancelamento de Protesto (6).frx"));
    Analisar("INTIMAÇÃO", Path.Combine(baseDir, "Intimação (1).frx"));
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
