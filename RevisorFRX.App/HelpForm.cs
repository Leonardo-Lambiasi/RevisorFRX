namespace RevisorFRX.App;

public class HelpForm : Form
{
    public HelpForm()
    {
        SuspendLayout();

        Text = "RevisorFRX — Guia de uso";
        ClientSize = new Size(700, 560);
        MinimumSize = new Size(600, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 249, 250);

        var tabControl = new TabControl
        {
            Location = new Point(8, 8),
            Size = new Size(684, 506),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.5F),
            BackColor = Color.FromArgb(248, 249, 250),
        };

        var tabGeral  = CriarAba("  Visão Geral  ");
        var tabPassos = CriarAba("  Passo a Passo  ");
        var tabSchema = CriarAba("  Busca de Dados 🔍  ");
        var tabGloss  = CriarAba("  Glossário  ");

        var rtbGeral  = CriarRtb();
        var rtbPassos = CriarRtb();
        var rtbSchema = CriarRtb();
        var rtbGloss  = CriarRtb();

        tabGeral.Controls.Add(rtbGeral);
        tabPassos.Controls.Add(rtbPassos);
        tabSchema.Controls.Add(rtbSchema);
        tabGloss.Controls.Add(rtbGloss);

        PreencherGeral(rtbGeral);
        PreencherPassos(rtbPassos);
        PreencherSchema(rtbSchema);
        PreencherGlossario(rtbGloss);

        // Congela edição depois de popular
        rtbGeral.ReadOnly  = true;
        rtbPassos.ReadOnly = true;
        rtbSchema.ReadOnly = true;
        rtbGloss.ReadOnly  = true;

        // Scroll para o topo em cada aba
        foreach (var rtb in new[] { rtbGeral, rtbPassos, rtbSchema, rtbGloss })
        {
            rtb.SelectionStart = 0;
            rtb.ScrollToCaret();
        }

        tabControl.TabPages.AddRange(new[] { tabGeral, tabPassos, tabSchema, tabGloss });

        var btnFechar = new Button
        {
            Text = "Fechar",
            Location = new Point(612, 522),
            Size = new Size(80, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
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

    // ── Construtores de controles ─────────────────────────────

    private static TabPage CriarAba(string title) => new TabPage
    {
        Text = title,
        BackColor = Color.FromArgb(248, 249, 250),
        Padding = new Padding(0),
    };

    private static RichTextBox CriarRtb() => new RichTextBox
    {
        ReadOnly = false,
        BackColor = Color.FromArgb(248, 249, 250),
        ForeColor = Color.FromArgb(33, 37, 41),
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10F),
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Dock = DockStyle.Fill,
        DetectUrls = false,
    };

    // ── Paleta de cores ───────────────────────────────────────

    private static readonly Color ClrTitulo   = Color.FromArgb(13, 110, 253);
    private static readonly Color ClrSub      = Color.FromArgb(13, 110, 253);
    private static readonly Color ClrTexto    = Color.FromArgb(33, 37, 41);
    private static readonly Color ClrDimmed   = Color.FromArgb(108, 117, 125);
    private static readonly Color ClrErro     = Color.FromArgb(180, 35, 35);
    private static readonly Color ClrAviso    = Color.FromArgb(160, 100, 0);
    private static readonly Color ClrOk       = Color.FromArgb(25, 135, 84);
    private static readonly Color ClrDestaque = Color.FromArgb(13, 110, 253);
    private static readonly Color ClrSep      = Color.FromArgb(222, 226, 230);

    // ── Helpers de formatação ─────────────────────────────────

    private static void Sel(RichTextBox rtb, Font font, Color cor)
    {
        rtb.SelectionStart  = rtb.TextLength;
        rtb.SelectionLength = 0;
        rtb.SelectionFont   = font;
        rtb.SelectionColor  = cor;
    }

    private static void H1(RichTextBox rtb, string text)
    {
        Sel(rtb, new Font("Segoe UI", 13F, FontStyle.Bold), ClrTitulo);
        rtb.AppendText(text + "\n\n");
    }

    private static void H2(RichTextBox rtb, string text)
    {
        Sel(rtb, new Font("Segoe UI", 10.5F, FontStyle.Bold), ClrSub);
        rtb.AppendText(text + "\n");
    }

    private static void Body(RichTextBox rtb, string text)
    {
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText(text + "\n");
    }

    private static void Dim(RichTextBox rtb, string text)
    {
        Sel(rtb, new Font("Segoe UI", 9.5F, FontStyle.Italic), ClrDimmed);
        rtb.AppendText(text + "\n");
    }

    private static void Blank(RichTextBox rtb, int lines = 1)
    {
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText(new string('\n', lines));
    }

    private static void Sep(RichTextBox rtb)
    {
        Sel(rtb, new Font("Segoe UI", 7F), ClrSep);
        rtb.AppendText("  " + new string('─', 78) + "\n\n");
    }

    // Linha com ícone colorido + texto
    private static void Bullet(RichTextBox rtb, string icon, Color iconColor, string text)
    {
        Sel(rtb, new Font("Segoe UI", 10F, FontStyle.Bold), iconColor);
        rtb.AppendText("  " + icon + " ");
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText(text + "\n");
    }

    // Rótulo em negrito seguido de corpo em tom normal
    private static void BulletNegrito(RichTextBox rtb, string rotulo, string corpo)
    {
        Sel(rtb, new Font("Segoe UI", 10F, FontStyle.Bold), ClrTexto);
        rtb.AppendText("  " + rotulo);
        Sel(rtb, new Font("Segoe UI", 10F), ClrDimmed);
        rtb.AppendText(" " + corpo + "\n");
    }

    // Passo numerado: título em destaque, corpo indentado
    private static void Passo(RichTextBox rtb, int n, string titulo, string corpo)
    {
        Sel(rtb, new Font("Segoe UI", 10.5F, FontStyle.Bold), ClrSub);
        rtb.AppendText($"\n  {n}.  {titulo}\n");
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText("      " + corpo + "\n");
    }

    // Chip colorido (ex: 🔴 ERRO) + descrição
    private static void Chip(RichTextBox rtb, string rotulo, Color cor, string descricao)
    {
        Sel(rtb, new Font("Segoe UI", 10.5F, FontStyle.Bold), cor);
        rtb.AppendText("  " + rotulo + "\n");
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText("  " + descricao + "\n\n");
    }

    // Termo do glossário: nome em destaque, definição indentada
    private static void Termo(RichTextBox rtb, string nome, string def)
    {
        Sel(rtb, new Font("Segoe UI", 10F, FontStyle.Bold), ClrSub);
        rtb.AppendText("  " + nome + "\n");
        Sel(rtb, new Font("Segoe UI", 10F), ClrTexto);
        rtb.AppendText("      " + def + "\n\n");
    }

    // ── Conteúdo das abas ─────────────────────────────────────

    private static void PreencherGeral(RichTextBox rtb)
    {
        Blank(rtb);
        H1(rtb, "  O que é o RevisorFRX");

        Body(rtb, "  O RevisorFRX analisa arquivos de relatório (.frx) do FastReport em busca de");
        Body(rtb, "  problemas que podem fazer o relatório falhar ou exibir dados incorretos.");
        Body(rtb, "  Você não precisa abrir o FastReport Designer — basta selecionar o arquivo");
        Body(rtb, "  e clicar em Analisar.");

        Sep(rtb);
        H2(rtb, "  Quando usar");
        Blank(rtb);
        Bullet(rtb, "•", ClrSub, "Antes de entregar um relatório novo para produção");
        Bullet(rtb, "•", ClrSub, "Quando um relatório está gerando erro ou exibindo dados errados");
        Bullet(rtb, "•", ClrSub, "Ao receber um .frx de outro time para validar");
        Bullet(rtb, "•", ClrSub, "Como verificação de rotina após alterações no modelo de dados");

        Sep(rtb);
        H2(rtb, "  O que ele NÃO faz");
        Blank(rtb);
        Bullet(rtb, "•", ClrDimmed, "Não abre nem modifica o arquivo .frx");
        Bullet(rtb, "•", ClrDimmed, "Não se conecta ao banco de dados ou ao sistema");
        Bullet(rtb, "•", ClrDimmed, "Não gera relatórios nem PDFs");
        Bullet(rtb, "•", ClrDimmed, "Não detecta erros de conteúdo (ex: valores incorretos nos dados de origem)");

        Sep(rtb);
        H2(rtb, "  Os dois tipos de resultado");
        Blank(rtb);

        Chip(rtb, "🔴  ERRO",
            ClrErro,
            "Problema grave que impede o relatório de funcionar corretamente.\n" +
            "  O relatório pode travar, gerar uma tela de erro ou renderizar em branco.\n" +
            "  Corrija antes de publicar o relatório em produção.");

        Chip(rtb, "🟡  AVISO",
            ClrAviso,
            "Comportamento inesperado que pode não causar falha imediata, mas produz\n" +
            "  resultados incorretos em situações específicas.\n" +
            "  Exemplo: valor monetário aparece como \"1234.56\" em vez de \"R$ 1.234,56\".");
    }

    private static void PreencherPassos(RichTextBox rtb)
    {
        Blank(rtb);
        H1(rtb, "  Como usar o RevisorFRX");

        Passo(rtb, 1, "Selecionar o arquivo ou pasta",
            "Clique em \"Selecionar arquivo .frx\" para analisar um único relatório.\n" +
            "      Ou clique em \"Selecionar pasta\" para analisar todos os .frx de uma pasta de\n" +
            "      uma vez — útil para varreduras periódicas.");

        Passo(rtb, 2, "Clicar em Analisar",
            "A análise normalmente leva menos de 5 segundos por arquivo.\n" +
            "      Durante a análise de pasta, a barra de progresso indica o andamento.\n" +
            "      Para interromper, clique em \"✕ Cancelar\".");

        Passo(rtb, 3, "Ler os resultados no grid",
            "Linhas vermelhas = Erros  |  Linhas amarelas = Avisos.\n" +
            "      Os resultados são ordenados por severidade (Erros primeiro).");

        Passo(rtb, 4, "Corrigir os problemas",
            "Abra o .frx no FastReport Designer. Use a coluna \"Componente\" para localizar\n" +
            "      o elemento com problema, e \"Mensagem\" + \"Detalhe\" para entender o que corrigir.\n" +
            "      Para dúvidas sobre o que cada regra significa, consulte a aba \"Visão Geral\".");

        Passo(rtb, 5, "Exportar o relatório",
            "Clique em \"Exportar relatório CSV\" para salvar os resultados como planilha Excel.\n" +
            "      O CSV inclui todas as colunas do grid e pode ser compartilhado com o time.");

        Sep(rtb);
        H2(rtb, "  O que significa cada coluna do grid");
        Blank(rtb);
        BulletNegrito(rtb, "Regra",      "— código da verificação aplicada (ex: Ref-2, Format-1)");
        BulletNegrito(rtb, "Severidade", "— Error (vermelho) ou Warning (amarelo)");
        BulletNegrito(rtb, "Arquivo",    "— nome do .frx analisado (útil em análise de pasta)");
        BulletNegrito(rtb, "Componente", "— nome do TextObject, DataBand ou método com o problema");
        BulletNegrito(rtb, "Mensagem",   "— descrição clara do problema encontrado");
        BulletNegrito(rtb, "Detalhe",    "— informação adicional para localizar o problema no Designer");

        Sep(rtb);
        H2(rtb, "  Botões da tela principal");
        Blank(rtb);
        BulletNegrito(rtb, "⚙",
            "Configurações — ativa ou desativa regras individualmente");
        BulletNegrito(rtb, "🔍",
            "Explorador de Schema — lista todos os campos disponíveis no .frx (ver aba \"Busca de Dados\")");
        BulletNegrito(rtb, "Exportar CSV",
            "Salva todos os resultados do grid como planilha Excel");
        BulletNegrito(rtb, "✕ Cancelar",
            "Interrompe a análise em andamento (aparece apenas durante análise de pasta)");

        Sep(rtb);
        H2(rtb, "  As 12 verificações realizadas");
        Blank(rtb);

        Sel(rtb, new Font("Segoe UI", 10F, FontStyle.Bold), ClrErro);
        rtb.AppendText("  Erros — impedem o relatório de funcionar\n");
        BulletNegrito(rtb, "  Ref-2",   "DataSource referencia uma fonte não declarada no Dictionary");
        BulletNegrito(rtb, "  Ref-1",   "MasterComponent aponta para um componente que não existe");
        BulletNegrito(rtb, "  Code-2",  "Cast sem verificação de nulo pode travar o relatório");
        BulletNegrito(rtb, "  Code-3",  "Métodos utilitários obrigatórios ausentes no código embutido");
        Blank(rtb);

        Sel(rtb, new Font("Segoe UI", 10F, FontStyle.Bold), ClrAviso);
        rtb.AppendText("  Avisos — comportamento inesperado em runtime\n");
        BulletNegrito(rtb, "  Format-1", "Decimal sem formato de moeda ou DateTime sem formato de data");
        BulletNegrito(rtb, "  Format-6", "Tags HTML presentes mas o modo HtmlTags não está ativado");
        BulletNegrito(rtb, "  Format-7", "Código de barras com checksum habilitado incorretamente");
        BulletNegrito(rtb, "  Expr-1",   "Expressão referencia campo que não existe no schema");
        BulletNegrito(rtb, "  Expr-2",   "Expressão usa um campo do tipo objeto, não um valor simples");
        BulletNegrito(rtb, "  Ref-12",   "TextObject pode crescer verticalmente mas a banda pai não");
        BulletNegrito(rtb, "  Code-4",   "CNPJ tratado como apenas dígitos — novo formato permite letras");
        BulletNegrito(rtb, "  Ref-10",   "Colchetes desbalanceados em uma expressão de campo");
        Blank(rtb);
    }

    private static void PreencherSchema(RichTextBox rtb)
    {
        Blank(rtb);
        H1(rtb, "  Explorador de Schema  (botão 🔍)");

        H2(rtb, "  O que é");
        Blank(rtb);
        Body(rtb, "  Todo arquivo .frx contém uma lista interna de campos disponíveis —");
        Body(rtb, "  como uma tabela que o FastReport pode consultar para preencher o relatório.");
        Body(rtb, "  Essa lista inclui campos do cartório, títulos, devedores, endereços e mais.");
        Blank(rtb);
        Body(rtb, "  O Explorador de Schema (botão 🔍) mostra essa lista de forma navegável,");
        Body(rtb, "  com busca e filtros. É útil quando você precisa saber o caminho exato de");
        Body(rtb, "  um campo para usar em uma expressão do relatório.");

        Sep(rtb);
        H2(rtb, "  Como usar a busca");
        Blank(rtb);
        Bullet(rtb, "•", ClrSub,
            "Digite parte do nome na caixa de busca:  \"valor\",  \"nome\",  \"CEP\"...");
        Bullet(rtb, "•", ClrSub,
            "Filtro \"Tipo\" — exibe apenas Decimal, DateTime, String, Int etc.");
        Bullet(rtb, "•", ClrSub,
            "Filtro \"Entidade\" — exibe apenas campos de um grupo (ex: só do Devedor)");
        Bullet(rtb, "•", ClrSub,
            "Filtro \"Status\" — \"Disponível\" (não usado no layout) ou \"Em uso\" (já referenciado)");
        Blank(rtb);
        Body(rtb, "  Duplo-clique em uma linha — ou selecione e pressione Enter — para copiar");
        Body(rtb, "  o caminho completo do campo:");
        Blank(rtb);
        Sel(rtb, new Font("Segoe UI", 10.5F, FontStyle.Bold), ClrDestaque);
        rtb.AppendText("      [Dados.Titulo.ValorTotal]\n\n");
        Body(rtb, "  Esse caminho pode ser colado diretamente em um TextObject no FastReport Designer.");

        Sep(rtb);
        H2(rtb, "  Status dos campos");
        Blank(rtb);
        Chip(rtb, "✅  Em uso", ClrOk,
            "O campo já aparece em algum TextObject do relatório. Tudo certo.");
        Chip(rtb, "⚪  Disponível", ClrDimmed,
            "O campo está mapeado no código, mas não está sendo exibido no layout.\n\n" +
            "  Se precisar usar esse campo: copie o caminho e cole em um TextObject no Designer.\n\n" +
            "  Se o campo que você procura não aparecer nem em \"Disponível\": provavelmente ele\n" +
            "  não está mapeado no código — nesse caso, acione o time de projetos.");

        Sep(rtb);
        H2(rtb, "  Por que a lista não mostra todos os campos?");
        Blank(rtb);
        Body(rtb, "  Um relatório típico pode ter entre 8.000 e 26.000 campos mapeados pelo sistema.");
        Body(rtb, "  A maioria são objetos internos — metadados de auditoria, objetos de usuário,");
        Body(rtb, "  validações do framework — que nunca aparecem em expressões de relatório.");
        Blank(rtb);
        Body(rtb, "  O filtro padrão (profundidade 5) exibe apenas os campos realmente utilizáveis");
        Body(rtb, "  e esconde o ruído técnico. Resultado: de ~26.000 para ~3.700 campos visíveis —");
        Body(rtb, "  muito mais navegável, sem perder nenhum campo de negócio.");

        Sep(rtb);
        H2(rtb, "  Configurações avançadas  (botão ⚙ Avançado)");
        Blank(rtb);
        BulletNegrito(rtb, "  Campo não aparece na lista?",
            "Aumente a Profundidade de 5 para 6 ou 7 e clique em \"Reaplicar\".");
        BulletNegrito(rtb, "  \"ValidationResult\" excluído por padrão:",
            "Mecanismo interno do sistema de dados, nunca aparece em expressões de relatório.");
        BulletNegrito(rtb, "  \"Restaurar padrões\":",
            "Volta para Profundidade = 5 e exclui ValidationResult. Configuração recomendada.");
        Blank(rtb);
        Dim(rtb, "  Dica: use o filtro \"Disponível\" para ver todos os campos mapeados no código que");
        Dim(rtb, "  ainda não estão sendo exibidos no layout — útil para identificar lacunas.");
        Blank(rtb);
    }

    private static void PreencherGlossario(RichTextBox rtb)
    {
        Blank(rtb);
        H1(rtb, "  Glossário de Termos");

        Termo(rtb, ".frx",
            "Arquivo de relatório do FastReport. É um XML que contém o layout completo,\n" +
            "      os dados disponíveis, o código C# embutido e as configurações.");

        Termo(rtb, "Dictionary / Schema",
            "Seção do .frx que declara todas as fontes de dados e seus campos. É aqui\n" +
            "      que ficam as entidades e os campos que o relatório pode acessar.");

        Termo(rtb, "TextObject",
            "Campo de texto dentro do relatório. Pode conter texto estático, uma expressão\n" +
            "      [Dados.Entidade.Campo] ou uma combinação dos dois.");

        Termo(rtb, "DataBand",
            "Faixa de dados que se repete para cada registro da fonte de dados. Se a fonte\n" +
            "      tem 100 registros, a DataBand renderiza 100 vezes, uma por linha.");

        Termo(rtb, "Expressão  [Dados.X.Y]",
            "Forma de referenciar um campo do Dictionary dentro de um TextObject.\n" +
            "      Exemplo: [Dados.Titulo.ValorTotal] exibe o valor total do título.");

        Termo(rtb, "DataSource",
            "Fonte de dados que alimenta uma DataBand. Deve estar declarada no Dictionary\n" +
            "      para que o FastReport saiba de onde buscar os registros.");

        Termo(rtb, "MasterComponent",
            "Vínculo entre uma DataBand filha e sua DataBand pai, definindo a relação\n" +
            "      mestre-detalhe. Para cada linha do mestre, a filha renderiza seus registros.");

        Termo(rtb, "CanGrow",
            "Propriedade que permite um TextObject crescer verticalmente quando o conteúdo\n" +
            "      for maior que o espaço definido. A DataBand pai também precisa ter\n" +
            "      CanGrow=true — caso contrário, o texto extravasa e sobrepõe o próximo campo.");

        Termo(rtb, "ScriptText",
            "Bloco de código C# embutido no .frx. Contém métodos chamados pelos eventos\n" +
            "      do relatório (BeforePrint, AfterData, etc.). É compilado e executado pelo\n" +
            "      FastReport em tempo de execução, durante a geração do PDF.");

        Termo(rtb, "Row[\"Campo\"]",
            "Forma de acessar campos da fonte de dados dentro do ScriptText. Retorna\n" +
            "      object — sempre requer verificação de nulo antes de usar, pois campos\n" +
            "      anuláveis do banco retornam DBNull.Value em vez de null (Code-2).");

        Termo(rtb, "HtmlTags",
            "Modo de renderização de TextObject que interpreta tags HTML (<b>, <i>, <font>).\n" +
            "      Sem esse modo ativado, as tags aparecem como texto literal no PDF (Format-6).");

        Termo(rtb, "Barcode.CalcCheckSum",
            "Atributo do BarcodeObject que ativa o cálculo de dígito verificador. Deve ser\n" +
            "      \"false\" na maioria dos casos — o checksum embutido pode gerar códigos\n" +
            "      inválidos para leitura em leitores ópticos (Format-7).");

        Termo(rtb, "CNPJ Alfanumérico  (IN 2117/2023)",
            "A Receita Federal passou a permitir letras no CNPJ a partir de julho de 2026.\n" +
            "      Validações que assumem apenas dígitos (\\d{14}, Length==14) e campos numéricos\n" +
            "      (Int64, Decimal) precisam ser revisados. O RevisorFRX detecta esses padrões (Code-4).");

        Termo(rtb, "Severidade Error  (🔴)",
            "Problema grave que impede o relatório de funcionar. Corrigir antes de publicar.");

        Termo(rtb, "Severidade Warning  (🟡)",
            "Comportamento inesperado em runtime. O relatório funciona mas pode exibir\n" +
            "      dados de forma incorreta ou inesperada em determinadas situações.");
    }
}
