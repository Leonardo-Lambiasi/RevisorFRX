using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;
using System.Xml.Linq;

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
else if (cmdArgs[0] is "--validar-filtro" or "-v")
{
    var dir = cmdArgs.Length > 1 ? cmdArgs[1] : baseDir;
    ValidarFiltro(dir);
    return;
}
else if (cmdArgs[0] is "--schema" or "-s" && cmdArgs.Length > 1)
{
    ExibirSchema(cmdArgs[1]);
    return;
}
else
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  dotnet run                     Analisa arquivos fixos");
    Console.WriteLine("  dotnet run -- --relatorio       Gera relatório markdown");
    Console.WriteLine("  dotnet run -- --batch <dir>     Analisa todos .frx de uma pasta");
    Console.WriteLine("  dotnet run -- --file <path>     Analisa um arquivo específico");
    Console.WriteLine("  dotnet run -- --validar-filtro  Valida o filtro SchemaExtractor");
    Console.WriteLine("  dotnet run -- --schema <path>   Exibe e valida schema de um .frx");
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

static void ValidarFiltro(string dir)
{
    var arquivos = Directory.GetFiles(dir, "*.frx").OrderBy(f => f).ToArray();
    Console.WriteLine($"\nValidando filtro SchemaExtractor em {arquivos.Length} arquivo(s)...\n");

    int totalSemFiltroGlobal = 0, totalComFiltroGlobal = 0, validationResultGlobal = 0;
    var naoStringProfundoGlobal = new Dictionary<string, (int count, int files)>();
    string? maiorArquivo = null;
    int maiorCount = 0;
    List<SchemaField>? maiorFiltrado = null, maiorUnrestricted = null;

    var maxDepth = SchemaExtractorOptions.Default.MaxDepth;

    foreach (var path in arquivos)
    {
        var nome = Path.GetFileName(path);
        try
        {
            var doc = XDocument.Load(path);
            var semFiltro  = SchemaExtractor.Extract(doc, SchemaExtractorOptions.Unrestricted);
            var comFiltro  = SchemaExtractor.Extract(doc, SchemaExtractorOptions.Default);

            var removidos = semFiltro.Count - comFiltro.Count;
            var reducao   = semFiltro.Count > 0 ? (double)removidos / semFiltro.Count * 100 : 0;

            var filtradoSet    = new HashSet<string>(comFiltro.Select(ColPath));
            var camposRemovidos = semFiltro.Where(f => !filtradoSet.Contains(ColPath(f))).ToList();

            Console.WriteLine($"=== {nome} ===");
            Console.WriteLine($"  Total sem filtro:    {semFiltro.Count,8:N0} campos");
            Console.WriteLine($"  Total com filtro:    {comFiltro.Count,8:N0} campos  ({reducao:F1}% de redução)");
            Console.WriteLine($"  Removidos:           {removidos,8:N0} campos");

            // Breakdown by display type
            Console.WriteLine("\n  Tipos removidos pelo filtro:");
            foreach (var g in camposRemovidos.GroupBy(f => f.DisplayType).OrderByDescending(g => g.Count()))
            {
                var atencao = g.Key is not ("String" or "(sem tipo)" or "⚠ null (objeto)") ? " ← ATENÇÃO" : "";
                Console.WriteLine($"    {g.Key,-22}: {g.Count(),6:N0} campos{atencao}");
            }

            // Scalar (non-String, non-null) removed at depth > maxDepth
            var naoStringProfundos = camposRemovidos
                .Where(f => f.DisplayType is not ("String" or "(sem tipo)" or "⚠ null (objeto)") && ColDepth(f) > maxDepth)
                .ToList();

            if (naoStringProfundos.Count > 0)
            {
                Console.WriteLine($"\n  ⚠ Campos não-String removidos em profundidade > {maxDepth} ({naoStringProfundos.Count} total, amostra):");
                foreach (var f in naoStringProfundos.Take(20))
                    Console.WriteLine($"    {f.FullPath} — {f.DisplayType} — profundidade {ColDepth(f)}");

                foreach (var g in naoStringProfundos.GroupBy(f => f.DisplayType))
                {
                    if (!naoStringProfundoGlobal.ContainsKey(g.Key))
                        naoStringProfundoGlobal[g.Key] = (0, 0);
                    var (c, a) = naoStringProfundoGlobal[g.Key];
                    naoStringProfundoGlobal[g.Key] = (c + g.Count(), a + 1);
                }
            }
            else
            {
                Console.WriteLine($"\n  ✅ Nenhum campo não-String/não-null removido em profundidade > {maxDepth}");
            }

            // Removed by ValidationResult
            var valRemovidos = camposRemovidos
                .Where(f => ColPath(f).Split('.').Any(s => s.Equals("ValidationResult", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (valRemovidos.Count > 0)
            {
                Console.WriteLine($"\n  Campos removidos por ValidationResult ({valRemovidos.Count} total, amostra):");
                foreach (var f in valRemovidos.Take(5))
                    Console.WriteLine($"    {f.FullPath} — {f.DisplayType}");
                validationResultGlobal += valRemovidos.Count;
            }

            Console.WriteLine();
            totalSemFiltroGlobal += semFiltro.Count;
            totalComFiltroGlobal += comFiltro.Count;

            if (semFiltro.Count > maiorCount)
            {
                maiorCount       = semFiltro.Count;
                maiorArquivo     = path;
                maiorFiltrado    = comFiltro;
                maiorUnrestricted = semFiltro;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== {nome} ===\n  ERRO: {ex.Message}\n");
        }
    }

    // Global summary
    var totalRemovidos   = totalSemFiltroGlobal - totalComFiltroGlobal;
    var reducaoGlobal    = totalSemFiltroGlobal > 0 ? (double)totalRemovidos / totalSemFiltroGlobal * 100 : 0;

    Console.WriteLine("=== RESUMO GERAL ===");
    Console.WriteLine($"  Arquivos analisados:       {arquivos.Length}");
    Console.WriteLine($"  Total campos sem filtro:   {totalSemFiltroGlobal,8:N0}");
    Console.WriteLine($"  Total campos com filtro:   {totalComFiltroGlobal,8:N0}  ({reducaoGlobal:F1}% de redução)");

    if (naoStringProfundoGlobal.Count > 0)
    {
        Console.WriteLine($"\n  ⚠ ATENÇÃO — campos não-String removidos em profundidade > {maxDepth}:");
        foreach (var kv in naoStringProfundoGlobal.OrderByDescending(k => k.Value.count))
            Console.WriteLine($"    {kv.Key,-22}: {kv.Value.count,6:N0} campos em {kv.Value.files} arquivo(s)");
        Console.WriteLine($"\n  → Se os campos acima forem legítimos, considere aumentar MaxDepth para {maxDepth + 1}.");
    }
    else
    {
        Console.WriteLine($"\n  ✅ Nenhum campo não-String/não-null removido em profundidade > {maxDepth}");
        Console.WriteLine($"  → MaxDepth padrão = {maxDepth} está correto.");
    }
    Console.WriteLine($"  ✅ Removidos por ValidationResult: {validationResultGlobal:N0} (sempre seguro remover)");

    // Export CSV for the largest file
    if (maiorArquivo != null && maiorFiltrado != null && maiorUnrestricted != null)
    {
        var filtradoSet = new HashSet<string>(maiorFiltrado.Select(ColPath));
        var date        = DateTime.Now.ToString("yyyyMMdd");
        var stem        = Path.GetFileNameWithoutExtension(maiorArquivo).Replace(" ", "_");
        var csvPath     = Path.Combine(dir, "..", $"validacao_filtro_{stem}_{date}.csv");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Entidade;Campo;Tipo;Caminho Completo;Profundidade;Removido pelo filtro padrão");
        foreach (var f in maiorUnrestricted)
        {
            var removido = filtradoSet.Contains(ColPath(f)) ? "Não" : "Sim";
            sb.AppendLine(string.Join(";",
                CsvVal(f.EntityAlias), CsvVal(f.FieldName), CsvVal(f.DisplayType),
                CsvVal(f.FullPath), ColDepth(f).ToString(), removido));
        }
        File.WriteAllText(csvPath, sb.ToString(), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Console.WriteLine($"\n  CSV exportado: {Path.GetFullPath(csvPath)}");
    }
}

static string ColPath(SchemaField f) =>
    string.IsNullOrEmpty(f.EntityAlias) ? f.FieldName : $"{f.EntityAlias}.{f.FieldName}";

static int ColDepth(SchemaField f) => ColPath(f).Count(c => c == '.') + 1;

static string CsvVal(string value)
{
    if (string.IsNullOrEmpty(value)) return "";
    if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}

static void ExibirSchema(string path)
{
    var nome = Path.GetFileName(path);
    var doc  = XDocument.Load(path);

    var comFiltro = SchemaExtractor.Extract(doc, SchemaExtractorOptions.Default);
    var semFiltro = SchemaExtractor.Extract(doc, SchemaExtractorOptions.Unrestricted);

    var maxDepth = SchemaExtractorOptions.Default.MaxDepth;
    var excluidos = string.Join(", ", SchemaExtractorOptions.Default.ExcludedSegments);

    Console.WriteLine($"\n=== Schema: {nome} ===");
    Console.WriteLine();
    Console.WriteLine($"Com filtro padrão (MaxDepth={maxDepth}, excl. {excluidos}):");

    var emUso      = comFiltro.Where(f => f.IsUsed).OrderBy(f => f.FullPath).ToList();
    var disponivel = comFiltro.Where(f => !f.IsUsed).ToList();
    var nulos      = comFiltro.Where(f => f.IsNonScalar).ToList();

    Console.WriteLine($"  Total:       {comFiltro.Count} campos");
    Console.WriteLine($"  Em uso:      {emUso.Count} campos");
    Console.WriteLine($"  Disponível:  {disponivel.Count} campos");
    Console.WriteLine($"  null (obj):  {nulos.Count} campos");

    if (emUso.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Campos \"Em uso\" detectados:");
        foreach (var f in emUso)
        {
            var comp = f.UsedInComponents.Count > 0 ? f.UsedInComponents[0] : "?";
            Console.WriteLine($"    ✅ {f.FullPath,-65} → {comp}");
        }
    }

    // Campos filtrados por ValidationResult
    var filtradoSet = new HashSet<string>(comFiltro.Select(f => f.FullPath));
    var valFiltrados = semFiltro
        .Where(f => !filtradoSet.Contains(f.FullPath)
                 && ColPath(f).Split('.').Any(s => s.Equals("ValidationResult", StringComparison.OrdinalIgnoreCase)))
        .ToList();

    if (valFiltrados.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Campos filtrados (ValidationResult):");
        foreach (var f in valFiltrados)
            Console.WriteLine($"    ⛔ {f.FullPath}");
    }

    Console.WriteLine();
    Console.WriteLine("Sem filtro (Unrestricted):");
    Console.WriteLine($"  Total: {semFiltro.Count} campos (inclui ValidationResult e profundidade {maxDepth + 1}+)");

    // Validação específica para teste_master.frx
    if (!nome.Equals("teste_master.frx", StringComparison.OrdinalIgnoreCase))
        return;

    Console.WriteLine();
    Console.WriteLine("--- Validação: teste_master.frx ---");

    var esperados = new[]
    {
        "[Dados.Titulo.Numero]",
        "[Dados.Titulo.ValorTotal]",
        "[Dados.Titulo.DataVencimento]",
        "[Dados.Titulo.Devedor.Nome]",
        "[Dados.Titulo.Devedor.Endereco.CEP]",
        "[Dados.Titulo.Devedor.Endereco.Municipio.CodigoIBGE]",
        "[Dados.DadosDoCartorio.Nome]",
    };

    var emUsoSet = new HashSet<string>(emUso.Select(f => f.FullPath));
    bool tudo = true;

    foreach (var fullPath in esperados)
    {
        if (emUsoSet.Contains(fullPath))
            Console.WriteLine($"  ✅ {fullPath} detectado como Em uso");
        else
        {
            Console.WriteLine($"  ❌ FALHOU: {fullPath} não detectado como Em uso");
            tudo = false;
        }
    }

    bool valFiltrado = !comFiltro.Any(f =>
        ColPath(f).Split('.').Any(s => s.Equals("ValidationResult", StringComparison.OrdinalIgnoreCase)));

    if (valFiltrado)
        Console.WriteLine("  ✅ ValidationResult corretamente filtrado com Default options");
    else
    {
        Console.WriteLine("  ❌ FALHOU: ValidationResult aparece na extração com filtro ativo");
        tudo = false;
    }

    Console.WriteLine();
    Console.WriteLine(tudo ? "RESULTADO: ✅ TUDO OK" : "RESULTADO: ❌ FALHOU");
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
