using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace DedupeLight;

public sealed class MainForm : Form
{
    static readonly Color Ink = Color.FromArgb(17, 24, 39);
    static readonly Color Muted = Color.FromArgb(107, 114, 128);
    static readonly Color Line = Color.FromArgb(229, 231, 235);
    static readonly Color Accent = Color.FromArgb(15, 108, 189);
    static readonly Color Surface = Color.White;
    static readonly Color Canvas = Color.FromArgb(248, 249, 251);

    readonly TextBox pathBox = new() { PlaceholderText = "Choose a folder or drive to scan", Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    readonly TextBox searchBox = new() { PlaceholderText = "Search name or location… (Ctrl+F)", Width = 220, BorderStyle = BorderStyle.FixedSingle };

    readonly Button browse = MakeButton("Browse", true);
    readonly Button scan = MakeButton("Scan now");
    readonly Button cancel = MakeButton("Cancel", true);
    readonly Button selectExtras = MakeButton("Select extras", true);
    readonly Button clearSelection = MakeButton("Clear", true);
    readonly Button recycle = MakeButton("Recycle", false, true);
    readonly Button move = MakeButton("Move", true);
    readonly Button copy = MakeButton("Copy", true);
    readonly Button open = MakeButton("Open", true);
    readonly Button reveal = MakeButton("Show in Explorer", true);
    readonly Button export = MakeButton("Export CSV", true);
    readonly Button exportJson = MakeButton("Export JSON", true);
    readonly Button previewBtn = MakeButton("Compare", true);
    readonly Button togglePreview = MakeButton("Inspector ◫", true);
    readonly Button excludeFile = MakeButton("Exclude file", true);
    readonly Button excludeFolder = MakeButton("Exclude folder", true);
    readonly Button manageExclusions = MakeButton("Excluded sources", true);

    readonly ComboBox sortOrderBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 32 };
    readonly ComboBox keepRule = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 148, Height = 32 };
    readonly NumericUpDown minimumSize = new() { Minimum = 0, Maximum = 102400, Value = 1, Width = 76, ThousandsSeparator = true };
    readonly CheckBox hiddenSystem = new() { Text = "Include hidden/system", AutoSize = true, ForeColor = Muted, Padding = new Padding(4, 5, 0, 0) };

    readonly DataGridView grid = new SmoothGrid
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        RowHeadersVisible = false,
        BorderStyle = BorderStyle.None,
        BackgroundColor = Surface,
        GridColor = Line,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
    };

    readonly ContextMenuStrip gridMenu = new();
    readonly PreviewInspector inspector = new();
    readonly SplitContainer splitView = new() { Dock = DockStyle.Fill, SplitterDistance = 720, SplitterWidth = 6, BackColor = Line };

    readonly Label status = new() { Text = "Choose a location to begin", AutoSize = true, ForeColor = Muted };
    readonly Label summary = new() { Text = "No scan yet", AutoSize = true, Font = new Font("Segoe UI Semibold", 10.5f), ForeColor = Ink };
    readonly Label selectedSummary = new() { Text = "", AutoSize = true, ForeColor = Muted, Padding = new Padding(10, 6, 0, 0) };
    readonly Label fileDetails = new() { Dock = DockStyle.Fill, ForeColor = Muted, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    readonly ProgressBar progress = new() { Dock = DockStyle.Bottom, Height = 3, Style = ProgressBarStyle.Marquee, Visible = false, MarqueeAnimationSpeed=24 };
    readonly Label emptyState = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Muted, Font = new Font("Segoe UI", 11.5f), Text = "A little more room for what matters.\n\nChoose a folder or drop one here to find exact duplicates." };

    readonly FlowLayoutPanel filterChipsPanel = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, Margin = Padding.Empty };
    readonly List<FilterChipButton> chipButtons = [];
    string activeCategory = "All";

    readonly ToolTip hints = new();
    readonly System.Windows.Forms.Timer filterDelay = new() { Interval = 160 };
    readonly BindingSource source = new();
    readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DedupeLight");

    string CachePath => Path.Combine(appDataPath, "hash-cache.json");
    string SettingsPath => Path.Combine(appDataPath, "settings.json");

    List<DuplicateItem> allItems = [];
    List<ExclusionRule> exclusions = [];
    CancellationTokenSource? cts;
    int sortColumn = -1;
    bool sortAscending = true;
    bool busy, rebinding;
    bool hasScanned;

    public MainForm()
    {
        Text = "DedupeLight";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Width = 1240;
        Height = 840;
        MinimumSize = new Size(980, 680);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Canvas;
        ForeColor = Ink;
        AllowDrop = true;
        KeyPreview = true;

        keepRule.Items.AddRange(["Keep oldest", "Keep newest", "Keep shortest path", "Keep longest path", "Invert selection"]);
        keepRule.SelectedIndex = 0;

        sortOrderBox.Items.AddRange([
            "Largest sets first",
            "Smallest sets first",
            "Most copies first",
            "File name (A-Z)",
            "Newest modified",
            "Oldest modified"
        ]);
        sortOrderBox.SelectedIndex = 0;

        BuildLayout();
        ConfigureGrid();
        ConfigureContextMenu();
        WireEvents();
        LoadPreferences();
        SetBusy(false);
        UpdateSummary();
    }

    static Button MakeButton(string text, bool secondary = false, bool danger = false) =>
        new SoftButton { Text = text, Primary = !secondary && !danger, Destructive = danger };

    void BuildLayout()
    {
        SuspendLayout();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Padding = new Padding(24, 16, 24, 16),
            ColumnCount = 1,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));  // Header
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108)); // Location card
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));  // Search & Filter chips
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Split view (Grid + Inspector)
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // Details line
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116)); // Action toolbar

        // Header
        var header = new Panel { Dock = DockStyle.Fill };
        var titleLbl = new Label { Text = "DedupeLight", AutoSize = true, Font = new Font("Segoe UI Semibold", 21), ForeColor = Ink, Location = new Point(0, 0) };
        var badgeLbl = new Label { Text = "v1.1.0", AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Accent, Location = new Point(168, 10) };
        var subtitleLbl = new Label { Text = "Find and review exact duplicate files. Reclaim storage safely.", AutoSize = true, ForeColor = Muted, Location = new Point(2, 40) };
        var about = MakeButton("About", true);
        about.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        about.Location = new Point(Width - 170, 8);
        about.Click += (_, _) => MessageBox.Show(this,
            "DedupeLight 1.1.0\nLess clutter. More space.\n\nCreated by Ismail Hossain · MIT license\nExact duplicates verified with SHA-256 · Local scanning\n\nKeyboard Shortcuts:\n• Space: Toggle select\n• Enter: Open file\n• Delete: Recycle selected\n• Ctrl+F: Focus search\n• Ctrl+P: Toggle inspector\n• Ctrl+C: Copy path\n• F5: Rescan\n\ngithub.com/smile-plzz/dedupe-light",
            "About DedupeLight", MessageBoxButtons.OK, MessageBoxIcon.Information);
        header.Controls.AddRange([titleLbl, badgeLbl, subtitleLbl, about]);
        header.Resize += (_, _) => about.Location = new Point(Math.Max(0, header.ClientSize.Width - about.Width - 4), 8);
        layout.Controls.Add(header, 0, 0);

        // Location SurfaceCard
        var location = new SurfaceCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, BackColor = Surface };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++) fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        pathBox.Margin = new Padding(0, 4, 10, 0);
        pathBox.AccessibleName = "Folder or drive to scan";
        fields.Controls.Add(pathBox, 0, 0);
        fields.Controls.Add(browse, 1, 0);
        fields.Controls.Add(scan, 2, 0);
        fields.Controls.Add(cancel, 3, 0);

        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        options.Controls.Add(new Label { Text = "Minimum size", AutoSize = true, ForeColor = Muted, Padding = new Padding(0, 5, 6, 0) });
        options.Controls.Add(minimumSize);
        options.Controls.Add(new Label { Text = "MB", AutoSize = true, ForeColor = Muted, Padding = new Padding(0, 5, 14, 0) });
        options.Controls.Add(hiddenSystem);
        options.Controls.Add(manageExclusions);
        fields.SetColumnSpan(options, 4);
        fields.Controls.Add(options, 0, 1);
        location.Controls.Add(fields);
        layout.Controls.Add(location, 0, 1);

        // Filter & Sort bar
        var resultHead = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(2, 6, 0, 4), ColumnCount = 3 };
        resultHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        resultHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        resultHead.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var labels = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        labels.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        labels.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        summary.Dock = DockStyle.Fill; summary.AutoSize = false; summary.AutoEllipsis = true;
        status.Dock = DockStyle.Fill; status.AutoSize = false; status.AutoEllipsis = true;
        labels.Controls.Add(summary, 0, 0);
        labels.Controls.Add(status, 0, 1);
        resultHead.Controls.Add(labels, 0, 0);

        // Category Filter Chips
        SetupFilterChips();
        resultHead.Controls.Add(filterChipsPanel, 1, 0);

        var searchSortPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
        searchSortPanel.Controls.AddRange([togglePreview, sortOrderBox, searchBox]);
        resultHead.Controls.Add(searchSortPanel, 2, 0);
        layout.Controls.Add(resultHead, 0, 2);

        // Split view (Grid host + Preview Inspector)
        var gridHost = new SurfaceCard { Dock = DockStyle.Fill, Padding = new Padding(8), Margin = Padding.Empty };
        gridHost.Controls.Add(grid);
        gridHost.Controls.Add(emptyState);

        var inspectorCard = new SurfaceCard { Dock = DockStyle.Fill, Padding = new Padding(4), Margin = Padding.Empty };
        inspectorCard.Controls.Add(inspector);

        splitView.Panel1.Controls.Add(gridHost);
        splitView.Panel2.Controls.Add(inspectorCard);
        layout.Controls.Add(splitView, 0, 3);

        // Details line
        layout.Controls.Add(fileDetails, 0, 4);

        // Action Toolbar
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 4, 0, 0), Margin = Padding.Empty };
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        selectionRow.Controls.AddRange([keepRule, selectExtras, clearSelection, selectedSummary]);

        var toolsRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        toolsRow.Controls.AddRange([recycle, move, copy, previewBtn, open, reveal, export, exportJson]);

        var protectionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        protectionRow.Controls.AddRange([excludeFile, excludeFolder, new Label { Text = "Intentional copies? Exclude files, folders, or wildcard extensions.", AutoSize = true, ForeColor = Muted, Padding = new Padding(0, 6, 0, 0) }]);

        actions.Controls.Add(selectionRow, 0, 0);
        actions.Controls.Add(toolsRow, 0, 1);
        actions.Controls.Add(protectionRow, 0, 2);
        layout.Controls.Add(actions, 0, 5);

        hints.SetToolTip(togglePreview, "Toggle inline preview inspector pane (Ctrl+P)");
        hints.SetToolTip(previewBtn, "Open side-by-side comparison for duplicates in this set");
        hints.SetToolTip(excludeFile, "Skip selected files in current and future scans");
        hints.SetToolTip(excludeFolder, "Skip containing folder recursively");

        Controls.Add(layout);
        Controls.Add(progress);
        ResumeLayout(true);
    }

    void SetupFilterChips()
    {
        string[] cats = ["All", "Images", "Videos", "Audio", "Documents", "Code", "Archives", "Other"];
        foreach (var cat in cats)
        {
            var chip = new FilterChipButton { Text = cat, CategoryKey = cat, Selected = cat == "All" };
            chip.Click += (_, _) =>
            {
                foreach (var c in chipButtons) c.Selected = (c == chip);
                activeCategory = chip.CategoryKey;
                ApplyFilter();
            };
            chipButtons.Add(chip);
            filterChipsPanel.Controls.Add(chip);
        }
    }

    void ConfigureGrid()
    {
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 36;
        grid.AllowUserToResizeRows = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle = new()
        {
            BackColor = Color.FromArgb(243, 244, 246),
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 9),
            SelectionBackColor = Color.FromArgb(243, 244, 246),
            Padding = new Padding(4)
        };
        grid.DefaultCellStyle = new()
        {
            BackColor = Surface,
            ForeColor = Ink,
            SelectionBackColor = Color.FromArgb(232, 242, 255),
            SelectionForeColor = Ink,
            Padding = new Padding(4),
            NullValue = ""
        };

        grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(DuplicateItem.Selected), HeaderText = "", Width = 38 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.Group), HeaderText = "Set", Width = 64, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.Name), HeaderText = "File", Width = 230, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.Category), HeaderText = "Type", Width = 84, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.Folder), HeaderText = "Location", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.SizeText), HeaderText = "Size", Width = 96, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DuplicateItem.ModifiedText), HeaderText = "Modified", Width = 138, ReadOnly = true });

        // Visual group tinting & group separation
        grid.RowPrePaint += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count && grid.Rows[e.RowIndex].DataBoundItem is DuplicateItem item)
            {
                var isEvenGroup = item.Group % 2 == 0;
                var baseBg = isEvenGroup ? Color.FromArgb(250, 251, 253) : Color.White;
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = baseBg;
            }
        };

        grid.DataSource = source;
    }

    void ConfigureContextMenu()
    {
        var openItem = new ToolStripMenuItem("Open in default app", null, (_, _) => OpenFile()) { ShortcutKeyDisplayString = "Enter" };
        var revealItem = new ToolStripMenuItem("Show in File Explorer", null, (_, _) => Reveal());
        var compareItem = new ToolStripMenuItem("Compare with other copies…", null, (_, _) => ReviewFile());
        var copyPath = new ToolStripMenuItem("Copy full path", null, (_, _) => CopyPathToClipboard()) { ShortcutKeyDisplayString = "Ctrl+C" };
        var copyHash = new ToolStripMenuItem("Copy SHA-256 hash", null, (_, _) => CopyHashToClipboard());

        var toggleSelect = new ToolStripMenuItem("Toggle mark for recycle", null, (_, _) => ToggleCurrentSelect()) { ShortcutKeyDisplayString = "Space" };
        var keepOnlyThis = new ToolStripMenuItem("Keep ONLY this copy in set", null, (_, _) => KeepOnlyThisInSet());
        var keepAllInSet = new ToolStripMenuItem("Keep ALL copies in set", null, (_, _) => KeepAllInSet());

        var excludeFileItem = new ToolStripMenuItem("Exclude this file", null, (_, _) => ExcludeChosen(false));
        var excludeFolderItem = new ToolStripMenuItem("Exclude folder", null, (_, _) => ExcludeChosen(true));
        var excludeExtItem = new ToolStripMenuItem("Exclude this file extension", null, (_, _) => ExcludeCurrentExtension());

        var recycleItem = new ToolStripMenuItem("Recycle selected", null, async (_, _) => await RecycleAsync()) { ShortcutKeyDisplayString = "Del" };

        gridMenu.Items.AddRange([
            openItem,
            revealItem,
            compareItem,
            new ToolStripSeparator(),
            copyPath,
            copyHash,
            new ToolStripSeparator(),
            toggleSelect,
            keepOnlyThis,
            keepAllInSet,
            new ToolStripSeparator(),
            excludeFileItem,
            excludeFolderItem,
            excludeExtItem,
            new ToolStripSeparator(),
            recycleItem
        ]);

        grid.ContextMenuStrip = gridMenu;
    }

    void WireEvents()
    {
        browse.Click += (_, _) => ChooseFolder();
        scan.Click += async (_, _) => await ScanAsync();
        cancel.Click += (_, _) => cts?.Cancel();
        selectExtras.Click += (_, _) => SelectExtras();
        clearSelection.Click += (_, _) => { allItems.ForEach(x => x.Selected = false); grid.Refresh(); UpdateActions(); };
        recycle.Click += async (_, _) => await RecycleAsync();
        move.Click += async (_, _) => await TransferAsync(true);
        copy.Click += async (_, _) => await TransferAsync(false);
        open.Click += (_, _) => OpenFile();
        reveal.Click += (_, _) => Reveal();
        export.Click += async (_, _) => await ExportCsvAsync();
        exportJson.Click += async (_, _) => await ExportJsonAsync();
        previewBtn.Click += (_, _) => ReviewFile();
        togglePreview.Click += (_, _) => TogglePreviewPane();
        excludeFile.Click += (_, _) => ExcludeChosen(false);
        excludeFolder.Click += (_, _) => ExcludeChosen(true);
        manageExclusions.Click += (_, _) => ManageExclusions();
        sortOrderBox.SelectedIndexChanged += (_, _) => { if (!busy) ApplyFilter(); };

        searchBox.TextChanged += (_, _) => { filterDelay.Stop(); filterDelay.Start(); };
        filterDelay.Tick += (_, _) => { filterDelay.Stop(); if (!busy) ApplyFilter(); };

        grid.SelectionChanged += async (_, _) =>
        {
            UpdateActions();
            await inspector.SetItemAsync(Current());
        };
        grid.CellValueChanged += (_, _) => UpdateActions();
        grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.CellDoubleClick += (_, e) => { if (!busy && e.RowIndex >= 0 && e.ColumnIndex != 0 && grid.Rows[e.RowIndex].DataBoundItem is DuplicateItem item) OpenItem(item); };
        grid.ColumnHeaderMouseClick += (_, e) => SortBy(e.ColumnIndex);

        inspector.CompareRequested += _ => ReviewFile();

        pathBox.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await ScanAsync(); };
        DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
        DragDrop += (_, e) => AcceptDrop(e);

        KeyDown += async (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.F) { searchBox.Focus(); searchBox.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.P) { TogglePreviewPane(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.C && grid.Focused) { CopyPathToClipboard(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5 && !busy) { await ScanAsync(); e.Handled = true; }
            else if (e.KeyCode == Keys.Space && grid.Focused && !grid.IsCurrentCellInEditMode) { ToggleCurrentSelect(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete && !busy && !searchBox.Focused && !pathBox.Focused) { await RecycleAsync(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter && grid.Focused && !grid.IsCurrentCellInEditMode) { OpenFile(); e.Handled = true; }
        };

        FormClosing += (_, e) =>
        {
            if (busy) { e.Cancel = true; cts?.Cancel(); status.Text = "Cancelling operation • close again when it finishes"; }
            else SavePreferences();
        };
    }

    void TogglePreviewPane()
    {
        splitView.Panel2Collapsed = !splitView.Panel2Collapsed;
        togglePreview.Text = splitView.Panel2Collapsed ? "Inspector ◫" : "Hide Inspector ✕";
    }

    void ToggleCurrentSelect()
    {
        if (Current() is { } item)
        {
            item.Selected = !item.Selected;
            grid.Refresh();
            UpdateActions();
        }
    }

    void KeepOnlyThisInSet()
    {
        if (Current() is not { } current) return;
        foreach (var item in allItems.Where(x => x.Group == current.Group))
        {
            item.Selected = (item.Path != current.Path);
        }
        grid.Refresh();
        UpdateActions();
        status.Text = $"Kept {current.Name} in Set #{current.Group}; marked other copies for recycle";
    }

    void KeepAllInSet()
    {
        if (Current() is not { } current) return;
        foreach (var item in allItems.Where(x => x.Group == current.Group))
        {
            item.Selected = false;
        }
        grid.Refresh();
        UpdateActions();
        status.Text = $"Cleared marks for all copies in Set #{current.Group}";
    }

    void CopyPathToClipboard()
    {
        var chosen = Chosen();
        if (chosen.Count == 0 && Current() is { } item) chosen = [item];
        if (chosen.Count == 0) return;
        Clipboard.SetText(string.Join(Environment.NewLine, chosen.Select(x => x.Path)));
        status.Text = $"Copied {chosen.Count} path(s) to clipboard";
    }

    void CopyHashToClipboard()
    {
        if (Current() is { } item)
        {
            Clipboard.SetText(item.Hash);
            status.Text = $"Copied SHA-256 for {item.Name}";
        }
    }

    void ExcludeCurrentExtension()
    {
        if (Current() is not { } item || string.IsNullOrEmpty(item.Extension)) return;
        var ext = "*" + item.Extension;
        if (MessageBox.Show(this, $"Exclude all files with extension '{ext}' from current and future scans?", "Exclude by extension", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            exclusions.Add(new(ext, false, RuleKind.Extension, true));
            exclusions = ExclusionRules.Validate(exclusions);
            SavePreferences();
            ApplyExclusions();
            status.Text = $"Excluded extension {ext}";
        }
    }

    void ChooseFolder()
    {
        using var d = new FolderBrowserDialog { Description = "Select a folder or drive to scan", ShowNewFolderButton = false };
        if (d.ShowDialog() == DialogResult.OK) pathBox.Text = d.SelectedPath;
    }

    async Task ScanAsync()
    {
        if (busy) return;
        var root = pathBox.Text.Trim();
        if (!Directory.Exists(root))
        {
            MessageBox.Show("Choose an existing folder or drive.", "DedupeLight", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        cts = new();
        SetBusy(true);
        allItems = [];
        ApplyFilter();
        var reporter = new Progress<string>(s => { if (busy) status.Text = s; });
        try
        {
            var result = await DuplicateScanner.FindAsync(
                root,
                CachePath,
                new ScanOptions((long)minimumSize.Value * 1024 * 1024, hiddenSystem.Checked, exclusions.ToArray()),
                reporter,
                cts.Token);

            hasScanned = true;
            allItems = result.Items;
            ApplyFilter();
            var optimization = result.SizeCandidates - result.FullHashes - result.CacheHits;
            status.Text = $"Scanned {result.FilesSeen:N0} files • {result.FullHashes:N0} full reads • {result.CacheHits:N0} cached • {Math.Max(0, optimization):N0} rejected by sampling" +
                          (result.Skipped > 0 ? $" • {result.Skipped:N0} skipped" : "");
        }
        catch (OperationCanceledException) { status.Text = "Scan cancelled"; }
        catch (Exception ex) { status.Text = "Scan failed"; MessageBox.Show(ex.Message, "Could not scan", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            cts?.Dispose();
            cts = null;
            SetBusy(false);
            UpdateSummary();
        }
    }

    void ApplyFilter()
    {
        var q = searchBox.Text.Trim();
        IEnumerable<DuplicateItem> shown = allItems;

        // Search text filter
        if (!string.IsNullOrEmpty(q))
            shown = shown.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Folder.Contains(q, StringComparison.OrdinalIgnoreCase));

        // Category chip filter
        if (activeCategory != "All")
            shown = shown.Where(x => x.Category.ToString().Equals(activeCategory, StringComparison.OrdinalIgnoreCase));

        // Set-Aware Sorting: keeps duplicate sets coherent
        var groups = shown.GroupBy(x => x.Hash);
        IOrderedEnumerable<IGrouping<string, DuplicateItem>> orderedGroups = sortOrderBox.SelectedIndex switch
        {
            1 => groups.OrderBy(g => g.First().Size),                                      // Smallest sets
            2 => groups.OrderByDescending(g => g.Count()).ThenByDescending(g => g.First().Size), // Most copies
            3 => groups.OrderBy(g => g.First().Name),                                      // Alphabetical
            4 => groups.OrderByDescending(g => g.Max(x => x.ModifiedUtc)),                 // Newest
            5 => groups.OrderBy(g => g.Min(x => x.ModifiedUtc)),                           // Oldest
            _ => groups.OrderByDescending(g => g.First().Size * (g.Count() - 1))          // Largest reclaimable sets
        };

        var flatList = new List<DuplicateItem>();
        foreach (var g in orderedGroups)
        {
            // Within group order: keep sorted by keepRule or modification
            var withinGroup = sortAscending
                ? g.OrderBy(x => x.ModifiedUtc).ThenBy(x => x.Path)
                : g.OrderByDescending(x => x.ModifiedUtc).ThenBy(x => x.Path);
            flatList.AddRange(withinGroup);
        }

        rebinding = true;
        try { source.DataSource = flatList; }
        finally { rebinding = false; }

        UpdateEmptyState();
        UpdateSummary();
        UpdateActions();
    }

    void UpdateEmptyState()
    {
        emptyState.Visible = source.Count == 0;
        grid.Visible = !emptyState.Visible;
        emptyState.Text = busy
            ? "Finding exact copies…\n\nYour files stay untouched. Use Cancel to stop the scan."
            : allItems.Count > 0
                ? "No matches for active filter.\n\nTry another search keyword or switch category chips."
                : hasScanned
                    ? "All clear.\n\nNo exact duplicates found with these scan settings."
                    : "A little more room for what matters.\n\nChoose a folder or drop one here to find exact duplicates.";
    }

    void UpdateSummary()
    {
        var groups = allItems.Select(x => x.Group).Distinct().Count();
        var reclaim = allItems.GroupBy(x => x.Hash).Sum(g => g.First().Size * (g.Count() - 1));
        summary.Text = groups == 0
            ? (cts is null ? "No duplicates to review" : "Scanning…")
            : $"{groups:N0} duplicate sets  •  {allItems.Count:N0} files  •  {FormatSize(reclaim)} recoverable";
    }

    void SetBusy(bool busy)
    {
        this.busy = busy;
        manageExclusions.Enabled = !busy;
        grid.Enabled = searchBox.Enabled = sortOrderBox.Enabled = !busy;
        foreach (var c in chipButtons) c.Enabled = !busy;
        pathBox.Enabled = browse.Enabled = scan.Enabled = minimumSize.Enabled = hiddenSystem.Enabled = keepRule.Enabled = !busy;
        cancel.Visible = cancel.Enabled = busy && cts is not null;
        progress.Visible = busy;
        UpdateEmptyState();
        UpdateActions();
    }

    void UpdateActions()
    {
        if (rebinding) return;
        var available = allItems.Count > 0 && !busy;
        var marked = allItems.Where(x => x.Selected).ToList();
        var chosen = marked.Count > 0 || grid.SelectedRows.Count > 0;

        selectExtras.Enabled = clearSelection.Enabled = export.Enabled = exportJson.Enabled = available;
        recycle.Enabled = move.Enabled = copy.Enabled = available && chosen;
        previewBtn.Enabled = open.Enabled = reveal.Enabled = available && grid.SelectedRows.Count >= 1;
        excludeFile.Enabled = excludeFolder.Enabled = available && chosen;

        var current = Current();
        fileDetails.Text = current is null
            ? "Select a row to inspect · Space to toggle · Enter to open · Del to recycle"
            : $"{current.Name}  ·  {current.SizeText}  ·  {current.Category}  ·  Set #{current.Group}  ·  {current.Path}";
        hints.SetToolTip(fileDetails, current?.Path ?? "");

        manageExclusions.Text = $"Excluded sources ({exclusions.Count})";
        selectedSummary.Text = marked.Count == 0 ? "" : $"{marked.Count:N0} marked for recycle • {FormatSize(marked.Sum(x => x.Size))}";
    }

    List<DuplicateItem> Chosen()
    {
        grid.EndEdit();
        var marked = allItems.Where(x => x.Selected).ToList();
        return marked.Count > 0
            ? marked
            : grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem as DuplicateItem).Where(x => x is not null).Cast<DuplicateItem>().Distinct().ToList();
    }

    DuplicateItem? Current() => grid.SelectedRows.Count == 1 ? grid.SelectedRows[0].DataBoundItem as DuplicateItem : null;

    void SelectExtras()
    {
        allItems.ForEach(x => x.Selected = false);
        foreach (var group in allItems.GroupBy(x => x.Hash))
        {
            IOrderedEnumerable<DuplicateItem> ordered = keepRule.SelectedIndex switch
            {
                1 => group.OrderByDescending(x => x.ModifiedUtc).ThenBy(x => x.Path), // Keep newest
                2 => group.OrderBy(x => x.Path.Length).ThenBy(x => x.Path),          // Keep shortest path
                3 => group.OrderByDescending(x => x.Path.Length).ThenBy(x => x.Path), // Keep longest path
                4 => group.OrderBy(x => x.ModifiedUtc),                              // Invert selection (handled below)
                _ => group.OrderBy(x => x.ModifiedUtc).ThenBy(x => x.Path)           // Keep oldest
            };

            if (keepRule.SelectedIndex == 4)
            {
                // Invert selection: pick all except the last
                foreach (var item in ordered.Take(group.Count() - 1)) item.Selected = true;
            }
            else
            {
                foreach (var item in ordered.Skip(1)) item.Selected = true;
            }
        }
        grid.Refresh();
        UpdateActions();
        status.Text = $"Selected extra copies • {keepRule.SelectedItem?.ToString()?.ToLowerInvariant()} in every set";
    }

    async Task RecycleAsync()
    {
        if (busy) return;
        var chosen = Chosen();
        if (chosen.Count == 0) return;

        var fullGroups = RecyclePolicy.FullySelectedGroups(allItems, chosen);
        if (fullGroups.Count > 0)
        {
            using var warning = new RecycleOverrideDialog(fullGroups, chosen);
            if (warning.ShowDialog(this) != DialogResult.OK) return;
        }

        var bytes = chosen.Sum(x => x.Size);
        if (MessageBox.Show(
            $"Move {chosen.Count} file(s) ({FormatSize(bytes)}) to the Recycle Bin?\n\nYou can restore them later.",
            "Confirm recycle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        SetBusy(true);
        status.Text = "Recycling files…";
        var failed = new List<string>();
        var recycled = new HashSet<DuplicateItem>();

        try
        {
            await Task.Run(() =>
            {
                foreach (var x in chosen)
                {
                    try
                    {
                        FileSystem.DeleteFile(x.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        if (!File.Exists(x.Path)) recycled.Add(x);
                        else failed.Add(x.Path);
                    }
                    catch
                    {
                        failed.Add(x.Path);
                    }
                }
            });

            allItems.RemoveAll(x => recycled.Contains(x));
            RemoveOrphans();
            ApplyFilter();
            status.Text = failed.Count == 0
                ? $"Recycled {chosen.Count} file(s) • {FormatSize(bytes)} reclaimed"
                : $"Finished • {failed.Count} file(s) could not be recycled";
        }
        finally
        {
            SetBusy(false);
        }
    }

    async Task TransferAsync(bool moving)
    {
        if (busy) return;
        var chosen = Chosen();
        if (chosen.Count == 0) return;

        using var d = new FolderBrowserDialog { Description = moving ? "Move selected files to" : "Copy selected files to" };
        if (d.ShowDialog() != DialogResult.OK) return;

        var destination = d.SelectedPath;
        var succeeded = new HashSet<DuplicateItem>();
        var failed = 0;
        SetBusy(true);
        status.Text = moving ? "Moving files…" : "Copying files…";

        try
        {
            await Task.Run(() =>
            {
                foreach (var x in chosen)
                {
                    try
                    {
                        if (string.Equals(Path.GetFullPath(x.Folder), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                            throw new IOException("Source and destination are the same folder.");
                        var target = UniquePath(destination, x.Name);
                        if (moving) File.Move(x.Path, target);
                        else File.Copy(x.Path, target);
                        succeeded.Add(x);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        failed++;
                    }
                }
            });

            if (moving)
            {
                allItems.RemoveAll(x => succeeded.Contains(x));
                RemoveOrphans();
                ApplyFilter();
            }
            status.Text = $"{(moving ? "Moved" : "Copied")} {succeeded.Count} file(s)" + (failed > 0 ? $" • {failed} skipped" : "");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "File operation failed"); }
        finally { SetBusy(false); }
    }

    void OpenFile()
    {
        if (!busy && Current() is { } item) OpenItem(item);
    }

    void OpenItem(DuplicateItem item)
    {
        try { Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open file"); }
    }

    void Reveal()
    {
        var x = Current();
        if (x is null) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{x.Path}\"") { UseShellExecute = true });
    }

    async Task ExportCsvAsync()
    {
        if (busy) return;
        using var d = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "duplicate-files.csv" };
        if (d.ShowDialog() != DialogResult.OK) return;
        var destination = d.FileName;
        var items = allItems.ToArray();
        SetBusy(true);
        status.Text = "Exporting CSV…";
        try
        {
            await Task.Run(() =>
            {
                using var writer = new StreamWriter(destination, false, new UTF8Encoding(true));
                writer.WriteLine("Set,File,Category,Folder,Bytes,SizeFormatted,Modified,SHA256");
                foreach (var x in items)
                    writer.WriteLine($"{x.Group},{Csv(x.Name)},{x.Category},{Csv(x.Folder)},{x.Size},{Csv(x.SizeText)},{x.ModifiedUtc:O},{x.Hash}");
            });
            status.Text = $"Exported {items.Length:N0} results to CSV";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not export CSV"); status.Text = "Export failed"; }
        finally { SetBusy(false); }
    }

    async Task ExportJsonAsync()
    {
        if (busy) return;
        using var d = new SaveFileDialog { Filter = "JSON file (*.json)|*.json", FileName = "duplicate-files.json" };
        if (d.ShowDialog() != DialogResult.OK) return;
        var destination = d.FileName;
        var items = allItems.ToArray();
        SetBusy(true);
        status.Text = "Exporting JSON…";
        try
        {
            await Task.Run(() =>
            {
                var exportData = items.GroupBy(x => x.Group).Select(g => new
                {
                    Set = g.Key,
                    Hash = g.First().Hash,
                    FileCount = g.Count(),
                    FileSize = g.First().Size,
                    Copies = g.Select(c => new { c.Name, c.Folder, c.Path, c.ModifiedUtc, c.Selected }).ToList()
                });
                File.WriteAllText(destination, JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true }));
            });
            status.Text = $"Exported {items.Length:N0} results to JSON";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not export JSON"); status.Text = "Export failed"; }
        finally { SetBusy(false); }
    }

    void SortBy(int column)
    {
        if (column == 0) return;
        if (sortColumn == column) sortAscending = !sortAscending;
        else { sortColumn = column; sortAscending = true; }

        foreach (DataGridViewColumn item in grid.Columns) item.HeaderCell.SortGlyphDirection = SortOrder.None;
        grid.Columns[column].HeaderCell.SortGlyphDirection = sortAscending ? SortOrder.Ascending : SortOrder.Descending;

        // Map column to sort order box
        sortOrderBox.SelectedIndex = column switch
        {
            1 => 0, // Set
            2 => 3, // Name
            4 => 0, // Location
            5 => sortAscending ? 1 : 0, // Size
            6 => sortAscending ? 5 : 4, // Modified
            _ => 0
        };
        ApplyFilter();
    }

    void AcceptDrop(DragEventArgs e)
    {
        if (busy) return;
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0) return;
        var path = paths[0];
        if (File.Exists(path)) path = Path.GetDirectoryName(path) ?? path;
        if (Directory.Exists(path))
        {
            pathBox.Text = path;
            status.Text = "Folder ready • select Scan now";
        }
    }

    void LoadPreferences()
    {
        var settings = AppSettings.Load(SettingsPath);
        try { exclusions = ExclusionRules.Validate(settings.Exclusions ?? []); }
        catch { exclusions = []; }
        if (Directory.Exists(settings.LastPath)) pathBox.Text = settings.LastPath;
        minimumSize.Value = Math.Clamp(settings.MinimumMegabytes, minimumSize.Minimum, minimumSize.Maximum);
        hiddenSystem.Checked = settings.IncludeHiddenSystem;
        keepRule.SelectedIndex = Math.Clamp(settings.KeepRule, 0, keepRule.Items.Count - 1);
        splitView.Panel2Collapsed = !settings.ShowPreview;
        togglePreview.Text = splitView.Panel2Collapsed ? "Inspector ◫" : "Hide Inspector ✕";
    }

    void SavePreferences() =>
        new AppSettings(
            pathBox.Text.Trim(),
            minimumSize.Value,
            hiddenSystem.Checked,
            keepRule.SelectedIndex,
            exclusions,
            !splitView.Panel2Collapsed,
            sortOrderBox.SelectedIndex,
            activeCategory).Save(SettingsPath);

    void ReviewFile()
    {
        if (busy || Current() is not { } item) return;
        var siblings = allItems.Where(x => x.Group == item.Group).ToList();
        using var review = new FileReviewDialog(item, siblings);
        if (review.ShowDialog(this) == DialogResult.OK && review.ExcludeFolder is bool folder && review.TargetToExclude is { } target)
        {
            AddExclusions([target], folder);
        }
        else
        {
            grid.Refresh();
            UpdateActions();
        }
    }

    void ExcludeChosen(bool folder) { if (!busy) AddExclusions(Chosen(), folder); }

    void AddExclusions(IReadOnlyList<DuplicateItem> items, bool folder)
    {
        if (items.Count == 0) return;
        var rules = ExclusionRules.Validate(items.Select(x => new ExclusionRule(folder ? x.Folder : x.Path, folder, RuleKind.Path, true)));
        if (MessageBox.Show(this,
            $"Exclude {rules.Count} {(folder ? "folder(s), including their subfolders" : "file(s)")} from current results and future scans?\n\nFiles remain untouched on disk. Manage or restore them in Excluded sources.",
            "Exclude intentional duplicates", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        exclusions = ExclusionRules.Validate(exclusions.Concat(rules));
        SavePreferences();
        ApplyExclusions();
        status.Text = "Sources excluded · files remain untouched";
    }

    void ApplyExclusions()
    {
        allItems.RemoveAll(x => ExclusionRules.Matches(x.Path, exclusions));
        RemoveOrphans();
        ApplyFilter();
    }

    void ManageExclusions()
    {
        if (busy) return;
        using var dialog = new ExclusionDialog(exclusions);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        exclusions = dialog.Rules;
        SavePreferences();
        ApplyExclusions();
        status.Text = "Exclusions updated · rescan to restore sources removed from the list";
    }

    static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    static string UniquePath(string folder, string name)
    {
        var p = Path.Combine(folder, name);
        if (!File.Exists(p)) return p;
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (var i = 2; ; i++)
        {
            p = Path.Combine(folder, $"{stem} ({i}){ext}");
            if (!File.Exists(p)) return p;
        }
    }

    void RemoveOrphans()
    {
        allItems = allItems.GroupBy(x => x.Hash).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            filterDelay.Dispose();
            hints.Dispose();
            source.Dispose();
            cts?.Dispose();
            gridMenu.Dispose();
            inspector.Dispose();
        }
        base.Dispose(disposing);
    }

    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }
}
