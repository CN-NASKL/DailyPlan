using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 深色右键菜单渲染器（类似展示栏的纯灰高级感样式） =====
sealed class DarkMenuRenderer : ToolStripRenderer
{
    static readonly Color Bg      = Color.FromArgb(0x2A, 0x2A, 0x2E);   // 菜单背景
    static readonly Color Hover   = Color.FromArgb(0x3F, 0x3F, 0x46);   // 悬停背景
    static readonly Color Text    = Color.FromArgb(0xFA, 0xFA, 0xFA);   // 白字
    static readonly Color TextDim = Color.FromArgb(0xA1, 0xA1, 0xAA);   // 次要文字（标签）
    static readonly Color Sep     = Color.FromArgb(45, 255, 255, 255);  // 分隔线
    static readonly Color Border  = Color.FromArgb(56, 255, 255, 255);  // 微光描边

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(Bg);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(Bg);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected || e.Item.Pressed)
        {
            var r = new Rectangle(2, 2, e.Item.Width - 4, e.Item.Height - 4);
            using var b = new SolidBrush(Hover);
            e.Graphics.FillRectangle(b, r);
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var p = new Pen(Sep, 1f);
        e.Graphics.DrawLine(p, 10, y, e.Item.Width - 10, y);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var color = e.Item is ToolStripLabel ? TextDim : Text;
        TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var p = new Pen(Border, 1f);
        var r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(p, r);
    }

    // 菜单显示后设置圆角区域
    public static void ApplyRoundedCorners(ToolStrip menu)
    {
        int radius = 12;
        var hRgn = CreateRoundRectRgn(0, 0, menu.Width + 1, menu.Height + 1, radius * 2, radius * 2);
        SetWindowRgn(menu.Handle, hRgn, true);
    }
}
