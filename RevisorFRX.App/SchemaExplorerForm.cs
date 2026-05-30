using RevisorFRX.Core.Models;
using RevisorFRX.Core.Services;
using System.Xml.Linq;

namespace RevisorFRX.App;

public class SchemaExplorerForm : Form
{
    private List<SchemaField> _allFields;
    private string? _frxPath;
    private SchemaExtractorOptions _currentOptions = SchemaExtractorOptions.Default;
    private int _extractedCount;
    private int _totalSemFiltro;
    private bool _isCustomOptions;
    private int _gridOriginalTop;
    private int _gridOriginalHeight;
    private const int AdvancedPanelHeight = 72;

    private GroupBox _advancedPanel = null!;
    private NumericUpDown _nudDepth = null!;
    private TextBox _txtSegmentos = null!;

    private readonly TextBox _searchBox;
    private readonly ComboBox _typeCombo;
    private readonly ComboBox _entityCombo;
    private readonly ComboBox _cboStatus;
    private readonly CheckBox _chkNullOnly;
    private readonly CheckBox _chkUsedOnly;
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;
    private readonly Button _btnCopy;
    private readonly System.Windows.Forms.Timer _copyTimer;
    private readonly System.Windows.Forms.Timer _debounceTimer;
    private List<SchemaField> _visibleFields = [];
    private string? _sortColumn;
    private bool _sortAscending = true;
    private Label? _bannerSchemaGrande;
    private Button _btnReaplicar = null!;
    private Button _btnRestaurar = null!;
    private Button _btnAvancado  = null!;
    private bool _suppressFilters;

    public SchemaExplorerForm(List<SchemaField> fields, string frxFileName, string? frxPath = null)
    {
        _allFields = fields;
        _frxPath = frxPath;
        _extractedCount = fields.Count;

        SuspendLayout();

        Text = $"Explorador de Schema — {frxFileName}";
        ClientSize = new Size(920, 580);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 249, 250);

        // --- Row 1: search + combos ---
        var lblSearch = new Label
        {
            Text = "🔍",
            Location = new Point(12, 14),
            AutoSize = true
        };

        _searchBox = new TextBox
        {
            Location = new Point(34, 11),
            Size = new Size(230, 23),
            PlaceholderText = "Pesquisar campo, entidade ou caminho..."
        };

        var lblTipo = new Label
        {
            Text = "Tipo:",
            Location = new Point(276, 14),
            AutoSize = true
        };

        _typeCombo = new ComboBox
        {
            Location = new Point(308, 10),
            Size = new Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblEntidade = new Label
        {
            Text = "Entidade:",
            Location = new Point(470, 14),
            AutoSize = true
        };

        _entityCombo = new ComboBox
        {
            Location = new Point(534, 10),
            Size = new Size(180, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // --- Row 2: checkboxes + status combo + advanced button ---
        _chkNullOnly = new CheckBox
        {
            Text = "Apenas ⚠ null (objeto)",
            Location = new Point(12, 44),
            AutoSize = true
        };

        _chkUsedOnly = new CheckBox
        {
            Text = "Apenas campos usados no relatório",
            Location = new Point(210, 44),
            AutoSize = true
        };

        var lblStatus = new Label
        {
            Text = "Status:",
            Location = new Point(458, 47),
            AutoSize = true
        };

        _cboStatus = new ComboBox
        {
            Location = new Point(506, 43),
            Size = new Size(130, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnAvancado = new Button
        {
            Text = "⚙ Avançado ▼",
            Location = new Point(648, 40),
            Size = new Size(105, 27),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnAvancado.FlatAppearance.BorderSize = 0;
        _btnAvancado.Click += BtnAvancado_Click;

        // --- Separator ---
        var separator = new Panel
        {
            Location = new Point(12, 72),
            Size = new Size(896, 1),
            BackColor = Color.FromArgb(222, 226, 230)
        };

        // Grid starts at default position; AtualizarBannerSchemaGrande adjusts after controls are added
        _gridOriginalTop = 78;
        _gridOriginalHeight = 456;

        // --- Advanced panel (initially hidden) ---
        BuildAdvancedPanel(78);

        // --- Grid ---
        _grid = new DataGridView
        {
            Location = new Point(12, 78),
            Size = new Size(896, 456),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(222, 226, 230),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        _grid.DefaultCellStyle.Padding = new Padding(4, 3, 4, 3);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 200, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(233, 236, 239);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
        _grid.EnableHeadersVisualStyles = false;

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Entidade", HeaderText = "Entidade",        Width = 130, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn { Name = "Campo",    HeaderText = "Campo",            Width = 130, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn { Name = "Tipo",     HeaderText = "Tipo",             Width = 120, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn { Name = "Caminho",  HeaderText = "Caminho completo", Width = 230, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn { Name = "UsadoEm",  HeaderText = "Usado em",         AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Programmatic },
            new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "Status",           Width = 110, SortMode = DataGridViewColumnSortMode.Automatic }
        );

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns.Contains("Caminho"))
                CopySelectedPath();
        };
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && _grid.SelectedRows.Count > 0)
            {
                e.Handled = true;
                CopySelectedPath();
            }
        };
        _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
        _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;

        // --- Bottom bar ---
        _statusLabel = new Label
        {
            Location = new Point(12, 544),
            Size = new Size(530, 26),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray
        };

        var btnExport = new Button
        {
            Text = "⬇ Exportar CSV",
            Location = new Point(552, 540),
            Size = new Size(128, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnExport.FlatAppearance.BorderSize = 0;
        btnExport.Click += BtnExport_Click;

        _btnCopy = new Button
        {
            Text = "📋 Copiar caminho",
            Location = new Point(686, 540),
            Size = new Size(138, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnCopy.FlatAppearance.BorderSize = 0;
        _btnCopy.Click += (_, _) => CopySelectedPath();

        var btnFechar = new Button
        {
            Text = "Fechar",
            Location = new Point(830, 540),
            Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnFechar.FlatAppearance.BorderSize = 0;
        btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            lblSearch, _searchBox, lblTipo, _typeCombo, lblEntidade, _entityCombo,
            _chkNullOnly, _chkUsedOnly, lblStatus, _cboStatus, _btnAvancado,
            separator, _advancedPanel, _grid,
            _statusLabel, btnExport, _btnCopy, btnFechar
        });

        CancelButton = btnFechar;

        _copyTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _copyTimer.Tick += (_, _) =>
        {
            _btnCopy.Text = "📋 Copiar caminho";
            _copyTimer.Stop();
        };

        _debounceTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _debounceTimer.Tick += (_, _) => { _debounceTimer.Stop(); ApplyFilters(); };

        AtualizarBannerSchemaGrande(fields.Count);

        PopulateFilterCombos();
        ApplyFilters();

        _searchBox.TextChanged += (_, _) => { _debounceTimer.Stop(); _debounceTimer.Start(); };
        _typeCombo.SelectedIndexChanged += (_, _) => ApplyFilters();
        _entityCombo.SelectedIndexChanged += (_, _) => ApplyFilters();
        _chkNullOnly.CheckedChanged += (_, _) => ApplyFilters();
        _chkUsedOnly.CheckedChanged += (_, _) => ApplyFilters();
        _cboStatus.SelectedIndexChanged += (_, _) => ApplyFilters();

        ResumeLayout(false);
    }

    private void BuildAdvancedPanel(int top)
    {
        _advancedPanel = new GroupBox
        {
            Text = "Filtros avançados",
            Location = new Point(12, top),
            Size = new Size(896, AdvancedPanelHeight),
            Visible = false,
            Font = Font
        };

        var lblDepth = new Label
        {
            Text = "Profundidade máxima:",
            Location = new Point(8, 22),
            AutoSize = true
        };

        _nudDepth = new NumericUpDown
        {
            Location = new Point(148, 19),
            Size = new Size(55, 23),
            Minimum = 1,
            Maximum = 10,
            Value = SchemaExtractorOptions.Default.MaxDepth
        };

        var lblSegmentos = new Label
        {
            Text = "Segmentos excluídos:",
            Location = new Point(215, 22),
            AutoSize = true
        };

        _txtSegmentos = new TextBox
        {
            Location = new Point(355, 19),
            Size = new Size(200, 23),
            Text = string.Join(", ", SchemaExtractorOptions.Default.ExcludedSegments)
        };

        _btnReaplicar = new Button
        {
            Text = "Reaplicar",
            Location = new Point(567, 17),
            Size = new Size(92, 27),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnReaplicar.FlatAppearance.BorderSize = 0;
        _btnReaplicar.Click += BtnReaplicar_Click;

        _btnRestaurar = new Button
        {
            Text = "Restaurar padrões",
            Location = new Point(667, 17),
            Size = new Size(150, 27),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnRestaurar.FlatAppearance.BorderSize = 0;
        _btnRestaurar.Click += BtnRestaurar_Click;

        var lblHint = new Label
        {
            Text = "(separados por vírgula)",
            Location = new Point(355, 46),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7.5F, FontStyle.Italic)
        };

        _advancedPanel.Controls.AddRange(new Control[]
        {
            lblDepth, _nudDepth, lblSegmentos, _txtSegmentos,
            _btnReaplicar, _btnRestaurar, lblHint
        });
    }

    private void BtnAvancado_Click(object? sender, EventArgs e)
    {
        _advancedPanel.Visible = !_advancedPanel.Visible;
        if (_advancedPanel.Visible)
        {
            _btnAvancado.Text = "⚙ Avançado ▲";
            _grid.Top    = _gridOriginalTop + AdvancedPanelHeight;
            _grid.Height = _gridOriginalHeight - AdvancedPanelHeight;
        }
        else
        {
            _btnAvancado.Text = "⚙ Avançado ▼";
            _grid.Top    = _gridOriginalTop;
            _grid.Height = _gridOriginalHeight;
        }
    }

    private async void BtnReaplicar_Click(object? sender, EventArgs e)
    {
        var path = EnsureFrxPath();
        if (path == null) return;

        var options     = GetCurrentSchemaOptions();
        var prevTotal   = _totalSemFiltro;

        _btnReaplicar.Enabled = false;
        _btnRestaurar.Enabled = false;
        Cursor = Cursors.WaitCursor;

        try
        {
            var (fields, totalSemFiltro) = await Task.Run(() =>
            {
                var doc = XDocument.Load(path);
                var f   = SchemaExtractor.Extract(doc, options);
                var tot = prevTotal == 0
                    ? SchemaExtractor.Extract(doc, SchemaExtractorOptions.Unrestricted).Count
                    : prevTotal;
                return (f, tot);
            });

            _allFields       = fields;
            _extractedCount  = fields.Count;
            _currentOptions  = options;
            _isCustomOptions = !OptionsAreDefault(options);
            _totalSemFiltro  = totalSemFiltro;

            AtualizarBannerSchemaGrande(totalSemFiltro);
            RefreshFilterCombos();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao reextrair schema:\n{ex.Message}", "Filtros avançados",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnReaplicar.Enabled = true;
            _btnRestaurar.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private void BtnRestaurar_Click(object? sender, EventArgs e)
    {
        var def = SchemaExtractorOptions.Default;
        _nudDepth.Value = def.MaxDepth;
        _txtSegmentos.Text = string.Join(", ", def.ExcludedSegments);
        if (_isCustomOptions)
            BtnReaplicar_Click(sender, e);
    }

    private void AtualizarBannerSchemaGrande(int totalCampos)
    {
        bool precisaBanner = totalCampos > 5000;

        if (!precisaBanner && _bannerSchemaGrande == null)
            return;

        if (precisaBanner && _bannerSchemaGrande != null)
        {
            _bannerSchemaGrande.Text =
                $"⚠ Schema grande ({totalCampos:N0} campos). Use os filtros acima — " +
                "\"Disponível\" mostra apenas campos não usados no layout.";
            return;
        }

        if (!precisaBanner && _bannerSchemaGrande != null)
        {
            Controls.Remove(_bannerSchemaGrande);
            _bannerSchemaGrande.Dispose();
            _bannerSchemaGrande = null;
            _gridOriginalTop    = 78;
            _gridOriginalHeight = 456;
        }
        else // precisaBanner && _bannerSchemaGrande == null
        {
            _bannerSchemaGrande = new Label
            {
                Text      = $"⚠ Schema grande ({totalCampos:N0} campos). Use os filtros acima — " +
                            "\"Disponível\" mostra apenas campos não usados no layout.",
                BackColor = Color.FromArgb(80, 60, 20),
                ForeColor = Color.FromArgb(255, 210, 100),
                Location  = new Point(12, 73),
                Size      = new Size(896, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = Font
            };
            Controls.Add(_bannerSchemaGrande);
            _gridOriginalTop    = 102;
            _gridOriginalHeight = 432;
        }

        // Reposiciona o painel avançado e o grid conforme o novo gridOriginalTop
        _advancedPanel.Location = new Point(_advancedPanel.Location.X, _gridOriginalTop);

        if (_advancedPanel.Visible)
        {
            _grid.Top    = _gridOriginalTop + AdvancedPanelHeight;
            _grid.Height = _gridOriginalHeight - AdvancedPanelHeight;
        }
        else
        {
            _grid.Top    = _gridOriginalTop;
            _grid.Height = _gridOriginalHeight;
        }
    }

    private string? EnsureFrxPath()
    {
        if (_frxPath != null) return _frxPath;
        using var dlg = new OpenFileDialog
        {
            Filter = "FastReport (*.frx)|*.frx|Todos os arquivos (*.*)|*.*",
            Title = "Selecione o arquivo .frx para reextração"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return null;
        _frxPath = dlg.FileName;
        return _frxPath;
    }

    private SchemaExtractorOptions GetCurrentSchemaOptions()
    {
        var depth = (int)_nudDepth.Value;
        var segments = _txtSegmentos.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        return new SchemaExtractorOptions { MaxDepth = depth, ExcludedSegments = segments };
    }

    private static bool OptionsAreDefault(SchemaExtractorOptions options)
    {
        var def = SchemaExtractorOptions.Default;
        return options.MaxDepth == def.MaxDepth
            && options.ExcludedSegments.Count == def.ExcludedSegments.Count
            && options.ExcludedSegments.SequenceEqual(def.ExcludedSegments, StringComparer.OrdinalIgnoreCase);
    }

    private void PopulateFilterCombos()
    {
        RefreshFilterCombos();
        _cboStatus.Items.AddRange(new object[] { "(todos)", "Em uso", "Disponível" });
        _cboStatus.SelectedIndex = 2;
    }

    private void RefreshFilterCombos()
    {
        _suppressFilters = true;
        try
        {
            _typeCombo.Items.Clear();
            _typeCombo.Items.Add("(todos os tipos)");
            foreach (var t in _allFields.Select(f => f.DisplayType).Distinct().OrderBy(t => t))
                _typeCombo.Items.Add(t);
            _typeCombo.SelectedIndex = 0;

            _entityCombo.Items.Clear();
            _entityCombo.Items.Add("(todas as entidades)");
            foreach (var e in _allFields.Select(f => f.EntityAlias).Where(a => !string.IsNullOrEmpty(a)).Distinct().OrderBy(a => a))
                _entityCombo.Items.Add(e);
            _entityCombo.SelectedIndex = 0;
        }
        finally
        {
            _suppressFilters = false;
        }
    }

    private void ApplyFilters()
    {
        if (_suppressFilters) return;

        var search = _searchBox.Text.Trim();
        var selectedType   = _typeCombo.SelectedIndex   > 0 ? _typeCombo.SelectedItem?.ToString()   : null;
        var selectedEntity = _entityCombo.SelectedIndex > 0 ? _entityCombo.SelectedItem?.ToString() : null;
        var selectedStatus = _cboStatus.SelectedItem?.ToString();
        var nullOnly = _chkNullOnly.Checked;
        var usedOnly = _chkUsedOnly.Checked;

        var filtered = _allFields.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(f =>
                f.EntityAlias.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.FieldName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.FullPath.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (selectedType != null)
            filtered = filtered.Where(f => f.DisplayType == selectedType);

        if (selectedEntity != null)
            filtered = filtered.Where(f => f.EntityAlias == selectedEntity);

        if (nullOnly)
            filtered = filtered.Where(f => f.IsNonScalar);

        if (usedOnly)
            filtered = filtered.Where(f => f.IsUsed);

        if (selectedStatus == "Em uso")
            filtered = filtered.Where(f => f.IsUsed);
        else if (selectedStatus == "Disponível")
            filtered = filtered.Where(f => !f.IsUsed);

        var list = filtered.ToList();

        if (_sortColumn != null)
            list = (_sortColumn switch
            {
                "Entidade" => _sortAscending ? list.OrderBy(f => f.EntityAlias, StringComparer.OrdinalIgnoreCase)   : list.OrderByDescending(f => f.EntityAlias, StringComparer.OrdinalIgnoreCase),
                "Campo"    => _sortAscending ? list.OrderBy(f => f.FieldName,   StringComparer.OrdinalIgnoreCase)   : list.OrderByDescending(f => f.FieldName,   StringComparer.OrdinalIgnoreCase),
                "Tipo"     => _sortAscending ? list.OrderBy(f => f.DisplayType, StringComparer.OrdinalIgnoreCase)   : list.OrderByDescending(f => f.DisplayType, StringComparer.OrdinalIgnoreCase),
                "Caminho"  => _sortAscending ? list.OrderBy(f => f.FullPath,    StringComparer.OrdinalIgnoreCase)   : list.OrderByDescending(f => f.FullPath,    StringComparer.OrdinalIgnoreCase),
                "UsadoEm"  => _sortAscending
                    ? list.OrderBy(f => f.UsedInComponents.Count).ThenBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase)
                    : list.OrderByDescending(f => f.UsedInComponents.Count).ThenBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase),
                _ => list.AsEnumerable()
            }).ToList();

        PopulateGrid(list);
        UpdateCountLabel(list.Count, selectedStatus);
    }

    private void PopulateGrid(List<SchemaField> fields)
    {
        _visibleFields = fields;
        _grid.SuspendLayout();
        _grid.Rows.Clear();

        if (fields.Count == 0)
        {
            _grid.Columns.Clear();
            _grid.Rows.Clear();
            _grid.Columns.Add("Vazio", "Nenhum campo encontrado para os filtros aplicados.");
            _grid.Rows.Add("Tente alterar os filtros ou limpar a busca.");
            _grid.ResumeLayout();
            AtualizarEstadoBotoes();
            return;
        }

        if (_grid.Columns.Count == 0 || _grid.Columns[0].Name != "Entidade")
        {
            _grid.Columns.Clear();
            _grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Entidade", HeaderText = "Entidade",        Width = 130, SortMode = DataGridViewColumnSortMode.Programmatic },
                new DataGridViewTextBoxColumn { Name = "Campo",    HeaderText = "Campo",            Width = 130, SortMode = DataGridViewColumnSortMode.Programmatic },
                new DataGridViewTextBoxColumn { Name = "Tipo",     HeaderText = "Tipo",             Width = 120, SortMode = DataGridViewColumnSortMode.Programmatic },
                new DataGridViewTextBoxColumn { Name = "Caminho",  HeaderText = "Caminho completo", Width = 230, SortMode = DataGridViewColumnSortMode.Programmatic },
                new DataGridViewTextBoxColumn { Name = "UsadoEm",  HeaderText = "Usado em",         AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Programmatic },
                new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "Status",           Width = 110, SortMode = DataGridViewColumnSortMode.Automatic }
            );
        }

        foreach (var f in fields)
        {
            // 0 → "—"  |  1 → nome direto  |  2+ → "N componentes" (tooltip traz a lista)
            var usedIn = f.UsedInComponents.Count switch
            {
                0 => "—",
                1 => f.UsedInComponents[0],
                _ => $"{f.UsedInComponents.Count} componentes"
            };
            var status = f.IsUsed ? "✅ Em uso" : "⚪ Disponível";
            var idx = _grid.Rows.Add(f.EntityAlias, f.FieldName, f.DisplayType, f.FullPath, usedIn, status);
            var row = _grid.Rows[idx];
            row.Tag = f;

            if (f.IsNonScalar)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(133, 100, 4);
            }
            else
            {
                row.DefaultCellStyle.BackColor = TypeColor(f.DisplayType);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            }

            var statusCell = row.Cells["Status"];
            statusCell.Style.BackColor = f.IsUsed
                ? Color.FromArgb(140, 200, 140)
                : Color.FromArgb(160, 160, 160);
            statusCell.Style.ForeColor = Color.FromArgb(33, 37, 41);
        }

        _grid.ResumeLayout();
        AtualizarEstadoBotoes();
    }

    private void AtualizarEstadoBotoes()
    {
        _btnCopy.Enabled = _grid.Rows.Count > 0 && _grid.Columns.Count > 0
                        && _grid.Columns[0].Name != "Vazio";
    }

    private void UpdateCountLabel(int visibleCount, string? statusFilter)
    {
        string text;
        if (!_isCustomOptions)
        {
            text = $"{visibleCount} campo(s) exibido(s) de {_extractedCount} total";
            if (string.IsNullOrEmpty(statusFilter) || statusFilter == "(todos)")
            {
                var emUso = _allFields.Count(f => f.IsUsed);
                var nulos = _allFields.Count(f => f.IsNonScalar);
                text += $"  •  {emUso} em uso  •  {nulos} null (obj)";
            }
        }
        else if (_totalSemFiltro > 0)
        {
            text = $"{visibleCount} campo(s) exibido(s) de {_extractedCount} extraídos ({_totalSemFiltro} no schema completo)";
        }
        else
        {
            text = $"{visibleCount} campo(s) exibido(s) de {_extractedCount} extraídos (filtros avançados ativos)";
        }
        _statusLabel.Text = text;
    }

    private static Color TypeColor(string displayType) => displayType switch
    {
        "Decimal" or "Double" or "Single"      => Color.FromArgb(207, 226, 255),
        "DateTime"                              => Color.FromArgb(230, 200, 255),
        var t when t.StartsWith("Int")          => Color.FromArgb(198, 239, 206),
        _                                       => Color.White
    };

    private void CopySelectedPath()
    {
        if (_grid.SelectedRows.Count == 0) return;
        if (!_grid.Columns.Contains("Caminho")) return;
        var path = _grid.SelectedRows[0].Cells["Caminho"].Value?.ToString();
        if (string.IsNullOrEmpty(path)) return;

        Clipboard.SetText(path);
        _btnCopy.Text = "✓ Copiado!";
        _copyTimer.Stop();
        _copyTimer.Start();
    }

    private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        var colName = _grid.Columns[e.ColumnIndex].Name;
        if (colName == "Status") return;

        if (_sortColumn == colName)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = colName;
            _sortAscending = true;
        }

        foreach (DataGridViewColumn col in _grid.Columns)
            col.HeaderCell.SortGlyphDirection = SortOrder.None;
        _grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
            _sortAscending ? SortOrder.Ascending : SortOrder.Descending;

        ApplyFilters();
    }

    private void Grid_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
        var colName = _grid.Columns[e.ColumnIndex].Name;

        if (colName == "UsadoEm" && _grid.Rows[e.RowIndex].Tag is SchemaField field && field.IsUsed)
        {
            e.ToolTipText = string.Join("\n", field.UsedInComponents);
            return;
        }

        if (colName == "Caminho")
        {
            var valor = _grid.Rows[e.RowIndex].Cells["Caminho"].Value?.ToString();
            if (!string.IsNullOrEmpty(valor))
                e.ToolTipText = valor;
        }
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_visibleFields.Count == 0)
        {
            MessageBox.Show("Nenhum campo visível para exportar.", "Explorador de Schema",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
            FileName = $"Schema_{timestamp}.csv",
            Title = "Exportar schema para CSV"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        ExportarCsv(_visibleFields, dialog.FileName);
        MessageBox.Show($"{_visibleFields.Count} campo(s) exportado(s) com sucesso!", "Explorador de Schema",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ExportarCsv(List<SchemaField> fields, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Entidade;Campo;Tipo;É Objeto?;Caminho completo;Usado em");

        foreach (var f in fields)
        {
            var usedIn = f.IsUsed ? string.Join(", ", f.UsedInComponents) : "";
            sb.AppendLine(string.Join(";",
                Csv(f.EntityAlias),
                Csv(f.FieldName),
                Csv(f.DisplayType),
                Csv(f.IsNonScalar ? "Sim" : "Não"),
                Csv(f.FullPath),
                Csv(usedIn)));
        }

        File.WriteAllText(filePath, sb.ToString(),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _copyTimer.Dispose();
            _debounceTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
