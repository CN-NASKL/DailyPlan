using System;
using System.Drawing;
using System.Windows.Forms;
using DailyPlan.Forms;

namespace DailyPlan.Core;

// ===== 右键弹出设置面板 =====
partial class PlanBar
{
    void ShowSettings(Point screenAnchor)
    {
        var form = new SettingsForm(
            _opacity,
            _fontBase,
            _bgSolid,
            IsStartupEnabled(),
            onOpacity: v => { _opacity = v; Render(); SaveSettings(); },
            onFont: v => { _fontBase = v; Render(); SaveSettings(); },
            onBg: c => { _bgSolid = c; Render(); SaveSettings(); },
            onStartup: SetStartup,
            onSchedule: OpenScheduleEditor,
            onExit: Close);

        form.PositionAt(screenAnchor);
        form.Show();
    }
}
