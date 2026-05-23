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

🔴 Ref-3 — Evento sem método no ScriptText                    [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Componentes do FastReport (TextObject, DataBand, etc.) podem disparar
  eventos C# em momentos específicos da renderização, como BeforePrint
  (antes de imprimir o componente) e AfterData (após processar os dados).
  Esses eventos referenciam métodos pelo nome — se o método não existe
  no ScriptText, o evento nunca é executado.

Por que é perigoso:
  O FastReport não lança erro quando o método está ausente. O relatório
  gera normalmente, mas a lógica do evento (visibilidade condicional,
  formatação, cálculos) é silenciosamente ignorada. O PDF sai errado
  sem nenhum aviso.

Exemplo do problema:
  TextObject com AfterDataEvent="CalcularTotal"
  → método CalcularTotal não existe no ScriptText
  → o campo nunca é formatado

Como corrigir:
  Criar o método no ScriptText, ou remover o atributo do componente
  se o evento não for mais necessário.

──────────────────────────────────────────────────────────────

🔴 Ref-2 — DataSource não declarado                           [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Cada DataBand (banda de dados) precisa referenciar uma fonte de dados
  declarada no Dictionary do relatório. Se o DataSource referenciado
  não existe, a banda não itera sobre nenhum dado.

Por que é perigoso:
  A banda renderiza vazia ou lança NullReferenceException em runtime,
  dependendo da versão do FastReport.

Como corrigir:
  Verificar o nome do DataSource na propriedade da DataBand e garantir
  que existe um BusinessObjectDataSource com o mesmo nome no Dictionary.

──────────────────────────────────────────────────────────────

🔴 Ref-1 — MasterComponent inválido                           [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Bandas de detalhe (DataBand filhas) precisam apontar para uma banda
  mestre via MasterComponent. Se o nome referenciado não existe no
  relatório, a banda filha é ignorada na renderização.

Por que é perigoso:
  Dados que deveriam aparecer simplesmente não aparecem. Sem erro,
  sem log — o relatório gera em branco naquela seção.

Como corrigir:
  Corrigir o valor de MasterComponent para o Name exato da banda mestre.

──────────────────────────────────────────────────────────────

🔴 Ref-4 — PrintOnParent com hierarquia incorreta             [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  SubreportObject com PrintOnParent=true renderiza o subrelatório
  inline, na mesma linha da banda pai. Para funcionar corretamente,
  o DataSource da banda do subrelatório deve ser filho (descendente)
  do DataSource da banda pai no schema de dados.

Por que é perigoso:
  Se a hierarquia estiver errada, o FastReport entra em loop tentando
  resolver a referência, causando freeze completo da aplicação ou
  NullReferenceException ao gerar o PDF.

Exemplo correto:
  Banda pai: DataSource="Apontamentos"
  Banda filha: DataSource="Apresentantes"
  → Apresentantes deve ser filho de Apontamentos no Dictionary

Como corrigir:
  Verificar o Dictionary e garantir que o DataSource do subrelatório
  está aninhado sob o DataSource da banda pai.

──────────────────────────────────────────────────────────────

🟡 Layout-1 — CanGrow sem ShiftMode                          [AVISO]
──────────────────────────────────────────────────────────────
O que é:
  Quando um componente tem CanGrow=true, ele pode crescer verticalmente
  para acomodar textos longos. Os componentes abaixo dele precisam ter
  ShiftMode=Shift para serem empurrados para baixo junto com o
  crescimento. Sem isso, o componente crescido sobrepõe os elementos
  abaixo.

Por que é perigoso:
  O relatório gera sem erro mas o layout fica sobreposto — textos e
  campos se misturam visualmente. O problema só aparece com dados reais
  longos, não é visível no Designer com dados de exemplo curtos.

Exemplo do problema:
  Campo "Endereço" com CanGrow=true
  → endereço longo cresce 3 linhas para baixo
  → campo "Bairro" abaixo sem ShiftMode=Shift
  → "Bairro" fica sobreposto pelo endereço no PDF

Como corrigir:
  Adicionar ShiftMode=Shift em todos os componentes abaixo do campo
  que tem CanGrow=true, dentro da mesma banda.

──────────────────────────────────────────────────────────────

🟡 Code-1 — catch vazio no ScriptText                        [AVISO]
──────────────────────────────────────────────────────────────
O que é:
  Um bloco catch sem nenhum código dentro silencia qualquer exceção
  que ocorra no bloco try correspondente. O FastReport continua a
  renderização sem saber que houve um erro.

Por que é perigoso:
  O PDF pode gerar em branco, com valores incorretos ou parcialmente
  preenchido — sem nenhuma mensagem de erro para o usuário ou log
  para diagnóstico.

Exemplo do problema:
  try { txtValor.Text = CalcularValor(); }
  catch { }  ← exceção engolida silenciosamente

Como corrigir:
  Nunca deixar catch vazio. No mínimo, registrar o erro:
  catch (Exception ex) { /* log ou mensagem */ }

──────────────────────────────────────────────────────────────

🔴 Code-2 — Cast direto em Row[] sem verificação de nulo      [ERRO]
──────────────────────────────────────────────────────────────
O que é:
  Acessar um campo do banco de dados via Row["Campo"] retorna object.
  Fazer cast direto como (Boolean)Row["Campo"] ou
  (DateTime)Row["Campo"] lança InvalidCastException se o campo vier
  nulo do banco.

Por que é perigoso:
  Campos anuláveis no banco (nullable) podem vir DBNull.Value.
  O cast direto quebra o relatório inteiro nesse caso — nenhuma
  linha é renderizada a partir do ponto do erro.

Exemplo do problema:
  bool ativo = (Boolean)Row["Ativo"];
  → se Ativo for null no banco → InvalidCastException → PDF em branco

Como corrigir:
  Usar Convert ou verificação de nulo:
  bool ativo = Row["Ativo"] != DBNull.Value && (Boolean)Row["Ativo"];
  // ou
  bool ativo = Convert.ToBoolean(Row["Ativo"] ?? false);

──────────────────────────────────────────────────────────────

🟡 Expr-1 — Campo ausente no schema                          [AVISO]
──────────────────────────────────────────────────────────────
O que é:
  Expressões nos TextObjects seguem o padrão [Dados.Entidade.Campo].
  Se o campo referenciado não existe no Dictionary do relatório,
  o FastReport renderiza o campo vazio ou lança exceção em runtime.

Por que é perigoso:
  O campo aparece em branco no PDF sem nenhum aviso. Em casos mais
  graves, o relatório inteiro para de renderizar.

Como corrigir:
  Verificar o nome exato do campo no Dictionary do FastReport Designer
  e corrigir a expressão no TextObject.

──────────────────────────────────────────────────────────────

🟡 Format-1 — Formatação incorreta ou ausente                [AVISO]
──────────────────────────────────────────────────────────────
O que é:
  TextObjects que exibem campos Decimal ou DateTime devem ter o
  atributo Format configurado corretamente. Sem ele, o FastReport
  exibe o valor no formato padrão do sistema operacional — que pode
  ser diferente do esperado dependendo da máquina onde o relatório
  é gerado.

Casos detectados:
  • Campo Decimal sem Format → exibe sem símbolo de moeda e sem
    casas decimais fixas
  • Campo Decimal com Format de data ou booleano → dado exibido
    de forma completamente errada
  • Campo DateTime sem Format → exibe data e hora juntos no
    formato do SO (pode vir MM/dd/yyyy em máquinas em inglês)
  • Campo DateTime com Format="Date" mas sem Format.Pattern →
    padrão pode variar entre ambientes
  • Campo DateTime com Format de moeda ou número → dado errado

Como corrigir:
  Para Decimal:
    Format="Currency" Format.DecimalDigits="2" Format.UseLocale="true"
  Para DateTime:
    Format="Date" Format.Pattern="dd/MM/yyyy"
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

DataBand
  Banda de dados — a faixa do relatório que se repete para cada registro
  da fonte de dados. Se a fonte tem 100 registros, a DataBand renderiza
  100 vezes, uma para cada linha.

MasterComponent
  Propriedade de uma DataBand filha que aponta para a DataBand pai.
  Define a relação mestre-detalhe: para cada linha do mestre, a filha
  renderiza seus registros correspondentes.

SubreportObject
  Componente que incorpora outro relatório (outra ReportPage) dentro
  do relatório atual. Permite reutilizar layouts e criar seções
  complexas com dados de fontes diferentes.

PrintOnParent
  Propriedade do SubreportObject. Quando true, o subrelatório é
  renderizado inline na mesma linha da banda pai, em vez de em uma
  área separada. Usado para exibir listas dentro de linhas —
  por exemplo, mostrar os apresentantes de um apontamento na mesma
  linha do apontamento.

CanGrow
  Propriedade de componentes de texto. Quando true, o componente
  cresce verticalmente para acomodar o conteúdo. Se false, o texto
  é cortado no tamanho fixo definido no Designer.

ShiftMode
  Define o comportamento de um componente quando o elemento acima
  dele cresce (CanGrow). ShiftMode=Shift empurra o componente para
  baixo. Sem ShiftMode, o componente fica na posição fixa e é
  sobreposto pelo elemento que cresceu.

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
""";
}
