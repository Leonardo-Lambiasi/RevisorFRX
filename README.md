# RevisorFRX
  
Desenvolvido por **Leonardo Lambiasi**

Ferramenta desktop para análise estática de relatórios **FastReport** (`.frx`).  
Detecta referências quebradas, problemas de layout e código problemático — sem abrir o FastReport Designer.

---

## O que faz

Ao abrir um arquivo `.frx`, o RevisorFRX aplica um conjunto de regras estáticas e exibe os resultados em uma tabela, classificados por severidade. Erros impedem o relatório de funcionar corretamente; avisos indicam comportamento inesperado em runtime.

---

## Regras implementadas

### 🔴 Erros — impedem o relatório de funcionar corretamente

| Código   | O que verifica |
|----------|----------------|
| `Ref-2`  | Atributo `DataSource` referencia um DataSource não declarado no relatório |
| `Ref-1`  | Atributo `MasterComponent` aponta para um componente inexistente no relatório |
| `Code-2` | Cast direto `(Boolean)`, `(DateTime)`, `(Decimal)` etc. em `Row[]` sem verificação de nulo |
| `Code-3` | Métodos utilitários obrigatórios do template padrão ausentes no `<ScriptText>` |

### 🟡 Avisos — comportamento inesperado em runtime

| Código     | O que verifica |
|------------|----------------|
| `Format-1` | Campo `Decimal` sem `Format="Currency"` ou campo `DateTime` sem `Format="Date"` |
| `Expr-1`   | Expressão `[Dados.Entidade.Campo]` referencia campo ausente no schema do Dictionary |

### 🔵 Info — pontos de atenção para revisão

| Código   | O que verifica |
|----------|----------------|
| `Ref-3`  | Atributo `*Event` referencia um método ausente no `<ScriptText>` |
| `Code-4` | Método `*_AfterData` modifica `.Text` ou `.Visible` de componente diferente do dono do evento |

---

## Interface

```
┌──────────────────────────────────────────────────────────────┐
│  RevisorFRX                                               [?] │
│  Análise estática de relatórios FastReport (.frx)             │
│                                                               │
│  [Selecionar arquivo .frx]  NomeDoArquivo.frx     [Analisar] │
│  ─────────────────────────────────────────────────────────── │
│  [ 3 Erros ] [ 2 Avisos ] [ 0 Info ]                         │
│                                                               │
│  Regra   │ Severidade │ Componente │ Mensagem   │ Detalhe    │
│  ────────┼────────────┼────────────┼────────────┼─────────── │
│  Ref-3   │ Info       │ Text364    │ Método...  │ ...        │
│  Ref-1   │ Error      │ SubReport1 │ Master...  │ ...        │
│  ...                                                          │
│                                                               │
│  [Exportar relatório CSV]                                     │
└──────────────────────────────────────────────────────────────┘
```

- Linhas vermelhas → `Error` | Linhas amarelas → `Warning` | Linhas azuis → `Info`
- Exportação gera `.csv` compatível com Excel: `RevisorFRX_NomeArquivo_yyyyMMdd_HHmmss.csv`
- Botão `[?]` abre guia de regras e glossário de termos FastReport

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
│   │   └── RuleResult.cs         # Modelo de resultado (Severity, RuleCode, Message…)
│   ├── Rules/
│   │   ├── SchemaBuilder.cs      # Helper compartilhado: constrói mapa de campos do Dictionary
│   │   ├── Ref1Rule.cs           # MasterComponent inválido
│   │   ├── Ref2Rule.cs           # DataSource não declarado
│   │   ├── Ref3Rule.cs           # Evento → método ausente no ScriptText
│   │   ├── Code2Rule.cs          # Cast direto em Row[] (Roslyn)
│   │   ├── Code3Rule.cs          # Métodos obrigatórios ausentes (Roslyn)
│   │   ├── Code4Rule.cs          # AfterData modificando componente diferente (Roslyn)
│   │   ├── Expr1Rule.cs          # Campo ausente no schema
│   │   └── Format1Rule.cs        # Formatação Decimal/DateTime ausente
│   └── Services/
│       └── FrxAnalyzer.cs        # Orquestra as regras e ordena por severidade
└── RevisorFRX.App/               # WinForms — apenas UI
    ├── MainForm.cs               # Janela principal
    ├── HelpForm.cs               # Guia de regras e glossário
    └── Program.cs
```

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

2. Registre em `FrxAnalyzer.cs`:

```csharp
results.AddRange(new MinhaRegra().Check(doc));
```

Nenhuma mudança na UI é necessária — os resultados aparecem automaticamente na tabela.

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
- Análise de código (`Ref-3`, `Code-2`, `Code-3`, `Code-4`) depende do `<ScriptText>` ser C# válido; VB.NET e Delphi não são suportados.
- `Code-4` não detecta acesso indireto via variável intermediária (`var t = Text3; t.Text = "x"`) — apenas acesso direto por nome.
- `Format-1` não analisa TextObjects cujo Text contenha expressões matemáticas (`[[Dados.A] + [Dados.B]]`) ou texto literal misturado com campos (`Protocolo: [Dados.X] - [Dados.Data]`) — nesses casos o `Format` age sobre o valor completo já resolvido, não sobre cada campo individualmente. Use funções de formato na própria expressão: `[Format([Dados.Data], 'dd/MM/yyyy')]`.
- `Format-1` não analisa TextObjects cujo Text contenha chamadas de função (parênteses) para evitar falsos positivos.

---

## Tecnologias

- **.NET 8** — runtime e SDK
- **WinForms** — interface desktop nativa
- **LINQ to XML** (`System.Xml.Linq`) — parsing do `.frx`
- **Microsoft.CodeAnalysis.CSharp** (Roslyn) — análise sintática do ScriptText
