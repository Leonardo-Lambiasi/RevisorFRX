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
---

## Interface

```
┌──────────────────────────────────────────────────────────────┐
│  RevisorFRX                                              [⚙][?] │
│  Análise estática de relatórios FastReport (.frx)             │
│                                                               │
│  [Selecionar arquivo .frx]  NomeDoArquivo.frx  [Selecionar pasta] [Analisar] │
│  ─────────────────────────────────────────────────────────── │
│  [ 3 Erros ] [ 2 Avisos ] [ 0 Info ]                         │
│                                                               │
│  Regra   │ Severidade │ Arquivo    │ Componente │ Mensagem   │ Detalhe    │
│  ────────┼────────────┼────────────┼────────────┼────────────┼─────────── │
│  Ref-10  │ Warning    │ rel.frx    │ Text364    │ Colchetes  │ ...        │
│  Ref-1   │ Error      │ rel.frx    │ SubReport1 │ Master...  │ ...        │
│  ...                                                              │
│                                                               │
│  [Exportar relatório CSV]                                     │
└──────────────────────────────────────────────────────────────┘
```

- Linhas vermelhas → `Error` | Linhas amarelas → `Warning` | Linhas azuis → `Info`
- Exportação gera `.csv` compatível com Excel: `RevisorFRX_NomeArquivo_yyyyMMdd_HHmmss.csv`
- Botão `[?]` abre guia de regras e glossário de termos FastReport
- Botão `[⚙]` abre tela de configuração para ativar/desativar regras individualmente

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
├── RevisorFRX.Core/              # Lógica pura, sem dependência de UI
│   ├── Models/
│   │   ├── RuleResult.cs         # Modelo de resultado (Severity, RuleCode, Message…)
│   │   ├── RuleDefinition.cs     # Metadados de cada regra (código, descrição, severidade)
│   │   └── RuleConfig.cs         # Estado on/off por regra
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
│   │   └── Ref12Rule.cs          # CanGrow inconsistente banda vs TextObject
│   └── Services/
│       └── FrxAnalyzer.cs        # Orquestra as regras e ordena por severidade
└── RevisorFRX.App/               # WinForms — apenas UI
    ├── MainForm.cs               # Janela principal
    ├── ConfigForm.cs             # Tela de ativação/desativação de regras
    ├── HelpForm.cs               # Guia de regras e glossário
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

---

## Limitações conhecidas

- A análise roda em background (`Task.Run`) — a UI não trava, mas arquivos `.frx` muito grandes podem demorar alguns segundos.
- Análise de código (`Code-2`, `Code-3`) depende do `<ScriptText>` ser C# válido; VB.NET e Delphi não são suportados.
- `Format-1` não analisa TextObjects cujo Text contenha expressões matemáticas (`[[Dados.A] + [Dados.B]]`) ou texto literal misturado com campos (`Protocolo: [Dados.X] - [Dados.Data]`) — nesses casos o `Format` age sobre o valor completo já resolvido, não sobre cada campo individualmente. Use funções de formato na própria expressão: `[Format([Dados.Data], 'dd/MM/yyyy')]`.
- `Format-1` não analisa TextObjects cujo Text contenha chamadas de função (parênteses) para evitar falsos positivos.
- `Format-7` verifica `Barcode.CalcCheckSum` — se o atributo não existe no XML, a regra não dispara (assinatura do FastReport para tipos como QR Code não utilizam este atributo).
- `Ref-3` removido — taxa de falso positivo >70% nos modelos reais (eventos legados/tratados externamente). O código (`Ref3Rule.cs`) permanece no repositório como referência, mas não é registrado.

---

## Tecnologias

- **.NET 8** — runtime e SDK
- **WinForms** — interface desktop nativa
- **LINQ to XML** (`System.Xml.Linq`) — parsing do `.frx`
- **Microsoft.CodeAnalysis.CSharp** (Roslyn) — análise sintática do ScriptText
