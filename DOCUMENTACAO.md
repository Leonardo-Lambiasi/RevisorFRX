# RevisorFRX — Documentação Técnica

Como o projeto funciona internamente, como cada regra opera e como publicar.

---

## Arquitetura

O projeto é dividido em duas camadas:

```
RevisorFRX.Core   →   Lógica pura. Sem dependência de UI, sem WinForms.
RevisorFRX.App    →   Interface WinForms. Apenas chama o Core e exibe resultados.
```

Isso permite reutilizar o Core em outros contextos (CLI, testes, integração CI) sem
arrastar dependências de interface.

Dentro do Core existem dois helpers de schema independentes:

```
SchemaBuilder.BuildTypeMap(doc)
    → Dictionary<string, string>   (caminho → DataType)
    normaliza DataType="null" → ""
    usado por: Expr1Rule (existência de campos) e Format1Rule (tipos formatáveis)

SchemaExtractor.Extract(doc)
    → List<SchemaField>
    preserva DataType="null" para detectar não-escalares
    recursão nos filhos de Column (sub-colunas aninhadas)
    enriquece com lista de TextObjects que referenciam cada campo
    usado por: SchemaExplorerForm (visualização interativa do Dictionary)

SchemaExtractorOptions
    → configuração de filtro para SchemaExtractor
    MaxDepth: profundidade máxima de entidades extraídas (padrão: 5)
    ExcludedSegments: segmentos de nome que causam exclusão (padrão: ["ValidationResult"])
    Unrestricted: extrai absolutamente tudo (diagnóstico/testes)
    Motivação: arquivos reais têm 8.000–26.000 campos mapeados pelo EF Core;
               o filtro padrão reduz ~82% do ruído sem perder campos de negócio.
               Validado com arquivos reais: campos em profundidade 5 são legítimos
               (ValorTotal, Data, CEP em Intimação); profundidade 6+ são artefatos
               (ImagemFoto, UsuarioQueFezOhCadastro, ValidationResult.*).
```

`SchemaField` (modelo em `RevisorFRX.Core.Models`):
```csharp
EntityAlias       // Ex: "ItenPedido" ou "DadosDoCartorio.Endereco"
FieldName         // Ex: "ValorTotal"
DataType          // Ex: "System.Decimal" ou "null"
FullPath          // → "[Dados.ItenPedido.ValorTotal]"
DisplayType       // → "Decimal", "String", "⚠ null (objeto)", etc.
IsNonScalar       // DataType == "null"
UsedInComponents  // nomes dos TextObjects que referenciam este campo
IsUsed            // UsedInComponents.Count > 0
```

---

## Tela de Ajuda (HelpForm)

Aberta pelo botão `[?]` na toolbar. Tema claro, consistente com MainForm e ConfigForm.

Estrutura: `TabControl` com 4 abas, `RichTextBox` read-only em cada aba
(Segoe UI 10pt, sem borda, fundo 248,249,250).

| Aba | Conteúdo |
|-----|----------|
| Visão Geral | O que o sistema faz, quando usar, o que não faz, tipos de resultado |
| Passo a Passo | Fluxo de uso numerado, colunas do grid, botões, 12 verificações |
| Busca de Dados | Explorador de Schema explicado sem jargão, status dos campos, filtros avançados |
| Glossário | 15 termos do FastReport em linguagem simples |

Paleta de cores do conteúdo:
- Títulos: azul `(13, 110, 253)`
- Texto corrido: quase preto `(33, 37, 41)`
- Severidade Error: vermelho escuro `(180, 35, 35)`
- Severidade Warning: âmbar escuro `(160, 100, 0)`
- Dimmed/secundário: cinza médio `(108, 117, 125)`

---

## Fluxo de execução

### Análise de regras

```
Usuário clica "Analisar"
       │
       ▼
MainForm.AnalyzeButton_Click
       │  File.ReadAllText(path)
       │  await Task.Run(...)     ← análise roda em background (não trava UI)
       ▼
FrxAnalyzer.Analyze(frxContent)
       │  XDocument.Parse(frxContent)   ← parse do XML
       │  foreach regra → Check(doc)
       │  OrderBy(Severity)
       ▼
List<RuleResult>
       │
       ▼
MainForm.PopulateGrid()   ← volta para o thread de UI após await
MainForm.UpdateBadges()
```

```
Proteção contra double-click:
  _analisandoArquivo = true antes do primeiro await
  AnalyzeButton_Click verifica _analisandoArquivo no início:
    → se true: chama _cts?.Cancel() e retorna sem reiniciar
  _cts anterior é cancelado e descartado antes de criar novo
  _analisandoArquivo = false no finally de FinalizarModoAnalise()
```

### Explorador de Schema

```
Usuário clica "🔍" (habilitado apenas com arquivo único selecionado)
       │
       ▼
MainForm.BtnSchema_Click
       │  XDocument.Load(path)  ← dentro de Task.Run (não trava UI)
       ▼
SchemaExtractor.Extract(doc, SchemaExtractorOptions.Default)
       │  navega <Dictionary> (Alias-first, recursivo)
       │  aplica filtro: MaxDepth=5, ExcludedSegments=["ValidationResult"]
       │  extrai List<SchemaField> com EntityAlias + FieldName + DataType
       │  enriquece UsedInComponents via TextObject.Text (case-sensitive)
       ▼
List<SchemaField>  (~3.000–4.000 campos após filtro vs 8.000–26.000 sem filtro)
       │
       ▼
SchemaExplorerForm(fields, fileName, frxPath).ShowDialog()
        │  Virtual Mode no DataGridView (renderiza só linhas visíveis)
        │  filtro assíncrono com Task.Run + CancellationToken
        │  debounce 150ms no campo de busca
        │  filtros: texto livre, tipo, entidade, status, null-only, used-only
        │  Status: "✅ Em uso" / "⚪ Disponível"
        │  coluna "Usado em": nome (1), "N componentes" (2+) com tooltip
        │  duplo-clique / Enter / botão copia FullPath para clipboard
        │  exporta CSV (UTF-8 BOM, separador ";", campos filtrados)
        │  painel "⚙ Avançado": MaxDepth e ExcludedSegments configuráveis
        │    → "Reaplicar": async, reextrai sem bloquear UI
        │    → "Restaurar padrões": volta para SchemaExtractorOptions.Default
        │  banner de aviso automático para schemas > 5.000 campos
        │  filtro Status abre em "Disponível" por padrão
```

### Análise de pasta (batch)

```
Usuário clica "Selecionar pasta"
       │
       ▼
MainForm.BatchButton_Click
       │  Directory.GetFiles("*.frx") ordenado por nome
       │  Exibe lista de arquivos no ListBox antes de analisar
       │  Habilita botão "Analisar"
       ▼
Usuário clica "Analisar"
       │  _batchCts = new CancellationTokenSource()
       │  Botão "✕ Cancelar" fica visível
       ▼
AnalisarPastaAsync(arquivos[], token)
       │
       └── Para cada arquivo (sequencial, um por vez):
              │  Atualiza ProgressBar + label "Analisando arquivo N de M — Nome.frx"
              │
              ▼
           Task.Run(token):
              │  XDocument.Load(caminho)   ← local ao lambda, GC coleta antes do próximo
              │  FrxAnalyzer.Analyze(doc, _ruleConfig)
              ▼
           List<RuleResult>
              │
              ▼
           AdicionarResultadosAoGrid()   ← incrementalmente, não só ao final
              │
              ▼
           _progressBar.Value++

       │  (após todos os arquivos)
       ▼
ReordenarGridPorSeveridade()   ← ordena _results e reconstrói grid
       │
       ▼
"Concluído — N arquivo(s) analisado(s)"  ← label permanece visível
Botão "✕ Cancelar" some

Cancelamento:
  Usuário clica "✕ Cancelar"
       │  _batchCts.Cancel()
       │  OperationCanceledException propagada
       ▼
  "Análise cancelada pelo usuário."
  Resultados parciais já exibidos permanecem no grid
```

---

## Estrutura do arquivo .frx

Um `.frx` é um XML com esta estrutura:

```xml
<Report>
  <Dictionary>
    <!-- Declara todas as fontes de dados -->
    <BusinessObjectDataSource Name="Dados" ...>
      <BusinessObjectDataSource Name="Apontamentos" ...>
        <Column Name="DataDoProtocolo" DataType="System.DateTime"/>
        <Column Name="Titulo" DataType="null">
          <Column Name="ValorAProtestar" DataType="System.Decimal"/>
        </Column>
      </BusinessObjectDataSource>
    </BusinessObjectDataSource>
  </Dictionary>

  <ReportPage Name="Page1">
    <DataBand Name="Data1" DataSource="Apontamentos">
      <TextObject Name="Text1"
                  Text="[Dados.Apontamentos.DataDoProtocolo]"
                  Format="Date"
                  Format.Pattern="dd/MM/yyyy"
                  BeforePrintEvent="FormatarData"/>
    </DataBand>
  </ReportPage>

  <ScriptText><![CDATA[
    private void FormatarData(object sender, EventArgs e) { ... }
  ]]></ScriptText>
</Report>
```

### Pontos-chave do formato

| Conceito | Onde no XML | O que é |
|----------|------------|---------|
| Schema de dados | `<Dictionary>` | Todas as entidades e campos disponíveis |
| Expressão de campo | `Text="[Dados.X.Y]"` | Referência a um campo do schema |
| Evento | `BeforePrintEvent="Metodo"` | Nome do método no ScriptText |
| Formatação | `Format="Currency"` | Tipo de formato a exibir |
| Código C# | `<ScriptText>` | Bloco compilado em runtime pelo FastReport |
| DataSource | `DataSource="Apontamentos"` | Nome do BODS que alimenta a banda |

### Paths de campos vs. atributos Name/Alias

Os caminhos nas expressões `[Dados.X.Y]` usam o **Alias** dos `BusinessObjectDataSource`
(quando presente) e o **Name** das colunas (`Column`):

```xml
<!-- Alias="Apresentantes" → expressão usa [Dados.Apontamentos.Apresentantes.Nome] -->
<BusinessObjectDataSource Name="BusinessObjectDataSource659"
                          Alias="Apresentantes" ...>
  <Column Name="Nome" DataType="System.String"/>
</BusinessObjectDataSource>
```

O RevisorFRX constrói o schema interno priorizando `Alias` sobre `Name` para corresponder
ao comportamento real do FastReport Designer.

---

## Como cada regra funciona

### Ref-1 — MasterComponent inválido

```
1. Coleta todos os atributos Name de todos os elementos do doc → HashSet
2. Varre elementos com atributo MasterComponent
3. Se MasterComponent não está no HashSet → Erro
```

Exemplo de detecção:
```xml
<DataBand MasterComponent="DataBand99"/>
<!-- "DataBand99" não existe no relatório → Ref-1 Error -->
```

---

### Ref-2 — DataSource não declarado

```
1. Coleta todos os BusinessObjectDataSource/TableDataSource/CsvDataSource declarados
2. Varre elementos com atributo DataSource
   (ignora elementos dentro do Dictionary e os próprios tipos de fonte)
3. Se DataSource não está nos declarados → Erro
```

Exemplo:
```xml
<DataBand DataSource="FonteQueNaoExiste"/>
<!-- → Ref-2 Error -->
```

---

### Format-6 — Tags HTML sem HtmlTags ativado

```
1. Para cada TextObject com atributo Text:
2.   Aplica regex em busca de tags HTML (<b>, <i>, <u>, <font>, <div>, <p>, etc.)
3.   Se encontrou tags E o atributo TextRenderType NÃO é "HtmlTags" → Aviso
```

Tags HTML sem `TextRenderType="HtmlTags"` são exibidas como texto literal.

```xml
<TextObject Text="&lt;b&gt;Nome:&lt;/b&gt; [Dados.X]" .../>
<!-- sem TextRenderType="HtmlTags" → Format-6 Warning -->
```

### Ref-12 — CanGrow/CanShrink inconsistente entre TextObject e banda

```
1. Para cada TextObject com CanGrow="true" ou CanShrink="true":
2.   Sobe na árvore até achar a DataBand/GroupHeader/ChildBand mais próxima
3.   Se a banda não tem a mesma propriedade ativa → Aviso
```

Se o TextObject cresce mas a banda não, o texto extravasa e sobrepõe
os componentes abaixo.

```xml
<DataBand Name="Data1" ...>                    <!-- sem CanGrow -->
  <TextObject Name="Text1" CanGrow="true" .../> <!-- Ref-12 Warning -->
</DataBand>
```

### Code-2 — Cast direto em Row[] + .Value sem HasValue (Roslyn semântico)

```
1. Parseia ScriptText com Roslyn
2. Constrói CSharpCompilation + SemanticModel com referências básicas
3. Navega DescendantNodes().OfType<CastExpressionSyntax>()
4. Para cada cast, obtém o TypeInfo via SemanticModel.GetTypeInfo()
5. Se o tipo resolvido é um value type (struct, primitivo, enum)
   E o operando contém "Row[" → Erro
6. Navega DescendantNodes().OfType<MemberAccessExpressionSyntax>()
7. Para cada acesso a ".Value" em expressão contendo "Row[":
8.   Se o ancestral NÃO tem um MemberAccessExpressionSyntax ".HasValue"
     (diferente do próprio .Value) → Erro
```

Usa TypeInfo do SemanticModel, não lista fixa de tipos.
Qualquer value type (int, decimal, struct personalizado) é detectado — sem falsos negativos.
Tipos seguros como string (reference type) são automaticamente ignorados pelo mesmo motivo.
Conversões seguras (Convert.ToBoolean, as? operator) não disparam o cast direto.

---

### Code-3 — Métodos obrigatórios ausentes

```
1. Extrai <ScriptText>
2. Se ausente ou vazio → Erro único "ScriptText ausente ou vazio"
3. Parseia com Roslyn → HashSet de nomes de métodos declarados
4. Para cada método em {AplicarMascaraDeDocumento,
   ExtrairCaracteresNumericos, AplicarMascaraDeCNPJ, AplicarMascaraDeCPF}:
   Se não está no HashSet → Erro
```

---

### Code-4 — CNPJ alfanumérico

```
1. VerificarScriptText: busca padrões no código C#:
   a. Regex \d{14} ou [0-9]{14} → validação que rejeita letras
   b. .Length == 14 ou .Count == 14 → contagem fixa que falha com letras
2. VerificarTextObjects: busca máscara ##.###.###/####-## no Text ou Format
3. VerificarDictionary: campos com nome contendo cnpj/cgc/cpf e tipo numérico
```

A IN 2117/2023 da Receita Federal permite letras no CNPJ.
Validações que assumem apenas dígitos precisam ser revisadas.

---

### Format-7 — Barcode sem Checksum=false

```
1. Para cada BarcodeObject:
2.   Lê atributo Barcode.CalcCheckSum
3.   Se valor não é "false" → Aviso
```

Checksum habilitado pode gerar códigos de barras inválidos para leitura.

```xml
<BarcodeObject Name="Cod1" Barcode.CalcCheckSum="true" .../>
<!-- → Format-7 Warning -->
```

---
### Expr-1 — Campo ausente no schema

```
1. Constrói HashSet com todos os caminhos de campos do Dictionary via SchemaBuilder
   (Alias-first para BODS, Name para Column, sem o prefixo "Dados.")
2. Para cada TextObject com Text contendo [Dados.X.Y]:
   a. Aplica regex \[Dados\.([^\]\[()]+)\]
   b. Verifica se caminho capturado existe no HashSet
   c. Se não existe → Aviso
```

Expressões com funções (parênteses no regex) são automaticamente ignoradas
porque o character class `[^\]\[()]` rejeita `(`.

---

### Ref-10 — Colchetes desbalanceados

```
1. Para cada TextObject com atributo Text:
2.   Conta quantos '[' e quantos ']' existem no valor
3.   Se a contagem difere → Aviso
```

Exemplo:
```xml
<TextObject Text="[Dados.Apontamentos.Valor" .../>
<!-- 1 abre, 0 fecha → Ref-10 Warning -->
```

---

### Expr-2 — Campo não escalar em expressão

```
1. Constrói mapa caminho→DataType via SchemaBuilder (mesmo hashSet do Expr-1)
2. Filtra apenas campos com DataType vazio (= "null" no XML)
3. Para cada TextObject com [Dados.X.Y]:
4.   Se o campo está no filtro → Aviso
```

Campos com `DataType="null"` são objetos intermediários (não escalares).
Exibi-los como texto não funciona em runtime.

Exemplo:
```xml
<Column Name="Endereco" DataType="null">
  <Column Name="Cep" DataType="System.String"/>
</Column>

<TextObject Text="[Dados.Pessoa.Endereco]" .../>
<!-- Endereco é objeto, não string → Expr-2 Warning -->
<!-- Correto: [Dados.Pessoa.Endereco.Cep] -->

---

### Format-1 — Formatação ausente ou incorreta

```
1. Constrói mapa caminho→DataType via SchemaBuilder (compartilhado com Expr-1)
   Filtra apenas colunas com DataType preenchido e diferente de "null"
2. Para cada TextObject:
   a. Aplica regex em todos os matches de [Dados.X.Y] no Text
   b. Skipa match individual se precedido por '(' (ex: FormatDateTime([...]))
   c. Busca DataType no mapa
   d. Coleta problemas de Decimal e DateTime em listas separadas
   e. Gera UM resultado por tipo de problema por TextObject com resumo no Detail:
      — 1 campo afetado: nomeia o campo e recomendação
      — N campos afetados: lista todos no Detail
```

Detecta Nullable<T> via `Contains("Decimal") && Contains("Nullable")`.
O agrupamento por TextObject reduz ruído — um componente com múltiplos campos
sem Format gera um único aviso em vez de N.

**Nota sobre DateTime:** a regra aceita qualquer valor de `Format` que não seja
`Currency`, `Number` ou `Boolean`. `Format.Pattern` não é exigido — o FastReport
omite esse atributo no XML quando o padrão `dd/MM/yyyy` está em uso.

**TextObjects ignorados pelo Format-1:**
- Text com `[[` → expressão matemática (`[[Dados.A] + [Dados.B]]`)
- Text com texto estático misturado (`Protocolo: [Dados.X] - [Dados.Data]`) — detectado
  removendo todos os matches `[Dados.X.Y]` do Text e verificando se sobra conteúdo não-branco.
  Nesses casos o `Format` age sobre o valor já resolvido; use `[Format([Dados.Data], 'dd/MM/yyyy')]`.
- Text com chamada de função (`(` detectado na regex)

---

### Fix-1 — Valor fixo no layout

**Desabilitada por padrão.** Ative em ⚙ quando necessário.
Configurável via `hard1-config.json` na pasta do executável.

```
1. Para cada TextObject:
   a. Se Text contém [Dados. → skip (expressão do schema)
   b. Se Text contém qualquer item da whitelist → skip
   c. Aplica detectores habilitados em Fix1Config:
      CNPJ:     \d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}
      CPF:      \b\d{3}\.\d{3}\.\d{3}-\d{2}\b
      CEP:      \bCEP\s*:?\s*\d{5}-\d{3}\b  (exige label "CEP:")
      Telefone: \(\d{2}\)\s*\d{4,5}-\d{4}
      Data:     \b\d{2}/\d{2}/\d{4}\b + sem '[' no Text
      Valor R$: R\$\s*\d+ + Text.Length <= 120
      Ordinal:  \d+[oOºª°]\s+(Tabelionato|Cartório|...)
                palavras configuráveis em PalavrasServentia
      Agência:  \bAg[eê]ncia\s+\d+\b
2. Para cada PictureObject:
   Se Image preenchido E DataColumn vazio
   E sem AfterDataEvent → Warning (imagem embutida em base64)
```

**Regex de ordinal:** compilado sob demanda e cacheado por instância de
`Fix1Rule`. Recompila apenas se `PalavrasServentia` mudar entre chamadas.

**Configuração (`hard1-config.json`):**
- `deteccao`: liga/desliga cada detector individualmente
- `palavras_serventia`: lista extensível de tipos de serventia
- `whitelist_textos`: textos que nunca devem ser flagados
  (padrão: "Certifico e dou fé", "1º Via", "2º Via"...)
- Se o arquivo não existir: usa defaults silenciosamente, sem erro

**Limite conhecido:** texto livre sem padrão estrutural (nome de tabelião,
endereço sem CEP, nome de município) não é detectado — requer lista curada.

---

## Modelo de dados

```csharp
public enum Severity { Error, Warning, Info }

public class RuleResult
{
    public string RuleCode      { get; set; }   // "Ref-1", "Code-2", etc.
    public Severity Severity    { get; set; }   // Error, Warning, Info
    public string ComponentName { get; set; }   // Nome do elemento afetado
    public string Message       { get; set; }   // Descrição curta
    public string Detail        { get; set; }   // Contexto adicional (linha, valor)
}

public class SchemaField
{
    public string EntityAlias        { get; init; }  // Ex: "Titulo" ou "Titulo.Devedor"
    public string FieldName          { get; init; }  // Ex: "ValorTotal"
    public string DataType           { get; init; }  // Ex: "System.Decimal" ou "null"
    public string FullPath           { get; }        // "[Dados.Titulo.ValorTotal]"
    public string DisplayType        { get; }        // "Decimal", "⚠ null (objeto)", etc.
    public bool   IsNonScalar        { get; }        // DataType == "null"
    public List<string> UsedInComponents { get; init; } // TextObjects que referenciam
    public bool   IsUsed             { get; }        // UsedInComponents.Count > 0
}
```

---

## Considerações de performance

### Análise de regras
- Roda em `Task.Run` — UI não trava durante análise
- `XDocument` declarado dentro do lambda: sai de escopo ao fim, GC coleta antes do próximo arquivo
- Arquivo único: `_analisandoArquivo` previne análises paralelas por double-click
- Fix-1: regex estáticos com `RegexOptions.Compiled`; regex de ordinal cacheado por instância — recompila apenas quando `PalavrasServentia` mudar entre execuções

### Explorador de Schema
- `DataGridView` em Virtual Mode: renderiza apenas as ~20 linhas visíveis na tela, independente do volume total
- Filtro assíncrono com `Task.Run` + `CancellationToken`: LINQ roda em background, último filtro vence
- Debounce 150ms no `TextChanged`: evita rebuild do grid a cada tecla
- `SchemaExtractorOptions.Default` (MaxDepth=5): reduz ~82% dos campos antes de qualquer filtro de UI
- `BtnReaplicar` é `async`: reextração com `Unrestricted` (~26.000 campos) não congela a janela

### Batch (pasta)
- Processamento sequencial intencional: evita acúmulo de múltiplos `XDocument` grandes em memória
- Resultados adicionados ao grid incrementalmente: usuário vê progresso em tempo real
- `CancellationToken` propagado por todo o loop: cancelamento responde imediatamente

---

## Bugs corrigidos

> O histórico completo de bugs corrigidos (changelog) foi movido para o
> `README.md`, seção **"Bugs corrigidos"**.

---

## Considerações para repositório público

O projeto não contém:
- Strings de conexão com banco de dados
- Nomes de servidores internos
- Credenciais ou tokens
- Caminhos de rede internos

O `RevisorFRX.Core` opera exclusivamente sobre o XML do arquivo `.frx` passado
como string — sem acesso a banco, rede ou sistema de arquivos além do arquivo analisado.
