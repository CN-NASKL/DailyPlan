using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DailyPlan.Controls;

namespace DailyPlan.Forms;

// ===== 设置面板：无边框圆角，现代化科技感 =====
sealed class SettingsForm : Form
{
    const int W = 340;          // 面板宽度
    const int PadX = 18;        // 左右内边距
    const int LabelW = 72;      // 行标签宽度
    const int HeaderH = 46;     // 标题区高度
    const int GroupH = 30;      // 分组标题高度
    const int RowH = 46;        // 内容行高度（统一）

    // 各区块 Y 坐标（自上而下，严格对齐）
    const int YAppearance = HeaderH;               // 46
    const int YOpacity    = YAppearance + GroupH;  // 76
    const int YFont       = YOpacity + RowH;       // 122
    const int YBg         = YFont + RowH;          // 168
    const int YSep1       = YBg + RowH;            // 214
    const int YSystem     = YSep1 + 1;             // 215
    const int YStartup    = YSystem + GroupH;      // 245
    const int YSep2       = YStartup + RowH;       // 291
    const int YMore       = YSep2 + 1;             // 292
    const int YSchedule   = YMore + GroupH;        // 322
    const int YExit       = YSchedule + RowH;      // 368
    const int TotalH      = YExit + RowH;          // 414

    static readonly Color Bg        = Color.FromArgb(0x1E, 0x1E, 0x24);
    static readonly Color Hover     = Color.FromArgb(0x2E, 0x2E, 0x37);
    static readonly Color Title     = Color.FromArgb(0xFA, 0xFA, 0xFA);
    static readonly Color RowText   = Color.FromArgb(0xE4, 0xE4, 0xEA);
    static readonly Color GroupText = Color.FromArgb(0x6E, 0x6E, 0x7A);
    static readonly Color SubText   = Color.FromArgb(0x8A, 0x8A, 0x94);
    static readonly Color Sep       = Color.FromArgb(0x34, 0x34, 0x3C);
    static readonly Color Border    = Color.FromArgb(0x3E, 0x3E, 0x48);

    // 展示栏底色预设（深色系，贴合任务栏）
    static readonly Color[] PresetColors =
    {
        Color.FromArgb(0x3E, 0x3E, 0x42), // 灰
        Color.FromArgb(0x2A, 0x2A, 0x2E), // 深灰
        Color.FromArgb(0x1E, 0x1E, 0x24), // 近黑
        Color.FromArgb(0x2C, 0x3A, 0x4A), // 深蓝灰
        Color.FromArgb(0x2E, 0x3A, 0x52), // 深蓝
        Color.FromArgb(0x2A, 0x4A, 0x44), // 深青
        Color.FromArgb(0x3A, 0x2E, 0x44), // 深紫
        Color.FromArgb(0x4A, 0x3A, 0x2E), // 深棕
    };

    readonly Action<int> _onOpacity;
    readonly Action<float> _onFont;
    readonly Action<Color> _onBg;
    readonly Action<bool> _onStartup;
    readonly Action _onSchedule;
    readonly Action _onExit;

    int _hoverRow = -1; // -1 无；0 作息；1 关闭

    Color _bg;
    bool _suppressClose;
    ColorPicker _bgPicker = null!;

    public SettingsForm(int opacity, float fontBase, Color bg, bool startupEnabled,
        Action<int> onOpacity, Action<float> onFont, Action<Color> onBg, Action<bool> onStartup,
        Action onSchedule, Action onExit)
    {
        _onOpacity = onOpacity;
        _onFont = onFont;
        _onBg = onBg;
        _onStartup = onStartup;
        _onSchedule = onSchedule;
        _onExit = onExit;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        TopMost = true;
        BackColor = Bg;
        Size = new Size(W, TotalH);

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        // 透明度滑块
        var opacitySlider = new Slider
        {
            Min = 10,
            Max = 100,
            Value = Math.Clamp(opacity * 100 / 255, 10, 100),
            Location = new Point(PadX + LabelW, YOpacity),
            Size = new Size(W - PadX - (PadX + LabelW), RowH),
            BackColor = Bg,
        };
        opacitySlider.ValueChanged += (_, _) =>
            _onOpacity(Math.Clamp((int)Math.Round(opacitySlider.Value * 255.0 / 100.0), 10, 255));
        Controls.Add(opacitySlider);

        // 字体大小滑块
        var fontSlider = new Slider
        {
            Min = 8,
            Max = 24,
            Value = (int)Math.Round(fontBase),
            Location = new Point(PadX + LabelW, YFont),
            Size = new Size(W - PadX - (PadX + LabelW), RowH),
            BackColor = Bg,
        };
        fontSlider.ValueChanged += (_, _) => _onFont(Math.Clamp(fontSlider.Value, 8f, 24f));
        Controls.Add(fontSlider);

        // 底色调色盘
        _bg = bg;
        _bgPicker = new ColorPicker
        {
            Location = new Point(PadX + LabelW, YBg),
            Size = new Size(W - PadX - (PadX + LabelW), RowH),
            BackColor = Bg,
        };
        _bgPicker.Init(PresetColors, bg);
        _bgPicker.ColorSelected += c => { _bg = c; _onBg(c); };
        _bgPicker.CustomRequested += OnCustomColorRequested;
        Controls.Add(_bgPicker);

        // 开机启动开关
        var startupToggle = new ToggleSwitch
        {
            Location = new Point(W - PadX - 44, YStartup + (RowH - 22) / 2),
            Size = new Size(44, 22),
            BackColor = Bg,
        };
        startupToggle.Checked = startupEnabled;
        startupToggle.CheckedChanged += (_, _) => _onStartup(startupToggle.Checked);
        Controls.Add(startupToggle);
    }

    // 定位到锚点上方，并 clamp 到工作区
    public void PositionAt(Point screenAnchor)
    {
        var wa = Screen.FromPoint(screenAnchor).WorkingArea;
        int x = Math.Clamp(screenAnchor.X - W / 2, wa.Left + 8, wa.Right - W - 8);
        int y = screenAnchor.Y - TotalH - 10;
        if (y < wa.Top + 8) y = screenAnchor.Y + 10;
        if (y + TotalH > wa.Bottom) y = wa.Bottom - TotalH - 8;
        Location = new Point(x, y);
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
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, W + 1, TotalH + 1, 24, 24));
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (_suppressClose) return;
        Close();
    }

    void OnCustomColorRequested()
    {
        _suppressClose = true;
        try
        {
            using var dlg = new ColorPickerDialog(_bg);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _bg = dlg.SelectedColor;
                _bgPicker.SetCustomColor(_bg);
                _onBg(_bg);
            }
        }
        finally
        {
            _suppressClose = false;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // 背景
        using (var bg = new SolidBrush(Bg))
            g.FillRectangle(bg, 0, 0, W, TotalH);

        // 描边
        using (var pen = new Pen(Border, 1f))
        using (var path = RoundedRect(0.5f, 0.5f, W - 1f, TotalH - 1f, 12f))
            g.DrawPath(pen, path);

        // 标题
        DrawText(g, "设置", PadX, 0, W - PadX * 2, HeaderH, Title, 12f, FontStyle.Bold);

        // 分组 + 标签 + 分隔线
        DrawGroupLabel(g, "外观", YAppearance);
        DrawRowLabel(g, "透明度", YOpacity);
        DrawRowLabel(g, "字体大小", YFont);
        DrawRowLabel(g, "底色", YBg);
        DrawSep(g, YSep1);
        DrawGroupLabel(g, "系统", YSystem);
        DrawRowLabel(g, "开机启动", YStartup);
        DrawSep(g, YSep2);
        DrawGroupLabel(g, "更多", YMore);

        // 按钮行
        DrawActionRow(g, "我的作息安排", YSchedule, 0);
        DrawActionRow(g, "关闭", YExit, 1);
    }

    void DrawGroupLabel(Graphics g, string text, int y)
    {
        DrawText(g, text, PadX, y, W - PadX * 2, GroupH, GroupText, 8.5f, FontStyle.Regular);
    }

    void DrawRowLabel(Graphics g, string text, int y)
    {
        DrawText(g, text, PadX, y, LabelW, RowH, RowText, 9.5f, FontStyle.Regular);
    }

    void DrawActionRow(Graphics g, string text, int y, int idx)
    {
        if (_hoverRow == idx)
        {
            using var hb = new SolidBrush(Hover);
            using var hp = RoundedRect(6, y + 2, W - 12, RowH - 4, 8);
            g.FillPath(hb, hp);
        }
        DrawText(g, text, PadX + 6, y, W - PadX * 2 - 30, RowH, Title, 9.5f, FontStyle.Regular);
        DrawText(g, "›", W - PadX - 20, y, 20, RowH, SubText, 14f, FontStyle.Regular, StringAlignment.Far);
    }

    void DrawSep(Graphics g, int y)
    {
        using var p = new Pen(Sep, 1f);
        g.DrawLine(p, PadX, y, W - PadX, y);
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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int r = -1;
        if (e.Y >= YSchedule && e.Y < YSchedule + RowH) r = 0;
        else if (e.Y >= YExit && e.Y < YExit + RowH) r = 1;
        if (r != _hoverRow) { _hoverRow = r; Invalidate(); }
        Cursor = r >= 0 ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverRow != -1) { _hoverRow = -1; Invalidate(); }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        if (e.Y >= YSchedule && e.Y < YSchedule + RowH) { var a = _onSchedule; Hide(); a(); }
        else if (e.Y >= YExit && e.Y < YExit + RowH) { var a = _onExit; Hide(); a(); }
    }

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

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
}
