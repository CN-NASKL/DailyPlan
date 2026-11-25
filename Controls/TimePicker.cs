using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 时间栏：自绘显示当前时间，点击弹出时间选择弹窗（滚轮 + 输入框） =====
sealed class TimePicker : UserControl
{
    static readonly Color Bg        = Color.FromArgb(0x33, 0x33, 0x38);   // 常态背景
    static readonly Color BgHover   = Color.FromArgb(0x3F, 0x3F, 0x46);   // 悬停背景
    static readonly Color Border    = Color.FromArgb(0x4A, 0x4A, 0x52);   // 描边
    static readonly Color TextWhite = Color.FromArgb(0xFA, 0xFA, 0xFA);   // 白字
    static readonly Color ArrowDim  = Color.FromArgb(0xA1, 0xA1, 0xAA);   // 箭头（常态）
    static readonly Color ArrowHi   = Color.FromArgb(0x33, 0x8E, 0xF7);   // 箭头（悬停，强调蓝）

    int _hour, _minute;
    bool _hover;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TimeText
    {
        get => $"{_hour:00}:{_minute:00}";
        set
        {
            if (TryParse(value, out int h, out int m)) { _hour = h; _minute = m; }
            Invalidate();
        }
    }

    public TimePicker()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(84, 26);
        Cursor = Cursors.Hand;
        Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        _hour = 6; _minute = 30;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
    protected override void OnClick(EventArgs e) { base.OnClick(e); OpenPopup(); }

    void OpenPopup()
    {
        var anchor = PointToScreen(new Point(0, Height));
        using var popup = new TimePickerPopup(_hour, _minute, anchor);
        if (popup.ShowDialog() == DialogResult.OK)
            TimeText = popup.TimeResult;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        int w = Width, h = Height;

        // 圆角背景 + 描边
        using (var path = RoundedRect(0.5f, 0.5f, w - 1f, h - 1f, 6))
        {
            using var bg = new SolidBrush(_hover ? BgHover : Bg);
            g.FillPath(bg, path);
            using var pen = new Pen(Border);
            g.DrawPath(pen, path);
        }

        // 时间文字（避开右侧箭头区，居中）
        var textRect = new Rectangle(0, 0, w - 18, h);
        TextRenderer.DrawText(g, $"{_hour:00}:{_minute:00}", Font, textRect, TextWhite,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        // 右侧下拉小箭头（自绘倒三角）
        int cx = w - 9;
        int cy = h / 2;
        using (var tri = new GraphicsPath())
        {
            tri.AddLine(cx - 3, cy - 3, cx + 3, cy - 3);
            tri.AddLine(cx + 3, cy - 3, cx, cy + 3);
            tri.CloseFigure();
            using var brush = new SolidBrush(_hover ? ArrowHi : ArrowDim);
            g.FillPath(brush, tri);
        }
    }

    static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        float d = r * 2;
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // 宽松解析：支持 "06:30"、"630"、"6:30" 等（用于接收外部传入值）
    static bool TryParse(string s, out int h, out int m)
    {
        h = 0; m = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var digits = "";
        foreach (var c in s.Trim())
            if (c >= '0' && c <= '9') digits += c;
        if (digits.Length < 3 || digits.Length > 4) return false;

        if (digits.Length == 3)
        {
            h = digits[0] - '0';
            m = (digits[1] - '0') * 10 + (digits[2] - '0');
        }
        else
        {
            h = (digits[0] - '0') * 10 + (digits[1] - '0');
            m = (digits[2] - '0') * 10 + (digits[3] - '0');
        }
        return h >= 0 && h <= 23 && m >= 0 && m <= 59;
    }
}
