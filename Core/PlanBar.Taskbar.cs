using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace DailyPlan.Core;

// ===== 任务栏嵌入 + UIA 按钮避让 =====
partial class PlanBar
{
    // 确保窗口嵌入任务栏；仅在窗口脱离任务栏时才重新嵌入并定位，
    // 平时不做任何 MoveWindow/SetParent 操作，避免与任务栏 Shell 布局引擎打架导致位置闪烁。
    void EnsureEmbedded()
    {
        var hwnd = Handle;
        if (hwnd == IntPtr.Zero) return;

        var taskbar = _lastTray;
        if (taskbar == IntPtr.Zero || GetParent(hwnd) != taskbar)
            taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return;

        // 已嵌入：直接返回，不重复定位
        if (GetParent(hwnd) == taskbar)
        {
            _lastTray = taskbar;
            return;
        }

        // 重新嵌入到任务栏
        long style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr((style & ~(long)WS_POPUP) | WS_CHILD));
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr((ex & ~(long)WS_EX_TOPMOST) | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED));
        SetParent(hwnd, taskbar);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        _lastTray = taskbar;
        ApplyBarSize();
        Render();
    }

    // 依据当前宽度/位置/任务栏高度重排并重绘（拖拽、任务栏变化时调用）
    void ApplyBarSize()
    {
        var hwnd = Handle;
        if (hwnd == IntPtr.Zero) return;

        var taskbar = _lastTray != IntPtr.Zero ? _lastTray : FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return;

        if (!GetClientRect(taskbar, out var cr)) return;
        int h = cr.Bottom - cr.Top;
        if (h <= 0) return;

        int dpi = GetDpiForWindow(hwnd);
        if (dpi <= 0) dpi = 96;
        int pw = (int)(_barW * dpi / 96.0);
        int px = (int)(_barX * dpi / 96.0);

        if (Size.Width != pw || Size.Height != h)
            Size = new Size(pw, h);
        MoveWindow(hwnd, px, 0, pw, h, true);

        if (h != _lastH || pw != _lastW)
            Render();
    }

    // 用 UIA 获取任务栏上所有按钮（开始/搜索/任务视图/小组件/聊天/任务栏图标/托盘）的占位区间，
    // 合并为互不重叠的禁止区间，存到 _occupied（逻辑单位）。
    // 只在「启动时」和「拖动开始前」刷新，sticky 定时器不刷新、不重排位置 —— 这样能彻底消除闪烁。
    void RefreshOccupied()
    {
        var newRanges = new List<(int L, int R)>();
        try
        {
            var tray = _lastTray != IntPtr.Zero ? _lastTray : FindWindow("Shell_TrayWnd", null);
            if (tray == IntPtr.Zero) return;

            var root = AutomationElement.FromHandle(tray);
            if (root == null) return;

            int dpi = GetDpiForWindow(Handle);
            if (dpi <= 0) dpi = 96;
            GetWindowRect(tray, out var shellRect);
            int offX = shellRect.Left;

            // 遍历任务栏所有按钮（开始/搜索/任务视图/小组件/聊天/图标/托盘）
            var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
            var buttons = root.FindAll(TreeScope.Descendants, cond);
            foreach (AutomationElement b in buttons)
            {
                try
                {
                    var r = b.Current.BoundingRectangle;
                    if (r.Width <= 0 || r.Height <= 0) continue;
                    int L = (int)Math.Round((r.Left - offX) * 96 / dpi);
                    int R = (int)Math.Round((r.Right - offX) * 96 / dpi);
                    if (R <= L) continue;
                    newRanges.Add((L, R));
                }
                catch { /* 单个元素失败忽略 */ }
            }

            // 排序并合并重叠/相邻区间
            newRanges.Sort((a, b) => a.L.CompareTo(b.L));
            var merged = new List<(int L, int R)>();
            foreach (var (L, R) in newRanges)
            {
                if (merged.Count > 0 && L <= merged[merged.Count - 1].R + 2)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.L, Math.Max(last.R, R));
                }
                else
                {
                    merged.Add((L, R));
                }
            }

            // 只在成功拿到非空结果时替换缓存
            if (merged.Count > 0)
                _occupied = merged;
        }
        catch { /* UIA 失败则保留上次缓存 */ }
    }

    // 计算栏体合法 X（逻辑单位）：栏体 [x, x+w] 不得与任何按钮占位区间重叠
    int ClampBarX(int x)
    {
        int w = _barW;

        int dpi = GetDpiForWindow(Handle);
        if (dpi <= 0) dpi = 96;

        int taskbarW = 0;
        var tray = _lastTray != IntPtr.Zero ? _lastTray : FindWindow("Shell_TrayWnd", null);
        if (tray != IntPtr.Zero && GetWindowRect(tray, out var tr))
            taskbarW = (tr.Right - tr.Left) * 96 / dpi;
        if (taskbarW <= 0) return 0;

        int maxX = Math.Max(0, taskbarW - w);
        x = Math.Clamp(x, 0, maxX);

        // 计算所有合法间隙：x ∈ [0,maxX] 且 [x,x+w] 不与任何禁止区间 [L,R] 重叠
        // 即 x <= L-w 或 x >= R
        var allowed = new List<(int L, int R)>();
        int cur = 0;
        foreach (var (L, R) in _occupied)
        {
            if (L - w > cur)
                allowed.Add((cur, L - w));
            cur = Math.Max(cur, R);
        }
        if (cur < maxX)
            allowed.Add((cur, maxX));

        if (allowed.Count == 0)
            return x; // 无间隙可放（栏体过宽），退回任务栏范围 clamp 值

        // 选离理想 x 最近的合法位置
        int best = x, bestDist = int.MaxValue;
        foreach (var (aL, aR) in allowed)
        {
            int cx = Math.Clamp(x, aL, aR);
            int dist = Math.Abs(cx - x);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = cx;
            }
        }
        return best;
    }
}
