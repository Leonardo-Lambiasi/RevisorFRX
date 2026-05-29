namespace RevisorFRX.App;

public class HelpForm : Form
{
    public HelpForm()
    {
        SuspendLayout();

        Text = "RevisorFRX — Guia de Regras e Termos";
        ClientSize = new Size(686, 562);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 249, 250);

        var tabControl = new TabControl
        {
            Location = new Point(8, 8),
            Size = new Size(670, 512),
            Font = new Font("Segoe UI", 9F)
        };

        var tabRegras = new TabPage { Text = "Regras", BackColor = Color.White };
        var tabGlossario = new TabPage { Text = "Glossário", BackColor = Color.White };

        tabRegras.Controls.Add(CreateRichTextBox(GetRegrasContent()));
        tabGlossario.Controls.Add(CreateRichTextBox(GetGlossarioContent()));

        tabControl.TabPages.Add(tabRegras);
        tabControl.TabPages.Add(tabGlossario);

        var btnFechar = new Button
        {
            Text = "Fechar",
            Location = new Point(303, 530),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnFechar.FlatAppearance.BorderSize = 0;
        btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { tabControl, btnFechar });
        CancelButton = btnFechar;

        ResumeLayout(false);
    }

    private static RichTextBox CreateRichTextBox(string content) =>
        new RichTextBox
        {
            Text = content,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9F),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

    private static string GetRegrasContent() =>
"""
REGRAS DE ANÁLISE
═════════════════════════════════════════════════════════════

🔴 ERROS — impedem o relatório de funcionar corretamente
═════════════════════════════════════════════════════════════

🔴 Ref-2 — DataSource não declarado                           [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Cada DataBand precisa referenciar uma fonte de dados declarada
  no Dictionary. Se o DataSource não existe, a banda não itera.

Por que é perigoso:
  A banda renderiza vazia ou lança NullReferenceException.

Como corrigir:
  Garantir que existe um BusinessObjectDataSource com o mesmo nome
  no Dictionary.

──────────────────────────────────────────────────────────────

🔴 Ref-1 — MasterComponent inválido                           [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  DataBand filha aponta para uma banda mestre via MasterComponent.
  Se o nome não existe, a banda filha é ignorada.

Como corrigir:
  Corrigir MasterComponent para o Name exato da banda mestre.

──────────────────────────────────────────────────────────────

🔴 Code-2 — Cast direto em Row[] ou .Value sem HasValue       [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Acessar Row["Campo"] retorna object. Cast direto como
  (Boolean)Row["Campo"] lança InvalidCastException se DBNull.
  Acessar .Value em Nullable sem HasValue lança
  InvalidOperationException — trava o relatório.

Detecta QUALQUER value type (int, decimal, struct personalizado,
  etc.), não só os tipos conhecidos.

Exemplos que TRAVAM o relatório:
  bool ativo = (Boolean)Row["Ativo"];
  var data = ((DateTime?)Row["Data"]).Value;  // sem HasValue!

Como corrigir:
  Usar Convert ou verificação de nulo:
  bool ativo = Convert.ToBoolean(Row["Ativo"] ?? false);

──────────────────────────────────────────────────────────────

🔴 Code-3 — Métodos obrigatórios ausentes no ScriptText      [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  O relatório deve conter os métodos utilitários do template padrão:
  • AplicarMascaraDeDocumento
  • ExtrairCaracteresNumericos
  • AplicarMascaraDeCNPJ
  • AplicarMascaraDeCPF

Se ausentes, o relatório foi criado fora do template padrão.

🟡 AVISOS — comportamento inesperado em runtime
═════════════════════════════════════════════════════════════

🟡 Format-1 — Formatação incorreta ou ausente                [AVISO]
──────────────────────────────────────────────────────────────
Decimal sem Format="Currency" ou DateTime sem Format="Date".
O valor aparece no formato do SO, que pode variar por máquina.

Correção:
  Decimal:  Format="Currency" Format.DecimalDigits="2"
  DateTime: Format="Date"

──────────────────────────────────────────────────────────────

🟡 Expr-1 — Campo ausente no schema                          [AVISO]
──────────────────────────────────────────────────────────────
TextObjects com [Dados.Entidade.Campo] onde o campo não existe
no Dictionary.

──────────────────────────────────────────────────────────────

🟡 Expr-2 — Campo não escalar em expressão                   [AVISO]
──────────────────────────────────────────────────────────────
Expressão [Dados.X] referencia um campo com DataType="null"
(objeto não escalar). Deveria ser [Dados.X.Propriedade].

──────────────────────────────────────────────────────────────

🟡 Ref-10 — Colchetes desbalanceados no Text                 [AVISO]
──────────────────────────────────────────────────────────────
Número de [ diferente de ] no atributo Text do TextObject.
A expressão não resolve e vira texto literal.

──────────────────────────────────────────────────────────────

🟡 Format-6 — Tags HTML sem HtmlTags ativado                 [AVISO]
──────────────────────────────────────────────────────────────
TextObject contém <b>, <i> etc. mas TextRenderType não é
"HtmlTags". As tags aparecem como texto literal.

──────────────────────────────────────────────────────────────

🟡 Code-4 — CNPJ alfanumérico                                 [AVISO]
──────────────────────────────────────────────────────────────
O CNPJ pode conter letras. Detecta validações no ScriptText
(Length==14, \d{14}), máscaras ##.###.###/####-## e campos
numéricos no Dictionary — todos precisam ser revisados.

──────────────────────────────────────────────────────────────

🟡 Format-7 — Barcode sem Checksum=false                     [AVISO]
──────────────────────────────────────────────────────────────
BarcodeObject com Barcode.CalcCheckSum diferente de "false".
Checksum habilitado pode gerar códigos de barras inválidos
para leitura.

──────────────────────────────────────────────────────────────

🟡 Ref-12 — CanGrow inconsistente banda vs TextObject        [AVISO]
──────────────────────────────────────────────────────────────
TextObject com CanGrow=true mas a banda pai não. O texto
cresce e sobrepõe componentes abaixo.
""";

    private static string GetGlossarioContent() =>
"""
GLOSSÁRIO DE TERMOS
═════════════════════════════════════════════════════════════

.frx
  Formato de arquivo do FastReport. É um XML que contém toda a
  definição do relatório: layout, dados, script C# e configurações.
  Pode ser aberto no FastReport Designer ou analisado como XML puro.

ScriptText
  Bloco de código C# embutido dentro do arquivo .frx. Contém métodos
  que são chamados pelos eventos dos componentes (BeforePrint, AfterData,
  etc.). É compilado e executado pelo FastReport em runtime durante a
  geração do PDF.

Dictionary
  Seção do .frx que declara todas as fontes de dados disponíveis para
  o relatório. É aqui que ficam os BusinessObjectDataSource com o schema
  completo — todas as entidades e campos que o relatório pode acessar.

BusinessObjectDataSource
  Fonte de dados baseada em um objeto C# (DTO, lista, entidade).
  Representa uma tabela ou lista de dados no relatório. Podem ser
  aninhados — um pode conter outros como filhos, formando uma hierarquia
  que espelha o modelo de dados da aplicação.

TableDataSource / CsvDataSource / ViewDataSource / JsonDataSource
  Outros tipos de fonte de dados que o FastReport suporta. TableDataSource
  para DataTables, CsvDataSource para arquivos CSV, ViewDataSource para
  views de banco, JsonDataSource para APIs REST. O RevisorFRX detecta
  todos eles ao validar referências.

DataBand
  Banda de dados — a faixa do relatório que se repete para cada registro
  da fonte de dados. Se a fonte tem 100 registros, a DataBand renderiza
  100 vezes, uma para cada linha.

MasterComponent
  Propriedade de uma DataBand filha que aponta para a DataBand pai.
  Define a relação mestre-detalhe: para cada linha do mestre, a filha
  renderiza seus registros correspondentes.

CanGrow / CanShrink
  Propriedades que permitem um componente crescer ou encolher
  verticalmente para acomodar seu conteúdo. Se um TextObject tem
  CanGrow=true mas a DataBand pai não, o texto extravasa e sobrepõe
  componentes abaixo. O RevisorFRX flagra essa inconsistência (Ref-12).

BeforePrint / AfterData
  Eventos do ciclo de vida dos componentes FastReport.
  BeforePrint: disparado antes de o componente ser renderizado.
  AfterData: disparado após os dados serem vinculados ao componente.
  Ambos chamam métodos pelo nome — o método deve existir no ScriptText.

Row["Campo"]
  Forma de acessar campos da fonte de dados dentro do ScriptText.
  Retorna object — sempre requer verificação de nulo ou conversão
  segura antes do uso, pois campos anuláveis do banco retornam
  DBNull.Value em vez de null.

DBNull.Value
  Valor especial do .NET que representa um campo nulo vindo do banco
  de dados. É diferente de null — um cast direto em DBNull.Value
  lança InvalidCastException. Sempre verificar antes do cast:
  if (Row["Campo"] != DBNull.Value) { ... }

TextRenderType
  Atributo do TextObject que define como o texto é interpretado.
  "HtmlTags" permite usar tags HTML (<b>, <i>, <font>) no texto do
  relatório. Sem esse atributo, as tags aparecem como texto literal.
  O RevisorFRX flagra essa omissão (Format-6).

Barcode.CalcCheckSum
  Atributo do BarcodeObject que ativa o cálculo de dígito verificador.
  Deve ser false para a maioria dos tipos de código de barras, pois
  o checksum embutido pode gerar códigos inválidos para leitura.
  O RevisorFRX verifica este atributo (Format-7).

Roslyn
  Compilador C# da Microsoft usado internamente pelo RevisorFRX para
  analisar o ScriptText. Em vez de procurar padrões por texto (regex),
  o Roslyn compila o código em memória e inspeciona a árvore sintática
  (AST) — a mesma análise que o Visual Studio faz internamente.

AST (Árvore de Sintaxe Abstrata)
  Representação estruturada do código C# gerada pelo Roslyn. Permite
  navegar pelo código como um grafo — encontrar métodos, blocos catch,
  casts e expressões com precisão cirúrgica, sem depender de regex.

Format / Format.Pattern
  Atributos do TextObject que definem como o valor é apresentado
  no PDF. Format define o tipo de formatação (Currency, Date, Number,
  Boolean). Format.Pattern define o padrão específico para datas
  (ex: "dd/MM/yyyy"). Sem esses atributos, o FastReport usa o formato
  padrão do sistema operacional — que varia entre máquinas e pode
  gerar relatórios com datas em inglês ou valores sem símbolo de moeda.

Schema Explorer
  Ferramenta visual do RevisorFRX (botão 🔍) que exibe todos os campos
  declarados no Dictionary do arquivo .frx. Permite pesquisar por nome,
  filtrar por tipo de dado e entidade, ver quais campos são objetos não
  escalares (DataType=null) e quais são efetivamente usados no relatório.
  Duplo-clique copia o caminho completo para o clipboard.

CNPJ Alfanumérico (IN 2117/2023)
  A Receita Federal passou a permitir letras no CNPJ a partir de julho
  de 2026. Validações que assumem apenas dígitos (\d{14}, Length==14)
  e campos numéricos (Int64, Decimal) precisam ser revisados.
  O RevisorFRX detecta esses padrões (Code-4).
""";
}
