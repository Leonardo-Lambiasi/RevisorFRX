using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;

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
    Console.WriteLine();

    foreach (var r in results)
        Console.WriteLine($"  [{r.RuleCode}] {r.Severity,-8} | {r.ComponentName,-22} | {r.Message}");
}

var baseDir = "/home/leonardo/Documentos/RevisorFRX/ArquivoFRXTeste";
Analisar("ARQUIVO ORIGINAL", Path.Combine(baseDir, "Títulos Fora da Comarca.frx"));
Analisar("TESTE COMPLETO (com casos injetados)", Path.Combine(baseDir, "teste_completo.frx"));
Analisar("BOLETO DE PAGAMENTO", Path.Combine(baseDir, "Boleto de Pagamento (12).frx"));
Analisar("CERTIDÃO DE CANCELAMENTO", Path.Combine(baseDir, "Certidão de Cancelamento de Protesto (6).frx"));
Analisar("INTIMAÇÃO", Path.Combine(baseDir, "Intimação (1).frx"));
