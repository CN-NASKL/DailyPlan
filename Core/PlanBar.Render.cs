using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using DailyPlan.Services;

namespace DailyPlan.Core;

// ===== 渲染：圆角路径 + 逐像素 alpha 分层窗口 + 文字绘制 + 刷新 =====
partial class PlanBar
{
    // ===== 圆角路径 =====
    static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        if (r <= 0) r = 1f;
        if (r > w / 2f) r = w / 2f;
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

    // ===== 用逐像素 alpha 分层窗口渲染（突破任务栏 DirectComposition/Mica 层） =====
    void Render()
    {
        var hwnd = Handle;
        if (hwnd == IntPtr.Zero) return;

        int w = Width, h = Height;
        if (w <= 0 || h <= 0) return;
        _lastW = w; _lastH = h;

        int dpi = GetDpiForWindow(hwnd);
        if (dpi <= 0) dpi = 96;

        using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 圆角半径随高度自适应
                float r = Math.Min(11f, h * 0.22f);

                using (var path = RoundedRect(0.5f, 0.5f, w - 1f, h - 1f, r))
                {
                    // 背景：纯灰纯色
                    using (var bg = new SolidBrush(_bgSolid))
                        g.FillPath(bg, path);

                    // 1px 微光描边
                    using (var pen = new Pen(BorderColor, 1f))
                        g.DrawPath(pen, path);
                }

                // 左侧强调胶囊
                float barW = 3.5f;
                float barH = Math.Max(14f, h - 20f);
                float barX = 9f;
                float barY = (h - barH) / 2f;
                Color barColor = _accent;
                using (var accentPath = RoundedRect(barX, barY, barW, barH, barW / 2f))
                {
                    using (var accentBrush = new SolidBrush(barColor))
                        g.FillPath(accentBrush, accentPath);

                    // 白色描边框，贴合高亮条
                    using (var outline = new Pen(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF), 1f))
                        g.DrawPath(outline, accentPath);
                }

                // 主内容：有音乐时显示音乐区，否则显示计划任务
                {
                    float baseWpx = BarWidthDip * dpi / 96f;
                    float fontPt = _fontBase * (w / baseWpx);
                    float maxPtByHeight = h * 0.58f * 72f / dpi;
                    fontPt = Math.Clamp(fontPt, 6f, maxPtByHeight);

                    var music = _music.Current;
                    bool hasMusic = music.HasSession && music.Title.Length > 0;

                    if (hasMusic)
                    {
                        DrawMusicArea(g, w, h, music, fontPt);
                    }
                    else
                    {
                        _musicVisible = false;
                        _songTextRect = RectangleF.Empty;
                        float x = barX + barW + 13f;
                        float textRight = w - 12f;

                        var state = g.Save();
                        g.SetClip(new RectangleF(x, 0, textRight - x, h));

                        float tw = DrawMainText(g, _currentTask, x, textRight, h, fontPt);
                        _taskTextRect = new RectangleF(x, 0, Math.Min(tw, textRight - x), h);

                        g.Restore(state);
                    }
                }
            }

            var hdcScreen = GetDC(IntPtr.Zero);
            var hdcMem = CreateCompatibleDC(hdcScreen);
            var hbm = bmp.GetHbitmap(Color.FromArgb(0));
            var old = SelectObject(hdcMem, hbm);

            var ptDst = new POINT { X = 0, Y = 0 };
            var size = new SIZE { cx = w, cy = h };
            var ptSrc = new POINT { X = 0, Y = 0 };
            var blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = (byte)_opacity, AlphaFormat = AC_SRC_ALPHA };

            UpdateLayeredWindow(hwnd, hdcScreen, ref ptDst, ref size, hdcMem, ref ptSrc, 0, ref blend, ULW_ALPHA);

            SelectObject(hdcMem, old);
            DeleteObject(hbm);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    // ===== 刷新当前任务显示 + 竖条进度色 =====
    void RefreshText()
    {
        var now = DateTime.Now;
        int m = now.Hour * 60 + now.Minute;
        var items = ItemsFor(DayMaskOf(now)); // 按当天星期取对应作息

        string name = "睡眠时间（晚安）";
        float progress = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            int s = ToMin(items[i].start);
            int en = (i + 1 < items.Count) ? ToMin(items[i + 1].start) : 24 * 60;
            if (m >= s && m < en)
            {
                name = items[i].name;
                progress = en > s ? Math.Clamp((m - s) / (float)(en - s), 0f, 1f) : 0f;
            }
        }

        // 竖条颜色：按当前任务剩余时间，青绿 → 黄 → 红
        _accent = progress < 0.5f
            ? LerpColor(BarStart, BarMid, progress / 0.5f)
            : LerpColor(BarMid, BarEnd, (progress - 0.5f) / 0.5f);

        _currentTask = name;
        Render();
    }

    // ===== 绘制主文字（返回实际文字像素宽度） =====
    float DrawMainText(Graphics g, string text, float x, float textRight, float h, float fontPt)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        using (var font = new Font("Microsoft YaHei UI", fontPt, FontStyle.Bold, GraphicsUnit.Point))
        using (var fmt = (StringFormat)StringFormat.GenericTypographic.Clone())
        using (var brush = new SolidBrush(_textMain))
        {
            fmt.Alignment = StringAlignment.Near;
            fmt.LineAlignment = StringAlignment.Center;
            fmt.Trimming = StringTrimming.None;
            float w = textRight - x;
            g.DrawString(text, font, brush, new RectangleF(x, 0, w, h), fmt);
            var sz = g.MeasureString(text, font, new SizeF(w, h), fmt);
            return Math.Min(sz.Width, w);
        }
    }

    static int ToMin(string hhmm)
    {
        var p = hhmm.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }

    static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    // ===== 音乐区：控制按钮 + 歌名 =====
    enum MusicButton { Prev, Play, Pause, Next }

    void DrawMusicArea(Graphics g, int w, int h, MediaInfo music, float fontPt)
    {
        // 右侧控制按钮布局（像素坐标）
        float btnD = 20f;
        float gap = 6f;
        float rightPad = 10f;
        float cy = h / 2f;
        float r = btnD / 2f;

        float nextCx = w - rightPad - r;
        float playCx = nextCx - btnD - gap;
        float prevCx = playCx - btnD - gap;

        // 歌曲信息区（靠右）：歌名 + 按钮
        float songRight = prevCx - r - gap;              // 歌名右边界
        float songMaxW = Math.Min(230f, w * 0.34f);      // 歌名最大宽度
        float songLeft = songRight - songMaxW;

        // 安排信息区（靠左）：竖条右侧到歌名左侧
        float leftX = 25.5f;
        float leftRight = songLeft - gap;

        // 按下缩放反馈
        float prevScale = _pressedMusicBtn == 1 ? PressScale : 1f;
        float playScale = _pressedMusicBtn == 2 ? PressScale : 1f;
        float nextScale = _pressedMusicBtn == 3 ? PressScale : 1f;

        DrawMusicButton(g, prevCx, cy, r, MusicButton.Prev, prevScale);
        DrawMusicButton(g, playCx, cy, r, music.IsPlaying ? MusicButton.Pause : MusicButton.Play, playScale);
        DrawMusicButton(g, nextCx, cy, r, MusicButton.Next, nextScale);

        // 缓存命中矩形（供鼠标点击判断，用未缩放尺寸）
        _btnPrev = new RectangleF(prevCx - r, cy - r, btnD, btnD);
        _btnPlay = new RectangleF(playCx - r, cy - r, btnD, btnD);
        _btnNext = new RectangleF(nextCx - r, cy - r, btnD, btnD);
        _musicVisible = true;

        // 安排信息（靠左，主色）
        if (leftRight - leftX > 12f)
        {
            float tw = DrawTextAligned(g, _currentTask, leftX, leftRight, h, fontPt, StringAlignment.Near, _textMain);
            _taskTextRect = new RectangleF(leftX, 0, Math.Min(tw, leftRight - leftX), h);
        }
        else
        {
            _taskTextRect = RectangleF.Empty;
        }

        // 歌曲名（靠右，次色、略小 + ♪ 前缀）
        if (songRight - songLeft > 12f)
        {
            float songPt = Math.Max(6f, fontPt * 0.82f);
            string songText = "♪ " + MetaText(music);
            float tw = DrawTextAligned(g, songText, songLeft, songRight, h, songPt, StringAlignment.Far, SongText);
            _songTextRect = new RectangleF(songRight - Math.Min(tw, songRight - songLeft), 0, Math.Min(tw, songRight - songLeft), h);
        }
        else
        {
            _songTextRect = RectangleF.Empty;
        }
    }

    static readonly Color SongText = Color.FromArgb(0xB8, 0xB8, 0xC0);
    const float PressScale = 0.82f;   // 音乐按钮按下时的缩放比例

    // 返回实际文字像素宽度
    float DrawTextAligned(Graphics g, string text, float x, float right, float h, float fontPt, StringAlignment align, Color color)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        using var font = new Font("Microsoft YaHei UI", fontPt, FontStyle.Bold, GraphicsUnit.Point);
        using var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
        using var brush = new SolidBrush(color);
        fmt.Alignment = align;
        fmt.LineAlignment = StringAlignment.Center;
        fmt.Trimming = StringTrimming.EllipsisCharacter;
        g.DrawString(text, font, brush, new RectangleF(x, 0, right - x, h), fmt);
        var sz = g.MeasureString(text, font, new SizeF(right - x, h), fmt);
        return Math.Min(sz.Width, right - x);
    }

    static string MetaText(MediaInfo m)
        => m.Artist.Length > 0 ? $"{m.Title} — {m.Artist}" : m.Title;

    void DrawMusicButton(Graphics g, float cx, float cy, float r, MusicButton kind, float scale)
    {
        r *= scale;

        // 圆形底 + 微光描边
        using (var bg = new SolidBrush(Color.FromArgb(44, 255, 255, 255)))
            g.FillEllipse(bg, cx - r, cy - r, r * 2, r * 2);
        using (var ring = new Pen(Color.FromArgb(64, 255, 255, 255), 1f))
            g.DrawEllipse(ring, cx - r, cy - r, r * 2, r * 2);

        using var fg = new SolidBrush(_textMain);
        switch (kind)
        {
            case MusicButton.Play:
                g.FillPolygon(fg, new PointF[]
                {
                    new(cx - r * 0.35f, cy - r * 0.55f),
                    new(cx - r * 0.35f, cy + r * 0.55f),
                    new(cx + r * 0.60f, cy),
                });
                break;

            case MusicButton.Pause:
                float bw = r * 0.30f;
                g.FillRectangle(fg, cx - r * 0.38f, cy - r * 0.52f, bw, r * 1.04f);
                g.FillRectangle(fg, cx + r * 0.08f, cy - r * 0.52f, bw, r * 1.04f);
                break;

            case MusicButton.Prev:
                // 后退：左向三角形 + 右侧竖条
                g.FillPolygon(fg, new PointF[]
                {
                    new(cx + r * 0.10f, cy - r * 0.50f),
                    new(cx + r * 0.10f, cy + r * 0.50f),
                    new(cx - r * 0.48f, cy),
                });
                g.FillRectangle(fg, cx + r * 0.38f, cy - r * 0.50f, r * 0.14f, r * 1.0f);
                break;

            case MusicButton.Next:
                // 前进：右向三角形 + 左侧竖条（与后退镜像一致）
                g.FillPolygon(fg, new PointF[]
                {
                    new(cx - r * 0.10f, cy - r * 0.50f),
                    new(cx - r * 0.10f, cy + r * 0.50f),
                    new(cx + r * 0.48f, cy),
                });
                g.FillRectangle(fg, cx - r * 0.52f, cy - r * 0.50f, r * 0.14f, r * 1.0f);
                break;
        }
    }
}
