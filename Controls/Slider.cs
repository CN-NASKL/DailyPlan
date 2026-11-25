using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 自绘滑动条（HeroUI 现代化风格，通用范围） =====
sealed class Slider : Control
{
    int _min = 10;
    int _max = 100;
    int _value = 100;
    bool _dragging;
    bool _hovering;

    static readonly Color TrackBg   = Color.FromArgb(0x3A, 0x3A, 0x40);  // 轨道背景（深灰）
    static readonly Color FillStart = Color.FromArgb(0x4A, 0xA5, 0xFF);  // 填充渐变起（亮蓝）
    static readonly Color FillEnd   = Color.FromArgb(0x33, 0x8E, 0xF7);  // 填充渐变止（强调蓝）
    static readonly Color Knob      = Color.FromArgb(0xFA, 0xFA, 0xFA);  // 滑块白
    static readonly Color KnobRing  = Color.FromArgb(0x33, 0x8E, 0xF7);  // 滑块描边（蓝）

    public event EventHandler? ValueChanged;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Min { get => _min; set { _min = value; _value = Math.Clamp(_value, _min, _max); Invalidate(); } }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Max { get => _max; set { _max = value; _value = Math.Clamp(_value, _min, _max); Invalidate(); } }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, _min, _max); Invalidate(); }
    }

    public Slider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        BackColor = Color.FromArgb(0x2A, 0x2A, 0x2E); // 与菜单背景一致，避免白底
        Height = 26;
        Cursor = Cursors.Hand;
    }

    float Fraction => _max > _min ? (_value - _min) / (float)(_max - _min) : 0f;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int trackH = 6;
        int trackY = (Height - trackH) / 2;
        float halfH = Height / 2f;

        // 轨道：圆角胶囊
        using (var trackPath = RoundedRect(0, trackY, Width, trackH, trackH / 2f))
        using (var track = new SolidBrush(TrackBg))
            g.FillPath(track, trackPath);

        // 滑块圆点（悬停/拖动时放大）
        float knobR = _hovering || _dragging ? 8.5f : 7.5f;
        float knobX = knobR + (Width - knobR * 2) * Fraction;

        // 已填充：蓝色渐变胶囊，终点对齐滑块中心
        float fillW = knobX;
        if (fillW > trackH)
        {
            using var fillPath = RoundedRect(0, trackY, fillW, trackH, trackH / 2f);
            using var fill = new LinearGradientBrush(
                new Rectangle(0, trackY, Math.Max(trackH, (int)fillW), trackH),
                FillStart, FillEnd, LinearGradientMode.Horizontal);
            g.FillPath(fill, fillPath);
        }

        // 外圈光晕（悬停/拖动时更亮）
        float glowR = knobR + 3f;
        int glowAlpha = _hovering || _dragging ? 64 : 28;
        using (var glow = new SolidBrush(Color.FromArgb(glowAlpha, 0x33, 0x8E, 0xF7)))
            g.FillEllipse(glow, knobX - glowR, halfH - glowR, glowR * 2, glowR * 2);

        // 白色圆点 + 淡蓝描边
        using (var knob = new SolidBrush(Knob))
            g.FillEllipse(knob, knobX - knobR, halfH - knobR, knobR * 2, knobR * 2);
        using (var ring = new Pen(KnobRing, 1.5f))
            g.DrawEllipse(ring, knobX - knobR, halfH - knobR, knobR * 2, knobR * 2);
    }

    static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        if (r <= 0) r = 1f;
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _dragging = true; UpdateFromMouse(e.X); }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging) UpdateFromMouse(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging) { _dragging = false; UpdateFromMouse(e.X); }
        base.OnMouseUp(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovering = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    void UpdateFromMouse(int x)
    {
        float t = Math.Clamp(x / (float)Math.Max(1, Width), 0f, 1f);
        int v = (int)Math.Round(_min + t * (_max - _min));
        if (v != _value)
        {
            _value = v;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
