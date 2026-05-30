using RevisorFRX.Core;
using RevisorFRX.Core.Models;

namespace RevisorFRX.App;

public class ConfigForm : Form
{
    private readonly CheckedListBox _ruleList;
    private readonly RuleConfig _config;

    public RuleConfig Config { get; private set; } = null!;

    public ConfigForm(RuleConfig current)
    {
        _config = new RuleConfig();
        foreach (var kvp in current.EnabledRules)
            _config.EnabledRules[kvp.Key] = kvp.Value;

        SuspendLayout();

        Text = "Configuração — Regras";
        ClientSize = new Size(460, 440);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(248, 249, 250);

        var titleLabel = new Label
        {
            Text = "Ativar / Desativar Regras",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(16, 12),
            AutoSize = true,
            ForeColor = Color.FromArgb(33, 37, 41)
        };

        var subtitleLabel = new Label
        {
            Text = "Marque as regras que deseja ativar durante a análise.",
            Font = new Font("Segoe UI", 9),
            Location = new Point(16, 36),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        var btnSelectAll = new Button
        {
            Text = "Selecionar tudo",
            Location = new Point(16, 60),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnSelectAll.FlatAppearance.BorderSize = 0;
        btnSelectAll.Click += (_, _) => SetAllChecked(true);

        var btnDeselectAll = new Button
        {
            Text = "Desmarcar tudo",
            Location = new Point(156, 60),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnDeselectAll.FlatAppearance.BorderSize = 0;
        btnDeselectAll.Click += (_, _) => SetAllChecked(false);

        var btnRestaurar = new Button
        {
            Text = "Restaurar padrões",
            Location = new Point(296, 60),
            Size = new Size(130, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnRestaurar.FlatAppearance.BorderSize = 0;
        btnRestaurar.Click += (_, _) => RestaurarPadroes();

        _ruleList = new CheckedListBox
        {
            Location = new Point(16, 96),
            Size = new Size(428, 272),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9.5F),
            IntegralHeight = false
        };

        PopulateList();

        // Índice da Fix-1 na lista (dinâmico — não hardcoded)
        var fix1Index = RuleRegistry.GetAll()
            .Select((r, i) => (r, i))
            .First(x => x.r.Code == "Fix-1").i;

        var btnEditFix1 = new Button
        {
            Text      = "✏ Fix-1...",
            Location  = new Point(16, 380),
            Size      = new Size(110, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            Cursor    = Cursors.Hand,
            Enabled   = false,   // habilitado apenas quando Fix-1 está selecionada na lista
            UseVisualStyleBackColor = false
        };
        btnEditFix1.FlatAppearance.BorderSize = 0;

        // Habilita o botão apenas quando a linha da Fix-1 está selecionada
        _ruleList.SelectedIndexChanged += (_, _) =>
            btnEditFix1.Enabled = _ruleList.SelectedIndex == fix1Index;

        btnEditFix1.Click += (_, _) =>
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "hard1-config.json");
            if (!File.Exists(configPath))
            {
                MessageBox.Show(
                    $"Arquivo de configuração não encontrado:\n{configPath}\n\n" +
                    "Verifique se hard1-config.json está na pasta do executável.",
                    "Fix-1 — Config",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            System.Diagnostics.Process.Start("notepad.exe", configPath);
        };

        var toolTip = new ToolTip();
        toolTip.SetToolTip(btnEditFix1, "Editar configuração da regra Fix-1 (hard1-config.json)");

        var btnOk = new Button
        {
            Text = "OK",
            Location = new Point(192, 380),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += BtnOk_Click;

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(286, 380),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[]
        {
            titleLabel, subtitleLabel,
            btnSelectAll, btnDeselectAll, btnRestaurar,
            _ruleList, btnEditFix1, btnOk, btnCancel
        });

        ResumeLayout(true);
    }

    private void PopulateList()
    {
        _ruleList.Items.Clear();
        foreach (var rule in RuleRegistry.GetAll())
        {
            var severity = rule.DefaultSeverity switch
            {
                Severity.Error   => "🔴",
                Severity.Warning => "🟡",
                Severity.Info    => "🔵",
                _ => ""
            };
            _ruleList.Items.Add($"{severity} {rule.Code,-8} — {rule.Description}");
        }

        var rules = RuleRegistry.GetAll();
        for (int i = 0; i < rules.Count; i++)
            _ruleList.SetItemChecked(i, _config.IsEnabled(rules[i].Code));
    }

    private void RestaurarPadroes()
    {
        var rules = RuleRegistry.GetAll();
        for (int i = 0; i < rules.Count; i++)
            _ruleList.SetItemChecked(i, rules[i].DefaultEnabled);
    }

    private void SetAllChecked(bool value)
    {
        for (int i = 0; i < _ruleList.Items.Count; i++)
            _ruleList.SetItemChecked(i, value);
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var rules = RuleRegistry.GetAll();
        for (int i = 0; i < rules.Count; i++)
            _config.EnabledRules[rules[i].Code] = _ruleList.GetItemChecked(i);
        Config = _config;
        DialogResult = DialogResult.OK;
    }
}
