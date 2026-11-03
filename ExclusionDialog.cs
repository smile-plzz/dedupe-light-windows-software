using System.Text.Json;

namespace DedupeLight;

sealed class ExclusionDialog : Form
{
    readonly CheckedListBox list = new()
    {
        Dock = DockStyle.Fill,
        HorizontalScrollbar = true,
        CheckOnClick = true,
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 9.5f)
    };
    readonly TextBox searchBox = new()
    {
        Dock = DockStyle.Fill,
        PlaceholderText = "Filter rules…",
        BorderStyle = BorderStyle.FixedSingle
    };

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public List<ExclusionRule> Rules { get; private set; }

    public ExclusionDialog(IEnumerable<ExclusionRule> rules)
    {
        Rules = ExclusionRules.Validate(rules);
        Text = "Excluded Sources & Rules";
        Size = new Size(860, 560);
        MinimumSize = new Size(720, 440);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(248, 249, 251);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 20),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Intentional copies stay yours.\nExcluded paths, patterns, and folder names are skipped during scans. Uncheck to temporarily disable.",
            ForeColor = Color.FromArgb(100, 108, 122),
            AutoSize = false
        };
        layout.Controls.Add(header, 0, 0);

        var searchPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
        searchBox.TextChanged += (_, _) => PopulateList();
        searchPanel.Controls.Add(searchBox);
        layout.Controls.Add(searchPanel, 0, 1);

        var listCard = new SurfaceCard { Dock = DockStyle.Fill, Padding = new Padding(8), Margin = Padding.Empty };
        listCard.Controls.Add(list);
        layout.Controls.Add(listCard, 0, 2);

        list.ItemCheck += (_, e) =>
        {
            if (e.Index >= 0 && e.Index < list.Items.Count && list.Items[e.Index] is ExclusionRule rule)
            {
                var newEnabled = e.NewValue == CheckState.Checked;
                var idx = Rules.FindIndex(r => r.Path == rule.Path && r.Kind == rule.Kind && r.IsFolder == rule.IsFolder);
                if (idx >= 0) Rules[idx] = Rules[idx] with { Enabled = newEnabled };
            }
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 14, 0, 0),
            WrapContents = true
        };

        Add(buttons, "Add folder", () =>
        {
            using var d = new FolderBrowserDialog { Description = "Select folder to exclude" };
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                Rules.Add(new(d.SelectedPath, true, RuleKind.Path));
                RefreshRules();
            }
        });

        Add(buttons, "Add files", () =>
        {
            using var d = new OpenFileDialog { Multiselect = true, Title = "Select files to exclude" };
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                Rules.AddRange(d.FileNames.Select(p => new ExclusionRule(p, false, RuleKind.Path)));
                RefreshRules();
            }
        });

        Add(buttons, "Add pattern / name", PromptPattern);
        Add(buttons, "Add presets", AddPresets);
        Add(buttons, "Remove selected", RemoveSelected);
        Add(buttons, "Import", Import);
        Add(buttons, "Export", Export);

        var apply = new SoftButton { Text = "Save & Apply", Primary = true };
        apply.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        buttons.Controls.Add(apply);

        var cancel = new SoftButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(cancel);
        CancelButton = cancel;

        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        RefreshRules();
    }

    static void Add(Control parent, string text, Action action)
    {
        var b = new SoftButton { Text = text };
        b.Click += (_, _) => action();
        parent.Controls.Add(b);
    }

    void RefreshRules()
    {
        Rules = ExclusionRules.Validate(Rules);
        PopulateList();
    }

    void PopulateList()
    {
        var filter = searchBox.Text.Trim();
        var matching = string.IsNullOrEmpty(filter)
            ? Rules
            : Rules.Where(r => r.Path.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                               r.Kind.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        list.Items.Clear();
        foreach (var rule in matching)
        {
            list.Items.Add(rule, rule.Enabled);
        }
    }

    void PromptPattern()
    {
        using var prompt = new Form
        {
            Text = "Add Pattern or Folder Name",
            Size = new Size(500, 240),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        var pLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 4 };
        pLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        pLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        pLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        pLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lbl = new Label { Text = "Enter a file pattern (e.g. *.tmp, *.bak) or folder name (e.g. node_modules, .git):", Dock = DockStyle.Fill };
        var txt = new TextBox { Dock = DockStyle.Fill };
        var radioPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var rExt = new RadioButton { Text = "Extension / File pattern", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 16, 0) };
        var rFolder = new RadioButton { Text = "Folder name wildcard", AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
        radioPanel.Controls.AddRange([rExt, rFolder]);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var okBtn = new SoftButton { Text = "Add Rule", Primary = true };
        var cancelBtn = new SoftButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
        okBtn.Click += (_, _) =>
        {
            var val = txt.Text.Trim();
            if (!string.IsNullOrEmpty(val))
            {
                var kind = rExt.Checked ? RuleKind.Extension : RuleKind.FolderName;
                Rules.Add(new(val, rFolder.Checked, kind, true));
                RefreshRules();
                prompt.DialogResult = DialogResult.OK;
                prompt.Close();
            }
        };
        btnPanel.Controls.AddRange([okBtn, cancelBtn]);

        pLayout.Controls.Add(lbl, 0, 0);
        pLayout.Controls.Add(txt, 0, 1);
        pLayout.Controls.Add(radioPanel, 0, 2);
        pLayout.Controls.Add(btnPanel, 0, 3);
        prompt.Controls.Add(pLayout);
        prompt.AcceptButton = okBtn;
        prompt.CancelButton = cancelBtn;
        prompt.ShowDialog(this);
    }

    void AddPresets()
    {
        var countBefore = Rules.Count;
        foreach (var preset in ExclusionRules.CommonPresets)
        {
            if (!Rules.Any(r => r.Path.Equals(preset.Path, StringComparison.OrdinalIgnoreCase) && r.Kind == preset.Kind))
            {
                Rules.Add(preset);
            }
        }
        RefreshRules();
        MessageBox.Show(this, $"Added {Rules.Count - countBefore} common presets (developer folders, temp/cache extensions, and system directories).", "Presets added", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void RemoveSelected()
    {
        var toRemove = list.SelectedItems.Cast<ExclusionRule>().ToHashSet();
        if (toRemove.Count == 0) return;
        Rules.RemoveAll(toRemove.Contains);
        RefreshRules();
    }

    void Import()
    {
        using var d = new OpenFileDialog { Filter = "Exclusions (*.json)|*.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (new FileInfo(d.FileName).Length > 1024 * 1024) throw new IOException("Exclusion files must be smaller than 1 MB.");
            var imported = JsonSerializer.Deserialize<List<ExclusionRule>>(File.ReadAllText(d.FileName)) ?? throw new IOException("The file contains no rules.");
            var validated = ExclusionRules.Validate(imported);
            Rules.AddRange(validated);
            RefreshRules();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not import exclusions");
        }
    }

    void Export()
    {
        using var d = new SaveFileDialog { Filter = "Exclusions (*.json)|*.json", FileName = "dedupelight-exclusions.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(d.FileName, JsonSerializer.Serialize(Rules, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not export exclusions");
        }
    }
}
