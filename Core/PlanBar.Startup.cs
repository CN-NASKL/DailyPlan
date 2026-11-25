using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using DailyPlan.Controls;

namespace DailyPlan.Core;

// ===== 开机自启动：注册表 Run 键 + 菜单开关行 =====
partial class PlanBar
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "DailyPlan";

    static string ExePath =>
        Environment.ProcessPath ?? Application.ExecutablePath;

    // 当前是否已注册开机自启动
    static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(RunValueName) is string s && s.Length > 0;
        }
        catch { return false; }
    }

    // 写入 / 删除开机自启动项（HKCU，无需管理员权限）
    static void SetStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;
            if (enabled)
                key.SetValue(RunValueName, $"\"{ExePath}\"");
            else
                key.DeleteValue(RunValueName, false);
        }
        catch { /* 注册表写入失败忽略 */ }
    }

    // 构建「开机启动」开关行（左标签 + 右开关）
    ToolStripItem BuildStartupRow()
    {
        var panel = new Panel
        {
            Width = 186,
            Height = 26,
            BackColor = Color.FromArgb(0x2A, 0x2A, 0x2E),
        };

        var label = new Label
        {
            Text = "开机启动",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0xFA, 0xFA, 0xFA),
            Font = new Font("Microsoft YaHei UI", 9f),
            Location = new Point(0, 0),
            Size = new Size(130, 26),
        };

        var toggle = new ToggleSwitch
        {
            Location = new Point(142, 2),
            Size = new Size(44, 22),
        };
        toggle.Checked = IsStartupEnabled(); // 先设初值，再订阅，避免初始触发写入
        toggle.CheckedChanged += (_, _) => SetStartup(toggle.Checked);

        panel.Controls.Add(label);
        panel.Controls.Add(toggle);

        return new ToolStripControlHost(panel)
        {
            AutoSize = false,
            Width = 200,
            Height = 32,
            Margin = new Padding(14, 2, 6, 8),
        };
    }
}
