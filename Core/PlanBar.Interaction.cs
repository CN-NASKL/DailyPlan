using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DailyPlan.Core;

// ===== 鼠标交互：拖动移动/调宽 + 音乐按钮 + 设置持久化 =====
partial class PlanBar
{
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Right)
        {
            ShowSettings(PointToScreen(e.Location));
            return;
        }

        // 音乐控制按钮命中 → 触发命令 + 按下缩放反馈，不进入拖动
        if (e.Button == MouseButtons.Left && _musicVisible)
        {
            if (_btnPrev.Contains(e.X, e.Y)) { PressMusic(1); _ = _music.PreviousAsync(); return; }
            if (_btnPlay.Contains(e.X, e.Y)) { PressMusic(2); _ = _music.TogglePlayPauseAsync(); return; }
            if (_btnNext.Contains(e.X, e.Y)) { PressMusic(3); _ = _music.NextAsync(); return; }
        }

        if (e.Button != MouseButtons.Left) return;

        int dpi = GetDpiForWindow(Handle);
        if (dpi <= 0) dpi = 96;

        // 拖动前刷新一次按钮占位（保证拿到最新的任务栏图标/小组件位置）
        RefreshOccupied();

        // 右侧手柄区域 → 拖拽调整宽度
        if (e.X >= Width - ResizeZone)
        {
            _resizing = true;
            _dragStartX = e.X;
            _dragStartWpx = Width;
            _dragDpi = dpi;
            Capture = true;
        }
        else
        {
            // 其余区域 → 整体水平移动（用屏幕坐标避免窗口移动后相对坐标漂移）
            _moving = true;
            _moveStartX = Cursor.Position.X;
            _moveStartBarX = _barX;
            _moveDpi = dpi;
            Capture = true;
        }
    }

    // 记录按下的音乐按钮并重绘（缩放反馈）
    void PressMusic(int btn)
    {
        _pressedMusicBtn = btn;
        Capture = true;
        Render();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_resizing)
        {
            int newW = _dragStartWpx + (e.X - _dragStartX);
            int minPx = MinWidthDip * _dragDpi / 96;
            int maxPx = MaxWidthDip * _dragDpi / 96;
            newW = Math.Clamp(newW, minPx, maxPx);
            _barW = (int)Math.Round(newW * 96.0 / _dragDpi);
            _barX = ClampBarX(_barX);
            ApplyBarSize();
        }
        else if (_moving)
        {
            int dxPx = Cursor.Position.X - _moveStartX;
            int dxDip = (int)Math.Round(dxPx * 96.0 / _moveDpi);
            _barX = ClampBarX(_moveStartBarX + dxDip);
            ApplyBarSize();
        }
        else
        {
            UpdateCursor(e.X, e.Y);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        // 松开音乐按钮 → 恢复大小
        if (_pressedMusicBtn != 0)
        {
            _pressedMusicBtn = 0;
            Capture = false;
            Render();
            return;
        }

        if (_resizing || _moving)
        {
            _resizing = false;
            _moving = false;
            Capture = false;
            Cursor = Cursors.Default;
            SaveSettings();
        }
    }

    // 根据鼠标位置更新光标：按钮=手指，文字=默认，空白=拖动
    void UpdateCursor(int x, int y)
    {
        if (_musicVisible)
        {
            if (_btnPrev.Contains(x, y) || _btnPlay.Contains(x, y) || _btnNext.Contains(x, y))
            {
                Cursor = Cursors.Hand;
                return;
            }
            if (_songTextRect.Contains(x, y))
            {
                Cursor = Cursors.Default;
                return;
            }
        }

        if (_taskTextRect.Contains(x, y))
        {
            Cursor = Cursors.Default;
            return;
        }

        Cursor = x >= Width - ResizeZone ? Cursors.SizeWE : Cursors.SizeAll;
    }

    // ===== 设置持久化 =====
    void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            foreach (var raw in File.ReadAllLines(SettingsPath))
            {
                var kv = raw.Split('=');
                if (kv.Length != 2) continue;
                switch (kv[0].Trim())
                {
                    case "opacity": if (int.TryParse(kv[1], out var o)) _opacity = o; break;
                    case "fontBase": if (float.TryParse(kv[1], out var f)) _fontBase = f; break;
                    case "width": if (int.TryParse(kv[1], out var w)) _barW = w; break;
                    case "x": if (int.TryParse(kv[1], out var xx)) _barX = xx; break;
                    case "bg": if (int.TryParse(kv[1], System.Globalization.NumberStyles.HexNumber, null, out var rgb)) _bgSolid = Color.FromArgb(255, Color.FromArgb(rgb)); break;
                }
            }
        }
        catch { /* 忽略读取失败 */ }

        _opacity = Math.Clamp(_opacity, 10, 255);
        _fontBase = Math.Clamp(_fontBase, 8f, 24f);
        _barW = Math.Clamp(_barW, MinWidthDip, MaxWidthDip);
        if (_barX < 0) _barX = 0;
    }

    void SaveSettings()
    {
        try
        {
            File.WriteAllText(SettingsPath,
                $"opacity={_opacity}\nfontBase={_fontBase}\nwidth={_barW}\nx={_barX}\nbg={_bgSolid.R:X2}{_bgSolid.G:X2}{_bgSolid.B:X2}\n");
        }
        catch { /* 忽略写入失败 */ }
    }
}
