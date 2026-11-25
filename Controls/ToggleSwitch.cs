using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 自绘开关按钮（现代化风格，用于开机自启动） =====
sealed class ToggleSwitch : Control
{
    bool _checked;

    static readonly Color TrackOff  = Color.FromArgb(0x3A, 0x3A, 0x40);  // 关闭态轨道（深灰）
    static readonly Color FillStart = Color.FromArgb(0x4A, 0xA5, 0xFF);  // 开启态渐变起（亮蓝）
    static readonly Color FillEnd   = Color.FromArgb(0x33, 0x8E, 0xF7);  // 开启态渐变止（强调蓝）
    static readonly Color Knob      = Color.FromArgb(0xFA, 0xFA, 0xFA);  // 滑块白

    public event EventHandler? CheckedChanged;

    [System.ComponentModel.DefaultValue(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        BackColor = Color.FromArgb(0x2A, 0x2A, 0x2E); // 与菜单背景一致
        Size = new Size(44, 22);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float h = Height;
        float knobR = h / 2f - 2f;

        // 轨道
        using (var trackPath = RoundedRect(0, 0, Width, h, h / 2f))
        {
            if (_checked)
            {
                using var fill = new LinearGradientBrush(
                    new Rectangle(0, 0, Width, Height), FillStart, FillEnd, LinearGradientMode.Horizontal);
                g.FillPath(fill, trackPath);
            }
            else
            {
                using var b = new SolidBrush(TrackOff);
                g.FillPath(b, trackPath);
            }
        }

        // 开启态光晕
        if (_checked)
        {
            float knobX = Width - knobR - 2f;
            float cy = h / 2f;
            using var glow = new SolidBrush(Color.FromArgb(48, 0x33, 0x8E, 0xF7));
            g.FillEllipse(glow, knobX - knobR - 2f, cy - knobR - 2f, (knobR + 2f) * 2, (knobR + 2f) * 2);
        }

        // 滑块
        float cx = _checked ? Width - knobR - 2f : knobR + 2f;
        float cyy = h / 2f;
        using (var knob = new SolidBrush(Knob))
            g.FillEllipse(knob, cx - knobR, cyy - knobR, knobR * 2, knobR * 2);
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

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }
}
