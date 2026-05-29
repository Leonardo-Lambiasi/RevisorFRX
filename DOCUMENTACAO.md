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

### Explorador de Schema

```
Usuário clica "🔍" (habilitado apenas com arquivo único selecionado)
       │
       ▼
MainForm.BtnSchema_Click
       │  File.ReadAllText(path)
       │  XDocument.Parse(xml)
       ▼
SchemaExtractor.Extract(doc)
       │  navega <Dictionary> (Alias-first, recursivo)
       │  extrai List<SchemaField> com EntityAlias + FieldName + DataType
       │  enriquece UsedInComponents via TextObject.Text
       ▼
List<SchemaField>
       │
       ▼
SchemaExplorerForm(fields, fileName).ShowDialog()
       │  filtros em tempo real (texto, tipo, entidade, null-only, used-only)
       │  colorização por DataType
       │  duplo-clique / botão copia FullPath para clipboard
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
```

---

## Como publicar

### Opção 1 — Pasta completa (recomendado para distribuição)

```bash
dotnet publish RevisorFRX.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

Gera em `RevisorFRX.App/bin/Release/net8.0-windows/win-x64/publish/` uma pasta
com `RevisorFRX.exe` + todas as DLLs necessárias. Copie a pasta inteira para o
computador destino. Não precisa de .NET instalado.

### Opção 2 — Arquivo único (portátil, mais lento ao abrir)

```bash
dotnet publish RevisorFRX.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Gera um único `RevisorFRX.exe`. Na primeira execução, descompacta os arquivos
em uma pasta temporária — isso causa lentidão inicial de alguns segundos.

### Compilar no Linux/macOS para Windows

```bash
# Adicionar no RevisorFRX.App/RevisorFRX.App.csproj (já configurado):
# <EnableWindowsTargeting>true</EnableWindowsTargeting>

dotnet publish RevisorFRX.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

O binário gerado roda no Windows mesmo sendo compilado em outro sistema.

### Verificar antes de publicar

```bash
dotnet build RevisorFRX.sln   # deve terminar com 0 erros e 0 avisos
```

---

## Como testar regras manualmente

O projeto `RevisorFRX.TestRunner` (não incluído na solução principal) permite
rodar análise diretamente no terminal:

```bash
cd RevisorFRX.TestRunner
dotnet run
```

Para adicionar um caso de teste: edite o arquivo
`ArquivoFRXTeste/teste_completo.frx` injetando os atributos/código desejados.

---

## Considerações para repositório público

O projeto não contém:
- Strings de conexão com banco de dados
- Nomes de servidores internos
- Credenciais ou tokens
- Caminhos de rede internos

O `RevisorFRX.Core` opera exclusivamente sobre o XML do arquivo `.frx` passado
como string — sem acesso a banco, rede ou sistema de arquivos além do arquivo analisado.
