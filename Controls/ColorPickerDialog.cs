using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 自定义取色器：深色科技感，与设置面板风格统一 =====
sealed class ColorPickerDialog : Form
{
    const int W = 360;
    const int H = 352;
    const int PadX = 20;
    const int HeaderH = 44;

    // 主色区（SV 平面 + 色相条）
    static readonly RectangleF SatRect = new(20, 54, 284, 168);
    static readonly RectangleF HueRect = new(318, 54, 22, 168);

    // 下部
    const int PreviewX = 20, PreviewY = 238, PreviewS = 48;
    const int FieldH = 24;
    const int BtnY = 300, BtnH = 34;

    static readonly Color Bg      = Color.FromArgb(0x1E, 0x1E, 0x24);
    static readonly Color Border  = Color.FromArgb(0x3E, 0x3E, 0x48);
    static readonly Color Accent  = Color.FromArgb(0x33, 0x8E, 0xF7);
    static readonly Color AccentD = Color.FromArgb(0x2B, 0x72, 0xC9);
    static readonly Color Title   = Color.FromArgb(0xFA, 0xFA, 0xFA);
    static readonly Color SubText = Color.FromArgb(0x8A, 0x8A, 0x94);
    static readonly Color FieldBg = Color.FromArgb(0x2A, 0x2A, 0x32);

    double _h, _s, _v;
    Color _selected;
    bool _draggingSv, _draggingHue;
    bool _syncing;
    int _hoverBtn = -1; // 0 取消 1 确定

    TextBox _hexBox = null!, _rBox = null!, _gBox = null!, _bBox = null!;

    public Color SelectedColor => _selected;

    public ColorPickerDialog(Color initial)
    {
        (_h, _s, _v) = ToHsv(initial);
        _selected = initial;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        TopMost = true;
        BackColor = Bg;
        Size = new Size(W, H);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        BuildFields();
        SyncFields();
    }

    void BuildFields()
    {
        // HEX 输入
        _hexBox = MakeField(new Rectangle(98, 240, 96, FieldH), "HEX");
        _hexBox.KeyPress += (_, e) => e.Handled = !(Uri.IsHexDigit(e.KeyChar) || e.KeyChar == '\b');
        _hexBox.TextChanged += (_, _) => { if (_syncing) return; OnHexEdited(); };

        // RGB 输入
        _rBox = MakeField(new Rectangle(98, 268, 42, FieldH), "R");
        _gBox = MakeField(new Rectangle(166, 268, 42, FieldH), "G");
        _bBox = MakeField(new Rectangle(234, 268, 42, FieldH), "B");
        void Wire(TextBox box) => box.KeyPress += (_, e) => e.Handled = !(char.IsDigit(e.KeyChar) || e.KeyChar == '\b');
        Wire(_rBox); Wire(_gBox); Wire(_bBox);
        _rBox.TextChanged += (_, _) => { if (_syncing) return; OnRgbEdited(); };
        _gBox.TextChanged += (_, _) => { if (_syncing) return; OnRgbEdited(); };
        _bBox.TextChanged += (_, _) => { if (_syncing) return; OnRgbEdited(); };

        Controls.Add(_hexBox);
        Controls.Add(_rBox);
        Controls.Add(_gBox);
        Controls.Add(_bBox);
    }

    TextBox MakeField(Rectangle bounds, string placeholder)
    {
        var box = new TextBox
        {
            Bounds = bounds,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = FieldBg,
            ForeColor = Title,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            MaxLength = 6,
        };
        return box;
    }

    void OnHexEdited()
    {
        string t = _hexBox.Text.Trim();
        if (t.Length != 6) return;
        if (int.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            SetFromRgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }

    void OnRgbEdited()
    {
        if (int.TryParse(_rBox.Text, out int r) && int.TryParse(_gBox.Text, out int g) && int.TryParse(_bBox.Text, out int b))
            SetFromRgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    void SetFromRgb(int r, int g, int b)
    {
        (_h, _s, _v) = ToHsv(Color.FromArgb(r, g, b));
        _selected = Color.FromArgb(r, g, b);
        _syncing = true;
        _hexBox.Text = _selected.R.ToString("X2") + _selected.G.ToString("X2") + _selected.B.ToString("X2");
        _rBox.Text = _selected.R.ToString();
        _gBox.Text = _selected.G.ToString();
        _bBox.Text = _selected.B.ToString();
        _syncing = false;
        Invalidate();
    }

    void SyncFields()
    {
        _syncing = true;
        _hexBox.Text = _selected.R.ToString("X2") + _selected.G.ToString("X2") + _selected.B.ToString("X2");
        _rBox.Text = _selected.R.ToString();
        _gBox.Text = _selected.G.ToString();
        _bBox.Text = _selected.B.ToString();
        _syncing = false;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, W + 1, H + 1, 24, 24));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
        else if (e.KeyCode == Keys.Enter) { DialogResult = DialogResult.OK; Close(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        using (var bg = new SolidBrush(Bg))
            g.FillRectangle(bg, 0, 0, W, H);

        using (var pen = new Pen(Border, 1f))
        using (var path = RoundedRect(0.5f, 0.5f, W - 1f, H - 1f, 12f))
            g.DrawPath(pen, path);

        DrawText(g, "自定义颜色", PadX, 0, W - PadX * 2, HeaderH, Title, 12f, FontStyle.Bold);

        DrawSatPanel(g);
        DrawHueBar(g);
        DrawPreview(g);

        // 输入区标签
        DrawText(g, "#", 82, 240, 16, FieldH, SubText, 9f, FontStyle.Regular, StringAlignment.Center);
        DrawText(g, "R", 82, 268, 16, FieldH, SubText, 9f, FontStyle.Regular, StringAlignment.Center);
        DrawText(g, "G", 150, 268, 16, FieldH, SubText, 9f, FontStyle.Regular, StringAlignment.Center);
        DrawText(g, "B", 218, 268, 16, FieldH, SubText, 9f, FontStyle.Regular, StringAlignment.Center);

        DrawButtons(g);
    }

    void DrawSatPanel(Graphics g)
    {
        var r = SatRect;
        // 水平：白 → 纯色相
        var huePure = FromHsv(_h, 1.0, 1.0);
        using (var hb = new LinearGradientBrush(r, Color.White, huePure, LinearGradientMode.Horizontal))
            g.FillRectangle(hb, r);
        // 垂直：透明 → 黑
        using (var vb = new LinearGradientBrush(r, Color.Transparent, Color.Black, LinearGradientMode.Vertical))
            g.FillRectangle(vb, r);

        // 边框
        using (var pen = new Pen(Border, 1f))
            g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);

        // 十字光标
        float px = r.Left + (float)_s * r.Width;
        float py = r.Top + (float)(1.0 - _v) * r.Height;
        using (var ringWhite = new Pen(Color.White, 3f))
        {
            g.DrawLine(ringWhite, px - 7, py, px + 7, py);
            g.DrawLine(ringWhite, px, py - 7, px, py + 7);
        }
        using (var ringDark = new Pen(Color.Black, 1.2f))
        {
            g.DrawLine(ringDark, px - 7, py, px + 7, py);
            g.DrawLine(ringDark, px, py - 7, px, py + 7);
        }
    }

    void DrawHueBar(Graphics g)
    {
        var r = HueRect;
        var cb = new ColorBlend
        {
            Colors = new[]
            {
                Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red,
            },
            Positions = new[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f },
        };
        using var lb = new LinearGradientBrush(r, Color.Black, Color.Black, LinearGradientMode.Vertical);
        lb.InterpolationColors = cb;
        g.FillRectangle(lb, r);

        // 指示线
        float hy = r.Top + (float)(1.0 - _h / 360.0) * r.Height;
        using var ind = new Pen(Title, 2f);
        g.DrawLine(ind, r.Left - 4, hy, r.Right + 4, hy);
    }

    void DrawPreview(Graphics g)
    {
        using var path = RoundedRect(PreviewX, PreviewY, PreviewS, PreviewS, 8);
        using (var b = new SolidBrush(_selected))
            g.FillPath(b, path);
        using (var pen = new Pen(Border, 1f))
            g.DrawPath(pen, path);
    }

    void DrawButtons(Graphics g)
    {
        DrawButton(g, new RectangleF(W - PadX - 80 - 12 - 80, BtnY, 80, BtnH), "取消", 0, primary: false);
        DrawButton(g, new RectangleF(W - PadX - 80, BtnY, 80, BtnH), "确定", 1, primary: true);
    }

    void DrawButton(Graphics g, RectangleF r, string text, int idx, bool primary)
    {
        bool hover = _hoverBtn == idx;
        using var path = RoundedRect(r.X, r.Y, r.Width, r.Height, 8);

        if (primary)
        {
            using var fill = new SolidBrush(hover ? Accent : AccentD);
            g.FillPath(fill, path);
            DrawText(g, text, r.X, r.Y, r.Width, r.Height, Color.White, 9.5f, FontStyle.Bold, StringAlignment.Center);
        }
        else
        {
            using var fill = new SolidBrush(hover ? Color.FromArgb(0x2E, 0x2E, 0x37) : FieldBg);
            g.FillPath(fill, path);
            using var pen = new Pen(Border, 1f);
            g.DrawPath(pen, path);
            DrawText(g, text, r.X, r.Y, r.Width, r.Height, Title, 9.5f, FontStyle.Regular, StringAlignment.Center);
        }
    }

    void DrawText(Graphics g, string text, float x, float y, float w, float h, Color color, float pt, FontStyle style, StringAlignment align = StringAlignment.Near)
    {
        using var font = new Font("Microsoft YaHei UI", pt, style, GraphicsUnit.Point);
        using var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
        using var brush = new SolidBrush(color);
        fmt.Alignment = align;
        fmt.LineAlignment = StringAlignment.Center;
        fmt.Trimming = StringTrimming.EllipsisCharacter;
        g.DrawString(text, font, brush, new RectangleF(x, y, w, h), fmt);
    }

    // ===== 鼠标交互 =====
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        if (SatRect.Contains(e.Location)) { _draggingSv = true; Capture = true; UpdateSv(e.X, e.Y); return; }
        if (HueRect.Contains(e.Location)) { _draggingHue = true; Capture = true; UpdateHue(e.Y); return; }

        if (HitCancel(e.Location)) { DialogResult = DialogResult.Cancel; Close(); }
        else if (HitOk(e.Location)) { DialogResult = DialogResult.OK; Close(); }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_draggingSv) { UpdateSv(e.X, e.Y); return; }
        if (_draggingHue) { UpdateHue(e.Y); return; }

        int btn = HitCancel(e.Location) ? 0 : HitOk(e.Location) ? 1 : -1;
        if (btn != _hoverBtn) { _hoverBtn = btn; Invalidate(); }
        Cursor = (btn >= 0 || SatRect.Contains(e.Location) || HueRect.Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _draggingSv = false;
        _draggingHue = false;
        Capture = false;
    }

    void UpdateSv(int x, int y)
    {
        double s = Math.Clamp((x - SatRect.Left) / SatRect.Width, 0.0, 1.0);
        double v = Math.Clamp(1.0 - (y - SatRect.Top) / SatRect.Height, 0.0, 1.0);
        SetHsv(_h, s, v);
    }

    void UpdateHue(int y)
    {
        double h = Math.Clamp(360.0 * (1.0 - (y - HueRect.Top) / HueRect.Height), 0.0, 360.0);
        SetHsv(h, _s, _v);
    }

    void SetHsv(double h, double s, double v)
    {
        _h = h; _s = s; _v = v;
        _selected = FromHsv(h, s, v);
        SyncFields();
        Invalidate();
    }

    static RectangleF CancelRect => new(W - PadX - 80 - 12 - 80, BtnY, 80, BtnH);
    static RectangleF OkRect => new(W - PadX - 80, BtnY, 80, BtnH);
    static bool HitCancel(Point p) => CancelRect.Contains(p);
    static bool HitOk(Point p) => OkRect.Contains(p);

    // ===== HSV 转换 =====
    static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 360.0) + 360.0) % 360.0;
        double c = v * s;
        double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
        double m = v - c;
        double r, g, b;
        if (h < 60)      { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else              { r = c; g = 0; b = x; }
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }

    static (double h, double s, double v) ToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == r) h = 60.0 * (((g - b) / d) % 6.0);
            else if (max == g) h = 60.0 * ((b - r) / d + 2.0);
            else h = 60.0 * ((r - g) / d + 4.0);
        }
        if (h < 0) h += 360.0;
        double s = max == 0 ? 0 : d / max;
        double v = max;
        return (h, s, v);
    }

    static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        if (r <= 0) r = 1f;
        if (r > w / 2f) r = w / 2f;
        if (r > h / 2f) r = h / 2f;
        var p = new GraphicsPath();
        float d = r * 2f;
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
}
