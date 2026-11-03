using System.Drawing.Drawing2D;

namespace DedupeLight;

sealed class SoftButton : Button
{
    readonly System.Windows.Forms.Timer animation = new() { Interval = 16 };
    float hover;
    bool pointerInside;
    bool isPressed;

    [System.ComponentModel.DefaultValue(false)]
    public bool Primary { get; set; }

    [System.ComponentModel.DefaultValue(false)]
    public bool Destructive { get; set; }

    [System.ComponentModel.DefaultValue(false)]
    public bool Ghost { get; set; }

    public SoftButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        AutoSize = true;
        MinimumSize = new Size(68, 34);
        Padding = new Padding(12, 0, 12, 0);
        Margin = new Padding(0, 0, 6, 0);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9.5f);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        animation.Tick += (_, _) =>
        {
            var target = pointerInside && Enabled ? 1f : 0f;
            hover += (target - hover) * .28f;
            if (Math.Abs(target - hover) < .02f) { hover = target; animation.Stop(); }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); pointerInside = true; animation.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pointerInside = false; isPressed = false; animation.Start(); }
    protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); isPressed = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); isPressed = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);

        Color normal, over, pressed;
        if (Primary)
        {
            normal = Color.FromArgb(15, 108, 189);
            over = Color.FromArgb(17, 94, 163);
            pressed = Color.FromArgb(12, 59, 94);
        }
        else if (Destructive)
        {
            normal = Color.FromArgb(254, 242, 242);
            over = Color.FromArgb(254, 226, 226);
            pressed = Color.FromArgb(252, 165, 165);
        }
        else if (Ghost)
        {
            normal = Color.Transparent;
            over = Color.FromArgb(238, 240, 244);
            pressed = Color.FromArgb(226, 229, 235);
        }
        else
        {
            normal = Color.FromArgb(243, 244, 246);
            over = Color.FromArgb(229, 231, 235);
            pressed = Color.FromArgb(209, 213, 219);
        }

        if (!Enabled)
        {
            normal = over = Color.FromArgb(245, 245, 247);
        }

        var baseColor = isPressed && Enabled ? pressed : normal;
        var fill = Color.FromArgb(
            (int)(baseColor.R + (over.R - baseColor.R) * hover),
            (int)(baseColor.G + (over.G - baseColor.G) * hover),
            (int)(baseColor.B + (over.B - baseColor.B) * hover));

        using var path = Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 8);
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);

        // Draw border for secondary button
        if (!Primary && !Destructive && !Ghost && Enabled)
        {
            using var borderPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
            e.Graphics.DrawPath(borderPen, path);
        }

        Color ink;
        if (!Enabled) ink = Color.FromArgb(156, 163, 175);
        else if (Primary) ink = Color.White;
        else if (Destructive) ink = Color.FromArgb(185, 28, 28);
        else ink = Color.FromArgb(31, 41, 55);

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Color.FromArgb(15, 108, 189), 1.5f);
            using var focus = Rounded(new RectangleF(3, 3, Width - 7, Height - 7), 6);
            e.Graphics.DrawPath(pen, focus);
        }
    }

    internal static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        var d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) animation.Dispose();
        base.Dispose(disposing);
    }
}

sealed class FilterChipButton : Button
{
    bool isSelected;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => isSelected;
        set { if (isSelected != value) { isSelected = value; Invalidate(); } }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string CategoryKey { get; init; } = "All";

    public FilterChipButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        AutoSize = true;
        Height = 32;
        Padding = new Padding(12, 0, 12, 0);
        Margin = new Padding(0, 0, 6, 0);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 9.25f);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);

        var bg = Selected ? Color.FromArgb(15, 108, 189) : Color.FromArgb(243, 244, 246);
        var ink = Selected ? Color.White : Color.FromArgb(75, 85, 99);

        using var path = SoftButton.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 14);
        using var brush = new SolidBrush(bg);
        e.Graphics.FillPath(brush, path);

        if (!Selected)
        {
            using var pen = new Pen(Color.FromArgb(229, 231, 235), 1f);
            e.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

sealed class SmoothGrid : DataGridView
{
    public SmoothGrid()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
}

sealed class SurfaceCard : Panel
{
    public SurfaceCard()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Padding = new Padding(16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var outline = SoftButton.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10);
        using var pen = new Pen(Color.FromArgb(229, 231, 235), 1f);
        e.Graphics.DrawPath(pen, outline);
    }
}
