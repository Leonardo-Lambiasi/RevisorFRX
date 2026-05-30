# RevisorFRX
  
Desenvolvido por **Leonardo Lambiasi**

Ferramenta desktop para análise estática de relatórios **FastReport** (`.frx`).  
Detecta referências quebradas, problemas de layout e código problemático — sem abrir o FastReport Designer.

---

## O que faz

Ao abrir um arquivo `.frx`, o RevisorFRX aplica um conjunto de regras estáticas e exibe os resultados em uma tabela, classificados por severidade. Erros impedem o relatório de funcionar corretamente; avisos indicam comportamento inesperado em runtime.

Todas as regras podem ser ativadas/desativadas individualmente pelo botão **⚙** na tela principal.

---

## Regras implementadas

### 🔴 Erros — impedem o relatório de funcionar corretamente

| Código   | O que verifica |
|----------|----------------|
| `Ref-2`  | Atributo `DataSource` referencia um DataSource não declarado no relatório |
| `Ref-1`  | Atributo `MasterComponent` aponta para um componente inexistente no relatório |
| `Code-2` | Cast direto em `Row[]` sem verificação de nulo (qualquer value type) ou `.Value` sem `HasValue` |
| `Code-3` | Métodos utilitários obrigatórios do template padrão ausentes no `<ScriptText>` |

### 🟡 Avisos — comportamento inesperado em runtime

| Código     | O que verifica |
|------------|----------------|
| `Format-1` | Campo `Decimal` sem `Format="Currency"` ou campo `DateTime` sem `Format="Date"` |
| `Format-6` | TextObject contém tags HTML (`<b>`, `<i>`, etc.) mas `TextRenderType` não é `HtmlTags` |
| `Format-7` | Barcode com `Barcode.CalcCheckSum` diferente de `false` — checksum habilitado pode gerar códigos inválidos |
| `Expr-1`   | Expressão `[Dados.Entidade.Campo]` referencia campo ausente no schema do Dictionary |
| `Expr-2`   | Expressão `[Dados.Entidade.Campo]` referencia campo com `DataType="null"` (objeto não escalar) |
| `Ref-12`   | TextObject com `CanGrow="true"` dentro de DataBand sem `CanGrow="true"` — texto pode sobrepor |
| `Code-4`  | CNPJ pode conter letras — detecta validações/máscaras que assumem apenas dígitos |
| `Ref-10`  | Colchetes `[` `]` desbalanceados no `Text` — expressão não resolve corretamente |
| `Fix-1` | Valor fixo no layout que deveria vir do schema: CNPJ, CPF, CEP, telefone, data literal, valor R$, ordinal de cartório (ex: "4º Tabelionato"), agência bancária ou imagem embutida em base64. **Desabilitada por padrão** — ative em ⚙ quando necessário. |
---

## Interface

```
┌──────────────────────────────────────────────────────────────────┐
│  RevisorFRX                                        [🔍][⚙][?]   │
│  Análise estática de relatórios FastReport (.frx)                │
│                                                                  │
│  [Selecionar arquivo .frx]  NomeDoArquivo.frx                    │
│  [Selecionar pasta]  [Analisar]  [✕ Cancelar]                    │
│  ──────────────────────────────────────────────────────────────  │
│  📁 C:\Relatorios\  (15 arquivos .frx)                           │
│  ● Boleto.frx                                                    │
│  ● Certidão.frx                                                  │
│  ● Intimação.frx  ...                                            │
│  ──────────────────────────────────────────────────────────────  │
│  [████████████░░░░]  Analisando arquivo 3 de 15 — Boleto.frx     │
│  ──────────────────────────────────────────────────────────────  │
│  [ 3 Erros ] [ 2 Avisos ]                                       │
│                                                                  │
│  Regra  │ Severidade │ Arquivo │ Componente │ Mensagem │ Detalhe  │
│  ───────┼────────────┼─────────┼────────────┼──────────┼───────  │
│  Ref-10 │ Warning    │ rel.frx │ Text364    │ Colch... │ ...     │
│  Ref-1  │ Error      │ rel.frx │ SubReport1 │ Master.. │ ...     │
│                                                                  │
│  [Exportar relatório CSV]                                        │
└──────────────────────────────────────────────────────────────────┘
```

- `[🔍]` abre o Explorador de Schema (habilitado apenas no modo arquivo único)
- `[⚙]` abre configuração de regras
- `[?]` abre a ajuda interativa com tutorial, passo a passo e glossário
- `[✕ Cancelar]` interrompe análise em andamento (visível apenas durante análise)
- Ao selecionar pasta: lista os `.frx` encontrados antes de analisar
- ProgressBar com nome do arquivo atual durante análise de pasta
- Resultados aparecem incrementalmente por arquivo (não só ao final)

### Explorador de Schema

```
┌────────────────────────────────────────────────────────────────────────┐
│  Explorador de Schema — NomeArquivo.frx                                │
│                                                                        │
│  🔍 [______________]  Tipo: [▼ todos ]  Entidade: [▼ todas ]          │
│  Status: [▼ Disponível ]  □ Apenas ⚠ null    □ Apenas campos usados   │
│  ⚠ Schema grande (26.445 campos). Use os filtros acima.               │
│  [⚙ Avançado ▼]                                                        │
│  ┌─ Filtros avançados ──────────────────────────────────────────────┐  │
│  │  Profundidade máxima: [5 ▲▼]                                     │  │
│  │  Segmentos excluídos: [ValidationResult]                         │  │
│  │                            [Reaplicar]  [Restaurar padrões]      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│  ──────────────────────────────────────────────────────────────────    │
│  Entidade │ Campo │ Tipo │ Caminho completo │ Usado em │ Status        │
│  ─────────────────────────────────────────────────────────────────     │
│  18 campo(s) exibido(s) de 3.754 extraídos (26.445 no schema completo)│
│                         [📥 Exportar CSV] [📋 Copiar caminho] [Fechar] │
└────────────────────────────────────────────────────────────────────────┘
```

- Pesquisa em tempo real por entidade, campo ou caminho completo (debounce 150ms)
- Filtro por tipo, entidade e status (`Em uso` / `Disponível`)
- **Status `⚪ Disponível`**: campo mapeado no código mas não usado no layout — acione o time de projetos se precisar
- **Status `✅ Em uso`**: campo já referenciado em algum TextObject do relatório
- Coluna "Usado em": nome do componente (1 uso), "N componentes" (2+) com tooltip da lista completa, ou "—"
- Duplo-clique, Enter ou `[📋 Copiar caminho]` copia `[Dados.Entidade.Campo]` para o clipboard
- `[📥 Exportar CSV]` exporta os campos **atualmente filtrados** (UTF-8 BOM, separador `;`)
- **Filtro padrão**: profundidade máxima 5, exclui segmento `ValidationResult` — reduz ~82% dos campos sem perder campos de negócio (validado com arquivos reais de até 26.445 campos)
- Painel `[⚙ Avançado]`: ajuste de profundidade e segmentos excluídos; "Reaplicar" reextrai sem bloquear a UI
- Banner de aviso automático para schemas com mais de 5.000 campos
- Filtro Status abre em `Disponível` por padrão — evita congelar a UI na abertura de schemas grandes
- Contador no rodapé exibe `N em uso • M null (obj)` quando nenhum filtro de status está ativo

### Tela de Ajuda ([?])

A tela de ajuda contém documentação interativa com 4 abas:

- **Visão Geral** — o que o sistema faz, quando usar, o que não faz e tipos de resultado
- **Passo a Passo** — fluxo de uso numerado, colunas do grid, botões e as 12 verificações
- **Busca de Dados** — como usar o Explorador de Schema, status dos campos e filtros avançados explicados sem jargão técnico
- **Glossário** — 15 termos do FastReport explicados em linguagem simples

### Fix-1 — Configuração de detectores

A regra Fix-1 é configurável via arquivo `hard1-config.json` na mesma pasta do executável. Para editar: abra `⚙`, selecione Fix-1 na lista e clique em `[✏ Fix-1...]`.

O arquivo permite:
- Ligar/desligar cada detector individualmente (`cnpj`, `cpf`, `cep`, `telefone`, `data_literal`, `valor_monetario`, `ordinal_cartorio`, `agencia_bancaria`, `imagem_embutida`)
- Personalizar palavras de serventia para o detector de ordinal (padrão: Tabelionato, Cartório, Ofício, Registro, TPSP...)
- Adicionar textos à whitelist para evitar falsos positivos (padrão: "Certifico e dou fé", "1º Via", "2º Via"...)

Se o arquivo não existir, a regra usa os valores padrão automaticamente — sem erro.

---

## Requisitos

| Para usar | Para compilar / publicar |
|-----------|--------------------------|
| Windows 10/11 x64 | .NET SDK 8+ no Windows, Linux ou macOS |
| Sem instalação de .NET necessária | `EnableWindowsTargeting=true` no Linux/macOS |

---

## Como compilar

```bash
git clone <repo>
cd RevisorFRX
dotnet build
```

---

## Como publicar (`.exe` autocontido para Windows)

```bash
dotnet publish RevisorFRX.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

O executável gerado fica em:
```
RevisorFRX.App/bin/Release/net8.0-windows/win-x64/publish/
```

> **Importante:** distribua a pasta `publish/` inteira. O `.exe` precisa das DLLs nativas do WinForms na mesma pasta (`wpfgfx_cor3.dll`, `D3DCompiler_47_cor3.dll`, etc.).

Para gerar um **único `.exe` sem dependências externas** (mais lento para abrir, mas portátil):

```bash
dotnet publish RevisorFRX.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Estrutura do projeto

```
RevisorFRX/
├── RevisorFRX.sln
├── .gitignore
├── README.md
├── DOCUMENTACAO.md               # Como funciona internamente
├── hard1-config.json            # Configuração da Fix-1 (copiado para pasta do executável no publish)
├── RevisorFRX.Core/              # Lógica pura, sem dependência de UI
│   ├── Models/
│   │   ├── RuleResult.cs         # Modelo de resultado (Severity, RuleCode, Message…)
│   │   ├── RuleDefinition.cs     # Metadados de cada regra (código, descrição, severidade)
│   │   ├── RuleConfig.cs         # Estado on/off por regra
│   │   └── SchemaField.cs        # Campo do Dictionary com tipo, caminho e uso no relatório
│   ├── RuleRegistry.cs           # Catálogo central com todas as regras do sistema
│   ├── Rules/
│   │   ├── SchemaBuilder.cs      # Helper compartilhado: constrói mapa de campos do Dictionary
│   │   ├── Ref1Rule.cs           # MasterComponent inválido
│   │   ├── Ref2Rule.cs           # DataSource não declarado
│   │   ├── Code2Rule.cs          # Cast direto em Row[] + .Value sem HasValue (Roslyn semântico)
│   │   ├── Code3Rule.cs          # Métodos obrigatórios ausentes (Roslyn)
│   │   ├── Expr1Rule.cs          # Campo ausente no schema
│   │   ├── Expr2Rule.cs          # Campo não escalar em expressão
│   │   ├── Ref10Rule.cs          # Colchetes desbalanceados
│   │   ├── Format1Rule.cs        # Formatação Decimal/DateTime ausente
│   │   ├── Format6Rule.cs        # Tags HTML sem HtmlTags ativado
│   │   ├── Format7Rule.cs        # Barcode sem Checksum=false
│   │   ├── Code4Rule.cs          # CNPJ alfanumérico (jul/2026)
│   │   ├── Ref12Rule.cs          # CanGrow inconsistente banda vs TextObject
│   │   ├── Fix1Rule.cs           # Detecta valores hardcoded no layout
│   │   └── Fix1Config.cs         # Configuração carregada de hard1-config.json
│   └── Services/
│       ├── FrxAnalyzer.cs        # Orquestra as regras e ordena por severidade
│       ├── SchemaExtractor.cs    # Extrai campos do Dictionary como List<SchemaField>
│       └── SchemaExtractorOptions.cs   # Configuração de filtro do SchemaExtractor
└── RevisorFRX.App/               # WinForms — apenas UI
    ├── MainForm.cs               # Janela principal
    ├── ConfigForm.cs             # Tela de ativação/desativação de regras
    ├── HelpForm.cs               # Ajuda interativa com 4 abas (tutorial + glossário)
    ├── SchemaExplorerForm.cs     # Explorador de Schema com filtros e cópia de caminho
    └── Program.cs
```

---

## Modo batch (CLI)

O `RevisorFRX.TestRunner` suporta análise em lote via terminal:

```bash
dotnet run                      # Analisa arquivos fixos definidos no código
dotnet run -- --relatorio       # Gera relatório markdown de todos .frx da pasta
dotnet run -- --batch <dir>     # Analisa todos .frx de uma pasta
dotnet run -- --file <caminho>  # Analisa um arquivo específico
```

O relatório markdown (`--relatorio`) inclui tabela de totais
e detalhamento por regra, pronto para colar em issues ou PRs.

---

## Como adicionar uma nova regra

1. Crie `RevisorFRX.Core/Rules/MinhaRegra.cs`:

```csharp
using System.Xml.Linq;
using RevisorFRX.Core.Models;

namespace RevisorFRX.Core.Rules;

public class MinhaRegra
{
    public List<RuleResult> Check(XDocument doc)
    {
        var results = new List<RuleResult>();
        // sua lógica aqui
        return results;
    }
}
```

2. Registre no catálogo central em `RevisorFRX.Core/RuleRegistry.cs`:

```csharp
new() { Code = "XX-N", Description = "Descrição da regra", DefaultSeverity = Severity.Warning },
```

3. Registre em `FrxAnalyzer.cs` — adicione um campo `static readonly` e a chamada no método `Analyze`:

```csharp
private static readonly MinhaRegra MinhaRegra = new();

// Dentro de Analyze():
if (config.IsEnabled("XX-N"))
    results.AddRange(MinhaRegra.Check(doc));
```

A regra aparece automaticamente na tela de configuração (⚙) e pode ser ativada/desativada pelo usuário.

> Para regras que devem vir desabilitadas por padrão na interface, use `DefaultEnabled = false` no `RuleRegistry`. O modo CLI/TestRunner (`AllEnabled()`) sempre executa todas as regras independentemente.

---

## Testes

O diretório `ArquivoFRXTeste/` contém FRX de teste que exercitam todas as regras:

| Arquivo | O que cobre |
|---------|-------------|
| `teste_completo_todas_regras.frx` | **Todas as 11 regras** com casos positivos e negativos — 20 findings esperados |
| `teste_completo.frx` | Regras principais com exemplos isolados |
| `teste_code4.frx` / `teste_code4_expandido.frx` | CNPJ alfanumérico (Code-4) |
| `teste_aninhado.frx` | Schema aninhado (Expr-1, Expr-2, Format-1, Ref-10) |
| `teste_layout.frx` | Layout (Ref-12, Format-6) |
| `teste_ref2_*.frx` | DataSource ausente (Ref-2) |

Para validar:
```bash
dotnet run -- -f "ArquivoFRXTeste/teste_completo_todas_regras.frx"
```

---

## Bugs corrigidos

| # | Arquivo | Descrição |
|---|---------|-----------|
| 1 | `FrxAnalyzer.cs` | Resultados não estavam sendo ordenados por severidade |
| 2 | `MainForm.cs` | Botão "Analisar" não era desabilitado antes do `await` (double-click disparava análise dupla) |
| 3 | `Expr1Rule.cs` | Schema construído com prefixo `Dados.` causava falso positivo para todos os campos |
| 4 | `Expr1Rule.cs` | Schema usava `Name` de BusinessObjectDataSource; expressões usam `Alias` — todos os campos com Alias eram falsos positivos |
| 5 | `Ref2Rule.cs` | Elementos dentro do Dictionary com atributo `DataSource` geravam falsos positivos |
| 6 | `Format1Rule.cs` | `System.Nullable<Decimal>` e `System.Nullable<DateTime>` não eram detectados como tipos formatáveis |
| 7 | `Format1Rule.cs` | DateTime com `Format="Date"` sem `Format.Pattern` gerava falso positivo — o FastReport omite o atributo quando é o valor padrão (`dd/MM/yyyy`) |
| 8 | `MainForm.cs` | Rótulo do botão de exportação exibia `.txt` em vez de `CSV` |
| 9 | `Expr2Rule.cs` | `Detail` da regra Expr-2 exibia o literal `{caminho}` em vez do nome do campo — faltava prefixo `$` na string de interpolação |
| 10 | `SchemaExtractor.cs` | Colunas aninhadas dentro de `Column` com `DataType="null"` não eram extraídas — o método retornava cedo sem recursão nos filhos |
| 11 | `Ref12Rule.cs` | `InvalidOperationException` ao reordenar grid por severidade — `Clear()` invalidava as referências das linhas |
| 12 | `Code2Rule.cs` / `Code3Rule.cs` | Roslyn `MethodDeclarationSyntax` não capturava métodos em scripts file-scoped (típico do FastReport) — corrigido com `LocalFunctionStatementSyntax` |
| 13 | `MainForm.cs` | Race condition: `_selectedFilePath` lido dentro de `Task.Run` podia mudar se usuário clicasse em outro arquivo |
| 14 | `SchemaExplorerForm.cs` | `CopySelectedPath()` lançava exceção quando grid estava vazio (coluna "Vazio" não tem campo "Caminho") |
| 15 | MainForm.cs | Double-click em Analisar disparava duas análises simultâneas — _analisandoArquivo + dispose do _cts anterior |
| 16 | MainForm.cs | Botão Cancelar reiniciava análise de arquivo único em vez de cancelar — AnalyzeButton_Click agora detecta _analisandoArquivo e chama _cts?.Cancel() |
| 17 | SchemaExplorerForm.cs | BtnReaplicar bloqueava UI thread — async + Task.Run |
| 18 | SchemaExplorerForm.cs | Banner schema grande não atualizava após Reaplicar — AtualizarBannerSchemaGrande() chamado a cada extração |
| 19 | MainForm.cs | Mensagem "Concluído" sumia imediatamente — _lblProgress.Visible = false removido do finally |
| 20 | MainForm.cs | Botões desabilitados visualmente idênticos aos habilitados — SetButtonEnabled() com cor explícita |
| 21 | SchemaExtractor.cs | EnrichWithUsage usava OrdinalIgnoreCase — divergência com Expr1Rule; corrigido para Ordinal |
| 22 | MainForm.cs | CSV exportado sem BOM UTF-8 — acentos errados no Excel pt-BR |
| 23 | MainForm.cs | ReordenarGridPorSeveridade reutilizava DataGridViewRow após Clear() — reconstruído a partir de _results |
| 24 | `MainForm.cs` | Botão "Exportar CSV" visível mesmo com 0 resultados — ocultado quando `_results.Count == 0` |
| 25 | `MainForm.cs` | Badge "Info" removido da UI — nenhuma regra gera severidade Info atualmente; `Severity.Info` preservado no enum para uso futuro |

---

## Limitações conhecidas

- A análise roda em background (`Task.Run`) — a UI não trava, mas arquivos `.frx` muito grandes podem demorar alguns segundos.
- Análise de código (`Code-2`, `Code-3`) depende do `<ScriptText>` ser C# válido; VB.NET e Delphi não são suportados.
- `Format-1` não analisa TextObjects cujo Text contenha expressões matemáticas (`[[Dados.A] + [Dados.B]]`) ou texto literal misturado com campos (`Protocolo: [Dados.X] - [Dados.Data]`) — nesses casos o `Format` age sobre o valor completo já resolvido, não sobre cada campo individualmente. Use funções de formato na própria expressão: `[Format([Dados.Data], 'dd/MM/yyyy')]`.
- `Format-1` não analisa TextObjects cujo Text contenha chamadas de função (parênteses) para evitar falsos positivos.
- `Format-7` verifica `Barcode.CalcCheckSum` — se o atributo não existe no XML, a regra não dispara (assinatura do FastReport para tipos como QR Code não utilizam este atributo).
- `Ref-3` removido — taxa de falso positivo >70% nos modelos reais (eventos legados/tratados externamente). O código (`Ref3Rule.cs`) permanece no repositório como referência, mas não é registrado.
- O **Explorador de Schema** aplica filtro de profundidade (padrão `MaxDepth=5`) e exclui segmentos de infraestrutura (`ValidationResult`). Campos em profundidade > 5 não aparecem por padrão — ajuste em `[⚙ Avançado]` se necessário. Validado com arquivos reais: campos escalares legítimos (ValorTotal, Data, CEP) aparecem em profundidade 5; profundidade 6+ são artefatos do EF Core.
- O modo batch (pasta) processa arquivos **sequencialmente** — sem paralelismo intencional, para evitar acúmulo de `XDocument` grandes em memória simultaneamente.
- A ordenação por clique no header de coluna no Explorador de Schema não está implementada para o modo de filtro ativo.

---

## Fluxo de trabalho com branches (GitHub)

```bash
# 1. Criar e trocar para uma nova branch
git checkout -b apresentacao

# 2. Ver o que será commitado
git status
git diff

# 3. Adicionar os arquivos desejados (ou "." para todos)
git add .
# ou adicione arquivos específicos:
# git add README.md RevisorFRX.App/MainForm.cs

# 4. Commit
git commit -m "Versão apresentação: correções de bugs e FRX de cobertura total"

# 5. Subir a branch para o GitHub (primeira vez)
git push -u origin apresentacao

# Nas próximas vezes na mesma branch:
git push

# 6. No GitHub, abra um Pull Request da branch `apresentacao` para `main`
#    (pela interface web)
```

### Comandos úteis do dia a dia

```bash
git branch                  # lista branches locais
git checkout main           # volta para main
git branch -d apresentacao   # deleta branch local (depois de merge)
git log --oneline -10       # ver os últimos 10 commits
```

---

## Tecnologias

- **.NET 8** — runtime e SDK
- **WinForms** — interface desktop nativa
- **LINQ to XML** (`System.Xml.Linq`) — parsing do `.frx`
- **Microsoft.CodeAnalysis.CSharp** (Roslyn) — análise sintática do ScriptText
