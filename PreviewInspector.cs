using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text;

namespace DedupeLight;

sealed class PreviewInspector : UserControl
{
    readonly Label title = new()
    {
        Dock = DockStyle.Top,
        Font = new Font("Segoe UI Semibold", 11),
        ForeColor = Color.FromArgb(17, 24, 39),
        Height = 28,
        AutoEllipsis = true
    };

    readonly Label subtitle = new()
    {
        Dock = DockStyle.Top,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(107, 114, 128),
        Height = 24,
        AutoEllipsis = true
    };

    readonly Panel previewHost = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(249, 250, 251),
        Padding = new Padding(6)
    };

    readonly Label placeholder = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.FromArgb(156, 163, 175),
        Font = new Font("Segoe UI", 9.5f),
        Text = "Select a duplicate to inspect preview"
    };

    readonly PictureBox pictureBox = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        Visible = false
    };

    readonly TextBox textBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        Font = new Font("Consolas", 8.75f),
        BackColor = Color.White,
        ForeColor = Color.FromArgb(31, 41, 55),
        BorderStyle = BorderStyle.None,
        Visible = false,
        WordWrap = false
    };

    readonly Panel metaPanel = new()
    {
        Dock = DockStyle.Bottom,
        Height = 120,
        Padding = new Padding(0, 8, 0, 0)
    };

    readonly TextBox pathBox = new()
    {
        Dock = DockStyle.Top,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(243, 244, 246),
        Font = new Font("Segoe UI", 8.5f),
        ForeColor = Color.FromArgb(55, 65, 81),
        Height = 22
    };

    readonly Label hashLabel = new()
    {
        Dock = DockStyle.Top,
        Font = new Font("Consolas", 8f),
        ForeColor = Color.FromArgb(107, 114, 128),
        Height = 20,
        AutoEllipsis = true
    };

    readonly FlowLayoutPanel actionButtons = new()
    {
        Dock = DockStyle.Bottom,
        Height = 36,
        WrapContents = false
    };

    DuplicateItem? currentItem;
    CancellationTokenSource? previewCts;

    public event Action<DuplicateItem>? CompareRequested;

    public PreviewInspector()
    {
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = Color.White;
        Padding = new Padding(14);

        previewHost.Controls.Add(placeholder);
        previewHost.Controls.Add(pictureBox);
        previewHost.Controls.Add(textBox);

        var openBtn = new SoftButton { Text = "Open", Ghost = true };
        openBtn.Click += (_, _) =>
        {
            if (currentItem is not null)
            {
                try { Process.Start(new ProcessStartInfo(currentItem.Path) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open file"); }
            }
        };

        var revealBtn = new SoftButton { Text = "Explorer", Ghost = true };
        revealBtn.Click += (_, _) =>
        {
            if (currentItem is not null)
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{currentItem.Path}\"") { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        };

        var compareBtn = new SoftButton { Text = "Compare", Ghost = true };
        compareBtn.Click += (_, _) =>
        {
            if (currentItem is not null) CompareRequested?.Invoke(currentItem);
        };

        var copyPathBtn = new SoftButton { Text = "Copy path", Ghost = true };
        copyPathBtn.Click += (_, _) =>
        {
            if (currentItem is not null)
            {
                Clipboard.SetText(currentItem.Path);
            }
        };

        actionButtons.Controls.AddRange([openBtn, revealBtn, compareBtn, copyPathBtn]);

        metaPanel.Controls.Add(actionButtons);
        metaPanel.Controls.Add(hashLabel);
        metaPanel.Controls.Add(pathBox);

        Controls.Add(previewHost);
        Controls.Add(metaPanel);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    public async Task SetItemAsync(DuplicateItem? item)
    {
        previewCts?.Cancel();
        previewCts?.Dispose();
        previewCts = new CancellationTokenSource();
        var token = previewCts.Token;

        currentItem = item;
        if (item is null)
        {
            title.Text = "No file selected";
            subtitle.Text = "";
            pathBox.Text = "";
            hashLabel.Text = "";
            placeholder.Visible = true;
            placeholder.Text = "Select a duplicate file to inspect preview";
            pictureBox.Visible = false;
            textBox.Visible = false;
            return;
        }

        title.Text = item.Name;
        subtitle.Text = $"{item.SizeText}  ·  Set #{item.Group}  ·  {item.ModifiedText}";
        pathBox.Text = item.Path;
        hashLabel.Text = $"SHA-256: {item.Hash}";

        placeholder.Visible = true;
        placeholder.Text = "Loading preview…";
        pictureBox.Visible = false;
        textBox.Visible = false;

        var ext = item.Extension;
        try
        {
            if (item.Category == FileCategory.Images)
            {
                var image = await Task.Run(() =>
                {
                    if (new FileInfo(item.Path).Length > 40 * 1024 * 1024) throw new IOException("Image exceeds preview size limit.");
                    using var stream = File.OpenRead(item.Path);
                    using var original = Image.FromStream(stream);
                    var scale = Math.Min(1d, Math.Min(800d / original.Width, 600d / original.Height));
                    var thumb = new Bitmap(Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
                    using var g = Graphics.FromImage(thumb);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(original, 0, 0, thumb.Width, thumb.Height);
                    return (Image: (Image)thumb, original.Width, original.Height);
                }, token);

                if (token.IsCancellationRequested) { image.Image.Dispose(); return; }

                pictureBox.Image?.Dispose();
                pictureBox.Image = image.Image;
                pictureBox.Visible = true;
                placeholder.Visible = false;
                subtitle.Text = $"{item.SizeText}  ·  {image.Width}×{image.Height} px  ·  Set #{item.Group}";
            }
            else if (item.Category == FileCategory.Code || item.Category == FileCategory.Documents ||
                     ext is ".txt" or ".log" or ".json" or ".xml" or ".csv" or ".md" or ".yaml" or ".yml" or ".ini")
            {
                var snippet = await Task.Run(() =>
                {
                    if (new FileInfo(item.Path).Length > 10 * 1024 * 1024)
                    {
                        using var sr = new StreamReader(item.Path, Encoding.UTF8, true);
                        var lines = new List<string>();
                        for (var i = 0; i < 60 && !sr.EndOfStream; i++) lines.Add(sr.ReadLine() ?? "");
                        return (Text: string.Join(Environment.NewLine, lines) + "\n\n[... truncated preview ...]", Lines: lines.Count);
                    }
                    else
                    {
                        var allLines = File.ReadLines(item.Path).Take(80).ToList();
                        return (Text: string.Join(Environment.NewLine, allLines), Lines: allLines.Count);
                    }
                }, token);

                if (token.IsCancellationRequested) return;

                textBox.Text = snippet.Text;
                textBox.Visible = true;
                placeholder.Visible = false;
                subtitle.Text = $"{item.SizeText}  ·  Text ({snippet.Lines} lines)  ·  Set #{item.Group}";
            }
            else
            {
                // Hex view fallback for binary / other files
                var hex = await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(item.Path);
                    if (!fileInfo.Exists) return "File does not exist.";
                    using var stream = File.OpenRead(item.Path);
                    var buf = new byte[128];
                    var bytesRead = stream.Read(buf, 0, buf.Length);
                    var sb = new StringBuilder();
                    sb.AppendLine($"[Binary file: {item.Extension.ToUpperInvariant()} · {item.SizeText}]");
                    sb.AppendLine("Offset   00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F   ASCII");
                    sb.AppendLine(new string('-', 68));
                    for (var i = 0; i < bytesRead; i += 16)
                    {
                        sb.Append($"{i:X6}   ");
                        for (var j = 0; j < 16; j++)
                        {
                            if (i + j < bytesRead) sb.Append($"{buf[i + j]:X2} ");
                            else sb.Append("   ");
                            if (j == 7) sb.Append(' ');
                        }
                        sb.Append("  ");
                        for (var j = 0; j < 16; j++)
                        {
                            if (i + j < bytesRead)
                            {
                                var b = buf[i + j];
                                sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                            }
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                }, token);

                if (token.IsCancellationRequested) return;

                textBox.Text = hex;
                textBox.Visible = true;
                placeholder.Visible = false;
            }
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested) return;
            placeholder.Visible = true;
            placeholder.Text = $"Preview unavailable for this file.\n{ex.Message}";
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            previewCts?.Cancel();
            previewCts?.Dispose();
            pictureBox.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
