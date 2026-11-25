using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 单列滚轮选择器：支持鼠标滚轮与上下拖动滚动，中间高亮选中项 =====
sealed class WheelPicker : UserControl
{
    static readonly Color TrackBg    = Color.FromArgb(0x17, 0x17, 0x1B);   // 滚轮底色
    static readonly Color CenterBg   = Color.FromArgb(0x33, 0x8E, 0xF7);   // 中间高亮条（强调蓝）
    static readonly Color CenterEdge = Color.FromArgb(0x6B, 0xB5, 0xFF);   // 高亮条上下描边
    static readonly Color TextHi     = Color.FromArgb(0xFF, 0xFF, 0xFF);   // 选中文字
    static readonly Color TextLo     = Color.FromArgb(0xB4, 0xB4, 0xBC);   // 未选中文字

    const int ItemH = 34;

    int _min, _max, _value;
    float _offset;
    int _dragStartY;
    int _dragBase;
    bool _dragging;

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set { _value = Wrap(value); _offset = 0; Invalidate(); }
    }

    public WheelPicker()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(80, ItemH * 5);
        BackColor = TrackBg;
        Cursor = Cursors.Hand;
    }

    public void Setup(int min, int max, int value)
    {
        _min = min;
        _max = max;
        _value = Wrap(value);
        _offset = 0;
        Invalidate();
    }

    int Wrap(int v)
    {
        int range = _max - _min + 1;
        v = ((v - _min) % range + range) % range + _min;
        return v;
    }

    void Step(int dir)
    {
        _value = Wrap(_value + dir);
        _offset = 0;
        ValueChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta > 0) Step(1);
        else if (e.Delta < 0) Step(-1);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            Focus();
            _dragging = true;
            _dragStartY = e.Y;
            _dragBase = _value;
            _offset = 0;
            Capture = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        float dy = e.Y - _dragStartY;
        int steps = (int)Math.Round(dy / ItemH);
        int v = Wrap(_dragBase + steps);
        _offset = dy - steps * ItemH;
        if (v != _value)
        {
            _value = v;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging) return;
        _dragging = false;
        Capture = false;
        _offset = 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.Clear(TrackBg);

        int w = Width, h = Height;
        int cy = h / 2;

        // 中心高亮条
        using (var bg = new SolidBrush(CenterBg))
            g.FillRectangle(bg, 0, cy - ItemH / 2, w, ItemH);
        using (var pen = new Pen(CenterEdge, 1f))
        {
            g.DrawLine(pen, 0, cy - ItemH / 2, w, cy - ItemH / 2);
            g.DrawLine(pen, 0, cy + ItemH / 2, w, cy + ItemH / 2);
        }

        using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (int i = -3; i <= 3; i++)
        {
            int v = Wrap(_value + i);
            float yCenter = cy + i * ItemH + _offset;
            int alpha = i == 0 ? 255 : (Math.Abs(i) == 1 ? 150 : (Math.Abs(i) == 2 ? 88 : 36));
            float size = i == 0 ? 15f : (Math.Abs(i) == 1 ? 11.5f : 10f);
            var color = Color.FromArgb(alpha, i == 0 ? TextHi : TextLo);
            using var font = new Font("Segoe UI", size, i == 0 ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            using var brush = new SolidBrush(color);
            g.DrawString($"{v:00}", font, brush, new RectangleF(0, yCenter - ItemH / 2, w, ItemH), fmt);
        }
    }
}
