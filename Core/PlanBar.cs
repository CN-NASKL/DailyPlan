using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DailyPlan.Services;
using DailyPlan.Forms;

namespace DailyPlan.Core;

// ===== 主窗体：字段 + 构造函数 + 生命周期 =====
partial class PlanBar : Form
{
    readonly System.Windows.Forms.Timer _refreshTimer;
    readonly System.Windows.Forms.Timer _stickyTimer;
    readonly System.Windows.Forms.Timer _musicTimer;

    // 当前配色
    Color _bgSolid = BgSolid;
    Color _accent = BarStart;
    Color _textMain = TextMain;

    // 当前显示的任务名
    string _currentTask = "";

    // 任务栏句柄与上次渲染尺寸缓存
    IntPtr _lastTray;
    int _lastW, _lastH;

    // 可调设置（持久化）
    int _opacity = 255;      // 0-255 整体透明度
    float _fontBase = 11f;   // 基准字号（磅），对应默认宽度 480
    int _barW = BarWidthDip; // 当前宽度（逻辑单位）
    int _barX = 0;           // 栏体在任务栏内的水平位置（逻辑单位）

    // 任务栏按钮占位区间缓存（逻辑单位，已排序合并）
    List<(int L, int R)> _occupied = new();

    // 拖拽调整大小状态
    bool _resizing;
    int _dragStartX;
    int _dragStartWpx;
    int _dragDpi = 96;

    // 拖拽整体移动状态
    bool _moving;
    int _moveStartX;
    int _moveStartBarX;
    int _moveDpi = 96;

    // 音乐服务（SMTC + 歌词）
    readonly MusicService _music = new();
    readonly LyricsService _lyrics = new();

    // 音乐按钮命中矩形（像素，渲染时更新）
    RectangleF _btnPrev, _btnPlay, _btnNext;
    bool _musicVisible;
    int _lastLyricIndex = -1;

    // 文字命中矩形（像素，渲染时更新，用于光标判断）
    RectangleF _taskTextRect, _songTextRect;

    // 按下的音乐按钮：0=无 1=上一首 2=播放/暂停 3=下一首
    int _pressedMusicBtn;

    static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.txt");

    public PlanBar()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(0x14, 0x14, 0x18);
        Size = new Size(BarWidthDip, 48);
        Location = new Point(0, 0);

        LoadSettings();

        _musicTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _musicTimer.Tick += (_, _) => MusicFrameTick();

        _music.Changed += OnMusicChanged;
        _lyrics.Changed += OnLyricsChanged;

        Load += (_, _) =>
        {
            EnsureEmbedded();
            RefreshOccupied();
            _barX = ClampBarX(_barX);
            ApplyBarSize();
            RefreshText();
            _stickyTimer.Start();
            _refreshTimer.Start();
            _musicTimer.Start();
            _ = InitMusicAsync();
        };

        _stickyTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _stickyTimer.Tick += (_, _) => EnsureEmbedded();

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 20000 };
        _refreshTimer.Tick += (_, _) => RefreshText();
    }

    // 打开作息编辑器；保存后更新 ScheduleGroups + 持久化 + 刷新展示
    void OpenScheduleEditor()
    {
        using var f = new ScheduleEditorForm(ScheduleGroups);
        if (f.ShowDialog() == DialogResult.OK)
        {
            ScheduleGroups.Clear();
            ScheduleGroups.AddRange(f.Result);
            SaveSchedule();
            WriteScheduleTxt();
            RefreshText();
        }
    }
}
