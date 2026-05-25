using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;

namespace RevisorFRX.App;

public class MainForm : Form
{
    private string? _selectedFilePath;
    private List<RuleResult> _results = new();

    private readonly Label _fileLabel;
    private readonly Button _analyzeButton;
    private readonly Panel _badgePanel;
    private readonly Label _errorsLabel;
    private readonly Label _warningsLabel;
    private readonly Label _infoLabel;
    private readonly DataGridView _grid;
    private readonly Button _exportButton;
    private readonly Button _btnAjuda;

    public MainForm()
    {
        SuspendLayout();

        Text = "RevisorFRX";
        ClientSize = new Size(784, 561);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 249, 250);

        // --- Título ---
        var titleLabel = new Label
        {
            Text = "RevisorFRX",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Location = new Point(16, 12),
            AutoSize = true,
            ForeColor = Color.FromArgb(33, 37, 41)
        };

        var subtitleLabel = new Label
        {
            Text = "Análise estática de relatórios FastReport (.frx)",
            Font = new Font("Segoe UI", 9),
            Location = new Point(18, 52),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        _btnAjuda = new Button
        {
            Text = "?",
            Location = new Point(740, 16),
            Size = new Size(28, 28),
            Font = new Font("Arial", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Help,
            UseVisualStyleBackColor = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnAjuda.FlatAppearance.BorderSize = 0;
        _btnAjuda.Click += BtnAjuda_Click;

        // --- Linha de seleção de arquivo ---
        var selectButton = new Button
        {
            Text = "Selecionar arquivo .frx",
            Location = new Point(16, 80),
            Size = new Size(185, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        selectButton.FlatAppearance.BorderSize = 0;
        selectButton.Click += SelectButton_Click;

        _fileLabel = new Label
        {
            Text = "Nenhum arquivo selecionado",
            Location = new Point(210, 87),
            Size = new Size(450, 18),
            ForeColor = Color.Gray,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _analyzeButton = new Button
        {
            Text = "Analisar",
            Location = new Point(666, 80),
            Size = new Size(102, 30),
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _analyzeButton.FlatAppearance.BorderSize = 0;
        _analyzeButton.Click += AnalyzeButton_Click;

        // --- Separador ---
        var separator = new Panel
        {
            Location = new Point(16, 120),
            Size = new Size(752, 1),
            BackColor = Color.FromArgb(222, 226, 230)
        };

        // --- Badges ---
        _badgePanel = new Panel
        {
            Location = new Point(16, 128),
            Size = new Size(752, 40),
            Visible = false,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        var errorBadge   = CreateBadge(0,   Color.FromArgb(220, 53, 69),  Color.White,                  out _errorsLabel);
        var warningBadge = CreateBadge(130, Color.FromArgb(255, 193, 7),  Color.FromArgb(33, 37, 41),   out _warningsLabel);
        var infoBadge    = CreateBadge(260, Color.FromArgb(13, 202, 240), Color.FromArgb(33, 37, 41),   out _infoLabel);
        _badgePanel.Controls.AddRange(new Control[] { errorBadge, warningBadge, infoBadge });

        // --- DataGridView ---
        _grid = new DataGridView
        {
            Location = new Point(16, 178),
            Size = new Size(752, 340),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(222, 226, 230),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.DefaultCellStyle.Padding = new Padding(4, 3, 4, 3);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 200, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(233, 236, 239);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
        _grid.EnableHeadersVisualStyles = false;

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Regra",      HeaderText = "Regra",      Width = 75  },
            new DataGridViewTextBoxColumn { Name = "Severidade", HeaderText = "Severidade", Width = 85  },
            new DataGridViewTextBoxColumn { Name = "Componente", HeaderText = "Componente", Width = 150 },
            new DataGridViewTextBoxColumn { Name = "Mensagem",   HeaderText = "Mensagem",   Width = 235 },
            new DataGridViewTextBoxColumn { Name = "Detalhe",    HeaderText = "Detalhe",    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
        );

        // --- Rodapé ---
        _exportButton = new Button
        {
            Text = "Exportar relatório CSV",
            Location = new Point(16, 528),
            Size = new Size(185, 30),
            Visible = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _exportButton.FlatAppearance.BorderSize = 0;
        _exportButton.Click += ExportButton_Click;

        Controls.AddRange(new Control[]
        {
            titleLabel, subtitleLabel, _btnAjuda,
            selectButton, _fileLabel, _analyzeButton,
            separator, _badgePanel, _grid, _exportButton
        });

        ResumeLayout(false);
    }

    private static Panel CreateBadge(int x, Color bgColor, Color fgColor, out Label label)
    {
        var panel = new Panel
        {
            Location = new Point(x, 0),
            Size = new Size(120, 36),
            BackColor = bgColor
        };
        label = new Label
        {
            Text = "—",
            ForeColor = fgColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        panel.Controls.Add(label);
        return panel;
    }

    private void SelectButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "FastReport (*.frx)|*.frx|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo .frx"
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _selectedFilePath = dialog.FileName;
            _fileLabel.Text = Path.GetFileName(_selectedFilePath);
            _fileLabel.ForeColor = Color.FromArgb(33, 37, 41);
            _analyzeButton.Enabled = true;
        }
    }

    private async void AnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        try
        {
            _analyzeButton.Enabled = false;
            _analyzeButton.Text = "Analisando...";
            Cursor = Cursors.WaitCursor;

            var content = File.ReadAllText(_selectedFilePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("O arquivo selecionado está vazio.", "RevisorFRX",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _results = await Task.Run(() => new FrxAnalyzer().Analyze(content));

            PopulateGrid();
            UpdateBadges();
            _badgePanel.Visible = true;
            _exportButton.Visible = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao analisar o arquivo:\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _analyzeButton.Enabled = true;
            _analyzeButton.Text = "Analisar";
            Cursor = Cursors.Default;
        }
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();
        foreach (var r in _results)
        {
            var idx = _grid.Rows.Add(r.RuleCode, r.Severity, r.ComponentName, r.Message, r.Detail);
            _grid.Rows[idx].DefaultCellStyle.BackColor = r.Severity switch
            {
                Severity.Error   => Color.FromArgb(255, 220, 220),
                Severity.Warning => Color.FromArgb(255, 243, 205),
                Severity.Info    => Color.FromArgb(207, 226, 255),
                _                => Color.White
            };
        }
    }

    private void UpdateBadges()
    {
        var e = _results.Count(r => r.Severity == Severity.Error);
        var w = _results.Count(r => r.Severity == Severity.Warning);
        var i = _results.Count(r => r.Severity == Severity.Info);
        _errorsLabel.Text   = $"{e} Erro{(e != 1 ? "s" : "")}";
        _warningsLabel.Text = $"{w} Aviso{(w != 1 ? "s" : "")}";
        _infoLabel.Text     = $"{i} Info";
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = Path.GetFileNameWithoutExtension(_selectedFilePath ?? "relatorio");
        var suggestedName = $"RevisorFRX_{baseName}_{timestamp}.csv";

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
            FileName = suggestedName,
            Title = "Exportar relatório CSV"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        ExportarCsv(_results, dialog.FileName);
        MessageBox.Show("Relatório exportado com sucesso!", "RevisorFRX",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ExportarCsv(List<RuleResult> results, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Regra;Severidade;Componente;Mensagem;Detalhe");

        foreach (var r in results)
        {
            var linha = string.Join(";", new[]
            {
                Escapar(r.RuleCode),
                Escapar(r.Severity.ToString()),
                Escapar(r.ComponentName),
                Escapar(r.Message),
                Escapar(r.Detail)
            });
            sb.AppendLine(linha);
        }

        File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
    }

    private static string Escapar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n'))
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        return valor;
    }

    private void BtnAjuda_Click(object? sender, EventArgs e)
    {
        using var help = new HelpForm();
        help.ShowDialog(this);
    }
}
