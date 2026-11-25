using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 调色盘：一排预设色块 + 自定义取色按钮 =====
sealed class ColorPicker : Control
{
    Color[] _colors = Array.Empty<Color>();
    Color _selected;
    Color? _custom; // 自定义颜色（null=未自定义）

    public event Action<Color>? ColorSelected;
    public event Action? CustomRequested;

    const float PadX = 10f;   // 左右边距（给选中外圈留空间，修复外圈被裁）
    const float D = 18f;      // 色块直径
    const float RingOff = 3f; // 选中外圈外扩

    public ColorPicker()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Cursor = Cursors.Hand;
        Height = 32;
    }

    public void Init(Color[] colors, Color selected)
    {
        _colors = colors ?? Array.Empty<Color>();
        _selected = selected;
        _custom = Array.Exists(_colors, c => c.ToArgb() == selected.ToArgb()) ? null : selected;
        Invalidate();
    }

    public void SetCustomColor(Color c)
    {
        _custom = c;
        _selected = c;
        Invalidate();
    }

    int UnitCount => _colors.Length + 1; // 预设色块 + 1 个自定义按钮
    float Step => UnitCount > 1 ? (Width - PadX * 2) / UnitCount : 0;
    float CenterX(int i) => PadX + (i + 0.5f) * Step;
    float CenterY => Height / 2f;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        for (int i = 0; i < _colors.Length; i++)
        {
            var c = _colors[i];
            DrawSwatch(g, CenterX(i), c, _selected.ToArgb() == c.ToArgb());
        }
        DrawCustom(g, CenterX(_colors.Length));
    }

    void DrawSwatch(Graphics g, float cx, Color c, bool selected)
    {
        float cy = CenterY;
        using (var b = new SolidBrush(c))
            g.FillEllipse(b, cx - D / 2f, cy - D / 2f, D, D);
        using (var ring = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
            g.DrawEllipse(ring, cx - D / 2f, cy - D / 2f, D, D);
        if (selected)
            DrawSelectionRing(g, cx, cy);
    }

    void DrawCustom(Graphics g, float cx)
    {
        float cy = CenterY;
        bool selected = _custom.HasValue && _custom.Value.ToArgb() == _selected.ToArgb();

        if (_custom.HasValue)
        {
            using var b = new SolidBrush(_custom.Value);
            g.FillEllipse(b, cx - D / 2f, cy - D / 2f, D, D);
        }
        else
        {
            using var rb = new LinearGradientBrush(
                new RectangleF(cx - D / 2f, cy - D / 2f, D, D),
                Color.FromArgb(0xFF, 0x60, 0x60), Color.FromArgb(0xA8, 0x78, 0xFF),
                LinearGradientMode.Horizontal);
            g.FillEllipse(rb, cx - D / 2f, cy - D / 2f, D, D);
        }

        using (var ring = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
            g.DrawEllipse(ring, cx - D / 2f, cy - D / 2f, D, D);

        // 中心十字，表示"自定义取色"
        using (var p = new Pen(Color.White, 1.4f))
        {
            float s = D * 0.24f;
            g.DrawLine(p, cx - s, cy, cx + s, cy);
            g.DrawLine(p, cx, cy - s, cx, cy + s);
        }

        if (selected)
            DrawSelectionRing(g, cx, cy);
    }

    void DrawSelectionRing(Graphics g, float cx, float cy)
    {
        using var sel = new Pen(Color.FromArgb(0x3B, 0x8E, 0xFF), 2f);
        g.DrawEllipse(sel, cx - D / 2f - RingOff, cy - D / 2f - RingOff, D + RingOff * 2, D + RingOff * 2);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        float cy = CenterY;

        // 自定义按钮
        if (Hit(CenterX(_colors.Length), cy, e.X, e.Y))
        {
            CustomRequested?.Invoke();
            return;
        }

        // 预设色块
        for (int i = 0; i < _colors.Length; i++)
        {
            float cx = CenterX(i);
            if (Hit(cx, cy, e.X, e.Y))
            {
                _selected = _colors[i];
                Invalidate();
                ColorSelected?.Invoke(_selected);
                return;
            }
        }
    }

    static bool Hit(float cx, float cy, float x, float y)
    {
        float dx = x - cx, dy = y - cy;
        float r = D / 2f + RingOff;
        return dx * dx + dy * dy <= r * r;
    }
}
