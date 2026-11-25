using System;
using System.Runtime.InteropServices;

namespace DailyPlan.Core;

// ===== Win32 互操作：P/Invoke 声明 + 常量 + 结构体 =====
partial class PlanBar
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern int GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct SIZE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential)]
    struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const int WS_POPUP = unchecked((int)0x80000000);
    const int WS_CHILD = 0x40000000;
    const int WS_EX_TOPMOST = 0x00000008;
    const int WS_EX_NOACTIVATE = 0x08000000;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_LAYERED = 0x00080000;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint ULW_ALPHA = 0x00000002;
    const byte AC_SRC_ALPHA = 0x01;
}
