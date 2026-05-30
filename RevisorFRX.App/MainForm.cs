using System.Threading;
using System.Xml.Linq;
using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;

namespace RevisorFRX.App;

public class MainForm : Form
{
    private string? _selectedFilePath;
    private string? _selectedFolderPath;
    private string[]? _frxFiles;
    private List<RuleResult> _results = new();
    private RuleConfig _ruleConfig = RuleConfig.DefaultUI();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _batchCts;
    private bool _analisandoArquivo;

    private static readonly Color _colorBtnGreen    = Color.FromArgb(25, 135, 84);
    private static readonly Color _colorBtnDisabled = Color.FromArgb(150, 150, 150);

    private readonly Label _fileLabel;
    private readonly Button _analyzeButton;
    private readonly Panel _badgePanel;
    private readonly Label _errorsLabel;
    private readonly Label _warningsLabel;
    private readonly DataGridView _grid;
    private readonly Button _exportButton;
    private readonly Button _btnAjuda;
    private readonly Button _btnConfig;
    private readonly Button _btnSchema;
    private readonly Button _btnCancelar;
    private readonly ListBox _frxListBox;
    private readonly ProgressBar _progressBar;
    private readonly Label _lblProgress;
    private readonly ToolTip _toolTip = new();

    public MainForm()
    {
        SuspendLayout();

        Text = "RevisorFRX";
        ClientSize = new Size(784, 650);
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

        _btnSchema = new Button
        {
            Text = "🔍",
            Location = new Point(672, 16),
            Size = new Size(28, 28),
            Font = new Font("Segoe UI", 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = _colorBtnDisabled,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSchema.FlatAppearance.BorderSize = 0;
        _btnSchema.Click += BtnSchema_Click;
        _toolTip.SetToolTip(_btnSchema, "Explorador de Schema — visualize e pesquise todos os campos do Dictionary deste .frx");

        _btnConfig = new Button
        {
            Text = "⚙",
            Location = new Point(706, 16),
            Size = new Size(28, 28),
            Font = new Font("Segoe UI", 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnConfig.FlatAppearance.BorderSize = 0;
        _btnConfig.Click += BtnConfig_Click;
        _toolTip.SetToolTip(_btnConfig, "Configurar regras ativas");

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
        _toolTip.SetToolTip(_btnAjuda, "Guia de regras e glossário de termos FastReport");

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
            Size = new Size(300, 18),
            ForeColor = Color.Gray,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var batchButton = new Button
        {
            Text = "Selecionar pasta",
            Location = new Point(520, 80),
            Size = new Size(145, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        batchButton.FlatAppearance.BorderSize = 0;
        batchButton.Click += BatchButton_Click;

        _analyzeButton = new Button
        {
            Text = "Analisar",
            Location = new Point(675, 80),
            Size = new Size(102, 30),
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = _colorBtnDisabled,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _analyzeButton.FlatAppearance.BorderSize = 0;
        _analyzeButton.Click += AnalyzeButton_Click;

        _btnCancelar = new Button
        {
            Text = "✕ Cancelar",
            Location = new Point(675, 80),
            Size = new Size(102, 30),
            Visible = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnCancelar.FlatAppearance.BorderSize = 0;
        _btnCancelar.Click += BtnCancelar_Click;

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

        var errorBadge   = CreateBadge(0,   Color.FromArgb(220, 53, 69),  Color.White,                out _errorsLabel);
        var warningBadge = CreateBadge(160, Color.FromArgb(255, 193, 7),  Color.FromArgb(33, 37, 41), out _warningsLabel);
        _badgePanel.Controls.AddRange(new Control[] { errorBadge, warningBadge });

        // --- ListBox de arquivos (visível ao selecionar pasta, antes da análise) ---
        _frxListBox = new ListBox
        {
            Location = new Point(16, 176),
            Size = new Size(752, 110),
            Visible = false,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.FromArgb(200, 200, 200),
            BorderStyle = BorderStyle.None,
            SelectionMode = SelectionMode.None
        };

        // --- ProgressBar + label (visíveis apenas durante análise em lote) ---
        _progressBar = new ProgressBar
        {
            Location = new Point(16, 176),
            Size = new Size(600, 25),
            Visible = false,
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Value = 0
        };

        _lblProgress = new Label
        {
            Text = "",
            Location = new Point(624, 176),
            Size = new Size(144, 25),
            Visible = false,
            ForeColor = Color.FromArgb(33, 37, 41),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // --- DataGridView ---
        _grid = new DataGridView
        {
            Location = new Point(16, 296),
            Size = new Size(752, 300),
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
            new DataGridViewTextBoxColumn { Name = "Arquivo",    HeaderText = "Arquivo",    Width = 130 },
            new DataGridViewTextBoxColumn { Name = "Componente", HeaderText = "Componente", Width = 130 },
            new DataGridViewTextBoxColumn { Name = "Mensagem",   HeaderText = "Mensagem",   Width = 200 },
            new DataGridViewTextBoxColumn { Name = "Detalhe",    HeaderText = "Detalhe",    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
        );

        // --- Rodapé ---
        _exportButton = new Button
        {
            Text = "Exportar relatório CSV",
            Location = new Point(16, 608),
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
            titleLabel, subtitleLabel, _btnSchema, _btnConfig, _btnAjuda,
            selectButton, batchButton, _fileLabel, _analyzeButton, _btnCancelar,
            separator, _badgePanel, _frxListBox, _progressBar, _lblProgress,
            _grid, _exportButton
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

    private void LimparResultados()
    {
        _results.Clear();
        _grid.Rows.Clear();
        _badgePanel.Visible = false;
        _exportButton.Visible = false;
        _lblProgress.Visible = false;
        _lblProgress.Text = "";
    }

    private void CancelarBatchSeAtivo()
    {
        if (_batchCts != null)
        {
            _batchCts.Cancel();
            _batchCts.Dispose();
            _batchCts = null;
            _progressBar.Visible = false;
            _lblProgress.Visible = false;
            _btnCancelar.Visible = false;
            SetButtonEnabled(_analyzeButton, true, _colorBtnGreen);
            Cursor = Cursors.Default;
        }
    }

    private void SelectButton_Click(object? sender, EventArgs e)
    {
        CancelarBatchSeAtivo();

        using var dialog = new OpenFileDialog
        {
            Filter = "FastReport (*.frx)|*.frx|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo .frx"
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _selectedFilePath = dialog.FileName;
            _selectedFolderPath = null;
            _frxFiles = null;
            _fileLabel.Text = Path.GetFileName(_selectedFilePath);
            _fileLabel.ForeColor = Color.FromArgb(33, 37, 41);
            SetButtonEnabled(_analyzeButton, true, _colorBtnGreen);
            SetButtonEnabled(_btnSchema, true, _colorBtnGreen);
            _frxListBox.Visible = false;
            LimparResultados();
        }
    }

    private void BatchButton_Click(object? sender, EventArgs e)
    {
        CancelarBatchSeAtivo();

        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecionar pasta com arquivos .frx",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        _selectedFolderPath = dialog.SelectedPath;
        _selectedFilePath = null;
        SetButtonEnabled(_btnSchema, false, _colorBtnGreen);

        try
        {
            _frxFiles = Directory.GetFiles(_selectedFolderPath, "*.frx", SearchOption.TopDirectoryOnly)
                                 .OrderBy(f => Path.GetFileName(f))
                                 .ToArray();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao ler a pasta:\n{ex.Message}", "RevisorFRX",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _frxFiles = null;
            _fileLabel.Text = $"📁 {_selectedFolderPath}  (erro ao ler)";
            _fileLabel.ForeColor = Color.Gray;
            SetButtonEnabled(_analyzeButton, false, _colorBtnGreen);
            _frxListBox.Visible = false;
            return;
        }

        if (_frxFiles.Length == 0)
        {
            _fileLabel.Text = $"📁 {_selectedFolderPath}  (0 arquivos .frx)";
            _fileLabel.ForeColor = Color.Gray;
            SetButtonEnabled(_analyzeButton, false, _colorBtnGreen);
            _frxListBox.Visible = false;
            MessageBox.Show("Nenhum arquivo .frx encontrado na pasta.", "RevisorFRX",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _fileLabel.Text = $"📁 {_selectedFolderPath}  ({_frxFiles.Length} arquivo(s) .frx)";
        _fileLabel.ForeColor = Color.FromArgb(33, 37, 41);
        SetButtonEnabled(_analyzeButton, true, _colorBtnGreen);

        _frxListBox.Items.Clear();
        foreach (var f in _frxFiles)
            _frxListBox.Items.Add(Path.GetFileName(f));
        _frxListBox.Visible = true;

        LimparResultados();
    }

    private async void AnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (_batchCts != null) return; // batch já em andamento

        if (_analisandoArquivo)
        {
            _cts?.Cancel();
            return;
        }

        if (!string.IsNullOrEmpty(_selectedFolderPath) && _frxFiles != null && _frxFiles.Length > 0)
        {
            await AnalisarPastaAsync(_frxFiles);
            return;
        }

        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        IniciarModoAnalise();

        try
        {
            var content = await File.ReadAllTextAsync(_selectedFilePath, _cts!.Token);
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("O arquivo selecionado está vazio.", "RevisorFRX",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _results = await Task.Run(() => new FrxAnalyzer().Analyze(content, _ruleConfig), _cts.Token);
            _cts.Token.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(_selectedFilePath!);
            foreach (var r in _results) r.FileName = fileName;

            PopulateGrid();
            UpdateBadges();
            _badgePanel.Visible = true;
            _exportButton.Visible = _results.Count > 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao analisar o arquivo:\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            FinalizarModoAnalise();
        }
    }

    private async Task AnalisarPastaAsync(string[] arquivos)
    {
        _batchCts = new CancellationTokenSource();
        var token = _batchCts.Token;

        SetButtonEnabled(_analyzeButton, false, _colorBtnGreen);
        _btnCancelar.Visible = true;
        _badgePanel.Visible = false;
        _exportButton.Visible = false;
        _frxListBox.Visible = false;
        _progressBar.Visible = true;
        _lblProgress.Visible = true;
        _grid.Rows.Clear();
        _results = new List<RuleResult>();
        Cursor = Cursors.WaitCursor;

        _progressBar.Maximum = arquivos.Length;
        _progressBar.Value = 0;

        var totalArquivos = arquivos.Length;

        try
        {
            for (int i = 0; i < totalArquivos; i++)
            {
                token.ThrowIfCancellationRequested();

                var caminho = arquivos[i];
                var fileName = Path.GetFileName(caminho);

                _lblProgress.Text = $"Analisando {i + 1} de {totalArquivos} — {fileName}";

                List<RuleResult> resultados;
                try
                {
                    resultados = await Task.Run(() =>
                    {
                        var content = File.ReadAllText(caminho);
                        return new FrxAnalyzer().Analyze(content, _ruleConfig);
                    }, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    resultados = new List<RuleResult>
                    {
                        new RuleResult
                        {
                            RuleCode = "ERRO",
                            Severity = Severity.Error,
                            ComponentName = "",
                            Message = $"Falha ao analisar: {ex.Message}",
                            FileName = fileName
                        }
                    };
                }

                token.ThrowIfCancellationRequested();

                foreach (var r in resultados)
                    r.FileName = fileName;

                _results.AddRange(resultados);
                AdicionarResultadosAoGrid(resultados);
                _progressBar.Value = i + 1;
            }

            ReordenarGridPorSeveridade();

            _badgePanel.Visible = true;
            _exportButton.Visible = true;
            _lblProgress.Text = $"Concluído — {totalArquivos} arquivo(s) analisado(s)";
        }
        catch (OperationCanceledException)
        {
            _lblProgress.Text = "Análise cancelada pelo usuário.";
            _badgePanel.Visible = _results.Count > 0;
            _exportButton.Visible = _results.Count > 0;
        }
        finally
        {
            _progressBar.Visible = false;
            _btnCancelar.Visible = false;
            SetButtonEnabled(_analyzeButton, true, _colorBtnGreen);
            _batchCts?.Dispose();
            _batchCts = null;
            Cursor = Cursors.Default;
        }
    }

    private void AdicionarLinhaAoGrid(RuleResult r)
    {
        var idx = _grid.Rows.Add(r.RuleCode, r.Severity, r.FileName, r.ComponentName, r.Message, r.Detail);
        _grid.Rows[idx].DefaultCellStyle.BackColor = r.Severity switch
        {
            Severity.Error   => Color.FromArgb(255, 220, 220),
            Severity.Warning => Color.FromArgb(255, 243, 205),
            Severity.Info    => Color.FromArgb(207, 226, 255),
            _                => Color.White
        };
    }

    private void AdicionarResultadosAoGrid(List<RuleResult> resultados)
    {
        foreach (var r in resultados)
            AdicionarLinhaAoGrid(r);

        UpdateBadges();
        _grid.Refresh();
    }

    private void ReordenarGridPorSeveridade()
    {
        _results.Sort((a, b) =>
        {
            static int Ordem(Severity s) => s switch
            {
                Severity.Error   => 0,
                Severity.Warning => 1,
                Severity.Info    => 2,
                _                => 3
            };
            return Ordem(a.Severity).CompareTo(Ordem(b.Severity));
        });

        _grid.Rows.Clear();
        foreach (var r in _results)
            AdicionarLinhaAoGrid(r);
    }

    private void BtnCancelar_Click(object? sender, EventArgs e)
    {
        _batchCts?.Cancel();
    }

    private void IniciarModoAnalise()
    {
        _analisandoArquivo = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _analyzeButton.Text = "Cancelar";
        _analyzeButton.BackColor = Color.FromArgb(220, 53, 69);
        _analyzeButton.ForeColor = Color.White;
        _analyzeButton.Enabled = true;
        Cursor = Cursors.WaitCursor;
        _grid.Rows.Clear();
        _badgePanel.Visible = false;
        _exportButton.Visible = false;
    }

    private void FinalizarModoAnalise()
    {
        _analisandoArquivo = false;
        _cts?.Dispose();
        _cts = null;
        _analyzeButton.Text = "Analisar";
        SetButtonEnabled(_analyzeButton, true, _colorBtnGreen);
        Cursor = Cursors.Default;
    }

    private static void SetButtonEnabled(Button btn, bool enabled, Color enabledColor)
    {
        btn.Enabled   = enabled;
        btn.BackColor = enabled ? enabledColor : _colorBtnDisabled;
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();
        foreach (var r in _results)
            AdicionarLinhaAoGrid(r);

        if (_results.Count == 0)
        {
            var idx = _grid.Rows.Add("✅", "", "", "", "Nenhum problema encontrado neste arquivo.", "");
            var row = _grid.Rows[idx];
            row.DefaultCellStyle.ForeColor = Color.FromArgb(50, 160, 50);
            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }
    }

    private void UpdateBadges()
    {
        var e = _results.Count(r => r.Severity == Severity.Error);
        var w = _results.Count(r => r.Severity == Severity.Warning);
        _errorsLabel.Text   = $"{e} Erro{(e != 1 ? "s" : "")}";
        _warningsLabel.Text = $"{w} Aviso{(w != 1 ? "s" : "")}";
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = _selectedFilePath != null
            ? Path.GetFileNameWithoutExtension(_selectedFilePath)
            : _selectedFolderPath != null
                ? Path.GetFileName(_selectedFolderPath)
                : "relatorio";
        var suggestedName = $"RevisorFRX_{baseName}_{timestamp}.csv";

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
            FileName = suggestedName,
            Title = "Exportar relatório CSV"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        if (_results.Count == 0)
        {
            MessageBox.Show("Nenhum resultado para exportar.", "RevisorFRX",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ExportarCsv(_results, dialog.FileName);
        MessageBox.Show("Relatório exportado com sucesso!", "RevisorFRX",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ExportarCsv(List<RuleResult> results, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Arquivo;Regra;Severidade;Componente;Mensagem;Detalhe");

        foreach (var r in results)
        {
            var linha = string.Join(";", new[]
            {
                Escapar(r.FileName),
                Escapar(r.RuleCode),
                Escapar(r.Severity.ToString()),
                Escapar(r.ComponentName),
                Escapar(r.Message),
                Escapar(r.Detail)
            });
            sb.AppendLine(linha);
        }

        File.WriteAllText(filePath, sb.ToString(),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escapar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n'))
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        return valor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _cts?.Dispose();
            _batchCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BtnConfig_Click(object? sender, EventArgs e)
    {
        using var configForm = new ConfigForm(_ruleConfig);
        if (configForm.ShowDialog(this) == DialogResult.OK)
        {
            _ruleConfig = configForm.Config;
        }
    }

    private void BtnAjuda_Click(object? sender, EventArgs e)
    {
        using var help = new HelpForm();
        help.ShowDialog(this);
    }

    private async void BtnSchema_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        var filePath = _selectedFilePath;
        var fileName = Path.GetFileName(filePath);

        Cursor = Cursors.WaitCursor;
        try
        {
            var fields = await Task.Run(() =>
            {
                var xml = File.ReadAllText(filePath);
                var doc = XDocument.Parse(xml);
                return SchemaExtractor.Extract(doc);
            });

            if (fields.Count == 0)
            {
                MessageBox.Show("Nenhum campo foi encontrado no Dictionary deste arquivo .frx.",
                    "Explorador de Schema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var form = new SchemaExplorerForm(fields, fileName);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar o schema:\n{ex.Message}",
                "Explorador de Schema", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
