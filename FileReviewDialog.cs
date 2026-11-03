using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text;

namespace DedupeLight;

sealed class FileReviewDialog : Form
{
    readonly DuplicateItem leftItem;
    readonly IReadOnlyList<DuplicateItem> siblings;
    DuplicateItem? rightItem;

    readonly ComboBox rightSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Height = 32 };
    readonly Panel leftPreviewHost = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(249, 250, 251), Padding = new Padding(8) };
    readonly Panel rightPreviewHost = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(249, 250, 251), Padding = new Padding(8) };

    readonly PictureBox leftPic = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Visible = false };
    readonly PictureBox rightPic = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Visible = false };

    readonly TextBox leftText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 8.5f), Visible = false, WordWrap = false, BorderStyle = BorderStyle.None };
    readonly TextBox rightText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 8.5f), Visible = false, WordWrap = false, BorderStyle = BorderStyle.None };

    readonly Label leftLabel = new() { Dock = DockStyle.Top, Height = 56, Font = new Font("Segoe UI", 9) };
    readonly Label rightLabel = new() { Dock = DockStyle.Top, Height = 56, Font = new Font("Segoe UI", 9) };

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool? ExcludeFolder { get; private set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DuplicateItem? TargetToExclude { get; private set; }

    public FileReviewDialog(DuplicateItem item, IReadOnlyList<DuplicateItem>? allGroupItems = null)
    {
        leftItem = item;
        TargetToExclude = item;
        siblings = allGroupItems?.Where(x => x.Path != item.Path).ToList() ?? [];
        rightItem = siblings.FirstOrDefault();

        Text = $"Compare Duplicate Copies · Set #{item.Group}";
        Size = new Size(1060, 720);
        MinimumSize = new Size(840, 560);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(248, 249, 251);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        // Top bar
        var topBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var leftTitle = new Label { Text = $"Copy 1 (Active): {item.Name}", Font = new Font("Segoe UI Semibold", 10.5f), ForeColor = Color.FromArgb(17, 24, 39), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var rightTitle = new Label { Text = "Compare with copy:", Font = new Font("Segoe UI Semibold", 10f), ForeColor = Color.FromArgb(107, 114, 128), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

        foreach (var s in siblings) rightSelector.Items.Add($"{s.Folder} ({s.ModifiedText})");
        if (rightSelector.Items.Count > 0) rightSelector.SelectedIndex = 0;
        rightSelector.SelectedIndexChanged += async (_, _) =>
        {
            if (rightSelector.SelectedIndex >= 0 && rightSelector.SelectedIndex < siblings.Count)
            {
                rightItem = siblings[rightSelector.SelectedIndex];
                await LoadRightPreviewAsync();
            }
        };

        topBar.Controls.Add(leftTitle, 0, 0);
        topBar.Controls.Add(rightTitle, 1, 0);
        topBar.Controls.Add(rightSelector, 2, 0);
        layout.Controls.Add(topBar, 0, 0);

        // Split comparison area
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var leftCard = new SurfaceCard { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        leftPreviewHost.Controls.Add(leftPic);
        leftPreviewHost.Controls.Add(leftText);
        leftCard.Controls.Add(leftPreviewHost);
        leftCard.Controls.Add(leftLabel);

        var rightCard = new SurfaceCard { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        rightPreviewHost.Controls.Add(rightPic);
        rightPreviewHost.Controls.Add(rightText);
        rightCard.Controls.Add(rightPreviewHost);
        rightCard.Controls.Add(rightLabel);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        layout.Controls.Add(split, 0, 1);

        // Buttons
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), WrapContents = false };
        var selectLeftBtn = new SoftButton { Text = "Keep Left (Mark Right)", Primary = true };
        selectLeftBtn.Click += (_, _) =>
        {
            leftItem.Selected = false;
            if (rightItem is not null) rightItem.Selected = true;
            MessageBox.Show(this, "Marked right copy for recycling; left copy kept safe.", "Selection Updated");
        };

        var selectRightBtn = new SoftButton { Text = "Keep Right (Mark Left)" };
        selectRightBtn.Click += (_, _) =>
        {
            leftItem.Selected = true;
            if (rightItem is not null) rightItem.Selected = false;
            MessageBox.Show(this, "Marked left copy for recycling; right copy kept safe.", "Selection Updated");
        };

        var openLeftBtn = new SoftButton { Text = "Open Left in App" };
        openLeftBtn.Click += (_, _) => Launch(leftItem.Path);

        var openRightBtn = new SoftButton { Text = "Open Right in App" };
        openRightBtn.Click += (_, _) => { if (rightItem is not null) Launch(rightItem.Path); };

        var excludeLeftBtn = new SoftButton { Text = "Exclude Left File" };
        excludeLeftBtn.Click += (_, _) =>
        {
            TargetToExclude = leftItem;
            ExcludeFolder = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        var excludeLeftDirBtn = new SoftButton { Text = "Exclude Left Folder" };
        excludeLeftDirBtn.Click += (_, _) =>
        {
            TargetToExclude = leftItem;
            ExcludeFolder = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        var closeBtn = new SoftButton { Text = "Close", DialogResult = DialogResult.Cancel };

        buttons.Controls.AddRange([selectLeftBtn, selectRightBtn, openLeftBtn, openRightBtn, excludeLeftBtn, excludeLeftDirBtn, closeBtn]);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        CancelButton = closeBtn;

        Shown += async (_, _) =>
        {
            await LoadLeftPreviewAsync();
            await LoadRightPreviewAsync();
        };
    }

    static void Launch(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open file"); }
    }

    async Task LoadLeftPreviewAsync()
    {
        leftLabel.Text = $"Folder: {leftItem.Folder}\nSize: {leftItem.SizeText} · Modified: {leftItem.ModifiedText}";
        await RenderPreviewAsync(leftItem, leftPic, leftText, leftPreviewHost);
    }

    async Task LoadRightPreviewAsync()
    {
        if (rightItem is null)
        {
            rightLabel.Text = "No other copy in this set.";
            rightPic.Visible = false;
            rightText.Visible = false;
            return;
        }
        rightLabel.Text = $"Folder: {rightItem.Folder}\nSize: {rightItem.SizeText} · Modified: {rightItem.ModifiedText}";
        await RenderPreviewAsync(rightItem, rightPic, rightText, rightPreviewHost);
    }

    static async Task RenderPreviewAsync(DuplicateItem item, PictureBox pic, TextBox txt, Panel host)
    {
        try
        {
            if (item.Category == FileCategory.Images)
            {
                var img = await Task.Run(() =>
                {
                    if (new FileInfo(item.Path).Length > 40 * 1024 * 1024) throw new IOException("Image too large.");
                    using var s = File.OpenRead(item.Path);
                    using var orig = Image.FromStream(s);
                    var scale = Math.Min(1d, Math.Min(600d / orig.Width, 500d / orig.Height));
                    var thumb = new Bitmap(Math.Max(1, (int)(orig.Width * scale)), Math.Max(1, (int)(orig.Height * scale)));
                    using var g = Graphics.FromImage(thumb);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(orig, 0, 0, thumb.Width, thumb.Height);
                    return (Image)thumb;
                });
                pic.Image?.Dispose();
                pic.Image = img;
                pic.Visible = true;
                txt.Visible = false;
            }
            else if (item.Category == FileCategory.Code || item.Category == FileCategory.Documents ||
                     item.Extension is ".txt" or ".log" or ".json" or ".xml" or ".csv" or ".md")
            {
                var lines = await Task.Run(() =>
                {
                    return string.Join(Environment.NewLine, File.ReadLines(item.Path).Take(60));
                });
                txt.Text = lines;
                txt.Visible = true;
                pic.Visible = false;
            }
            else
            {
                txt.Text = $"[Binary file: {item.Extension.ToUpperInvariant()} · {item.SizeText}]\nUse 'Open in App' to review.";
                txt.Visible = true;
                pic.Visible = false;
            }
        }
        catch (Exception ex)
        {
            txt.Text = $"Preview unavailable: {ex.Message}";
            txt.Visible = true;
            pic.Visible = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            leftPic.Image?.Dispose();
            rightPic.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
