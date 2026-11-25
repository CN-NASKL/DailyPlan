using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DailyPlan.Core;
using DailyPlan.Controls;

namespace DailyPlan.Forms;

// ===== 作息安排编辑器（深色无边框圆角窗体） =====
// 支持三种日期模式：每天一致 / 工作日·周末 / 按星期独立
sealed class ScheduleEditorForm : Form
{
    static readonly Color Bg        = Color.FromArgb(0x1E, 0x1E, 0x24);   // 窗体底色
    static readonly Color PanelBg   = Color.FromArgb(0x2A, 0x2A, 0x2E);   // 列表面板背景
    static readonly Color InputBg   = Color.FromArgb(0x33, 0x33, 0x38);   // 输入框背景
    static readonly Color TextWhite = Color.FromArgb(0xFA, 0xFA, 0xFA);   // 白字
    static readonly Color TextDim   = Color.FromArgb(0xA1, 0xA1, 0xAA);   // 次要文字
    static readonly Color Accent    = Color.FromArgb(0x33, 0x8E, 0xF7);   // 强调蓝
    static readonly Color Danger    = Color.FromArgb(0xEF, 0x44, 0x44);   // 错误/删除

    [DllImport("user32.dll")]
    static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HTCAPTION = 2;

    // ===== 日期模式 =====
    enum Mode { All, WorkWeekend, PerDay }

    readonly List<ScheduleGroup> _groups = new();
    readonly List<Row> _rows = new();
    readonly List<Button> _modeButtons = new();
    readonly List<Button> _tabButtons = new();

    int _cur;
    Mode _mode;

    FlowLayoutPanel _listPanel = null!;
    Label _hint = null!;

    public List<ScheduleGroup> Result => _groups;

    public ScheduleEditorForm(IEnumerable<ScheduleGroup> groups)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ClientSize = new Size(480, 700);
        DoubleBuffered = true;
        ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9f);

        // 深拷贝传入分组
        foreach (var g in groups)
            _groups.Add(new ScheduleGroup { Days = g.Days, Items = new List<(string, string)>(g.Items) });
        if (_groups.Count == 0)
            _groups.Add(new ScheduleGroup { Days = PlanBar.AllDays, Items = new() });
        _mode = InferMode(_groups);

        BuildTitleBar();
        BuildModeBar();
        BuildListPanel();
        BuildHint();
        BuildFooter();

        UpdateModeButtons();
        UpdateTabs();
        RebuildList();
        ApplyRoundedRegion();
    }

    // ===== 标题栏 =====
    void BuildTitleBar()
    {
        var accentBar = new Panel { Location = new Point(20, 16), Size = new Size(3, 22), BackColor = Accent };
        accentBar.MouseDown += DragWindow;
        Controls.Add(accentBar);

        var title = new Label
        {
            Text = "我的作息安排",
            Location = new Point(34, 13),
            AutoSize = true,
            ForeColor = TextWhite,
            Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
        };
        title.MouseDown += DragWindow;
        Controls.Add(title);

        var closeBtn = new Label
        {
            Text = "✕",
            Location = new Point(ClientSize.Width - 40, 10),
            Size = new Size(28, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextDim,
            Font = new Font("Microsoft YaHei UI", 12f),
            Cursor = Cursors.Hand,
        };
        closeBtn.Click += (_, _) => Close();
        closeBtn.MouseEnter += (_, _) => closeBtn.ForeColor = TextWhite;
        closeBtn.MouseLeave += (_, _) => closeBtn.ForeColor = TextDim;
        Controls.Add(closeBtn);
    }

    // ===== 日期模式选择栏 =====
    void BuildModeBar()
    {
        var lbl = new Label
        {
            Text = "日期模式",
            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = TextDim,
        };
        Controls.Add(lbl);

        string[] texts = { "每天一致", "工作日/周末", "按星期独立" };
        int[] xs = { 20, 168, 316 };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var b = MakeToggle(texts[i], 140, 30);
            b.Location = new Point(xs[i], 76);
            b.Click += (_, _) => SwitchMode((Mode)idx);
            Controls.Add(b);
            _modeButtons.Add(b);
        }

        // 模式按钮与分组标签之间的分隔线
        Controls.Add(new Panel
        {
            Location = new Point(20, 118),
            Size = new Size(440, 1),
            BackColor = Color.FromArgb(0x3A, 0x3A, 0x40),
        });
    }

    void BuildListPanel()
    {
        _listPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 160),
            Size = new Size(440, 432),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = PanelBg,
            Padding = new Padding(8),
            Margin = new Padding(0),
        };
        Controls.Add(_listPanel);
    }

    void BuildHint()
    {
        _hint = new Label
        {
            Location = new Point(24, 602),
            AutoSize = true,
            ForeColor = Danger,
        };
        Controls.Add(_hint);
    }

    // ===== 底部按钮 =====
    void BuildFooter()
    {
        var addBtn = MakeButton("+ 添加", InputBg);
        addBtn.Location = new Point(20, 640);
        addBtn.Click += (_, _) =>
        {
            AddRow("", "");
            if (_rows.Count > 0) _listPanel.ScrollControlIntoView(_rows[^1].Panel);
        };
        Controls.Add(addBtn);

        var cancelBtn = MakeButton("取消", InputBg);
        cancelBtn.Location = new Point(ClientSize.Width - 200, 640);
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancelBtn);

        var saveBtn = MakeButton("保存", Accent);
        saveBtn.Location = new Point(ClientSize.Width - 100, 640);
        saveBtn.Click += (_, _) => OnSave();
        Controls.Add(saveBtn);
    }

    Button MakeButton(string text, Color bg)
    {
        return new Button
        {
            Text = text,
            Size = new Size(88, 32),
            BackColor = bg,
            ForeColor = TextWhite,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
        };
    }

    Button MakeToggle(string text, int width, int height)
    {
        return new Button
        {
            Text = text,
            Size = new Size(width, height),
            BackColor = InputBg,
            ForeColor = TextDim,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Microsoft YaHei UI", 9f),
        };
    }

    // ===== 模式推断与切换 =====
    static Mode InferMode(List<ScheduleGroup> groups)
    {
        if (groups.Count == 1 && groups[0].Days == PlanBar.AllDays) return Mode.All;
        if (groups.Count == 2 && groups[0].Days == PlanBar.Workdays && groups[1].Days == PlanBar.Weekend) return Mode.WorkWeekend;
        if (groups.Count == 7) return Mode.PerDay;
        return Mode.All;
    }

    void SwitchMode(Mode mode)
    {
        if (mode == _mode) return;
        CommitRows();

        var template = new List<(string, string)>(_groups[_cur].Items);

        int[] daysArr = mode switch
        {
            Mode.WorkWeekend => new[] { PlanBar.Workdays, PlanBar.Weekend },
            Mode.PerDay => new[] { PlanBar.DMon, PlanBar.DTue, PlanBar.DWed, PlanBar.DThu, PlanBar.DFri, PlanBar.DSat, PlanBar.DSun },
            _ => new[] { PlanBar.AllDays },
        };

        var newGroups = new List<ScheduleGroup>();
        foreach (var d in daysArr)
        {
            var old = _groups.Find(g => g.Days == d);
            var items = old != null ? new List<(string, string)>(old.Items) : new List<(string, string)>(template);
            newGroups.Add(new ScheduleGroup { Days = d, Items = items });
        }

        _groups.Clear();
        _groups.AddRange(newGroups);
        _mode = mode;
        _cur = 0;
        UpdateModeButtons();
        UpdateTabs();
        RebuildList();
    }

    void SwitchGroup(int i)
    {
        if (i == _cur) return;
        CommitRows();
        _cur = i;
        UpdateTabs();
        RebuildList();
    }

    void UpdateModeButtons()
    {
        for (int i = 0; i < _modeButtons.Count; i++)
        {
            bool sel = (int)_mode == i;
            _modeButtons[i].BackColor = sel ? Accent : InputBg;
            _modeButtons[i].ForeColor = sel ? TextWhite : TextDim;
        }
    }

    void UpdateTabs()
    {
        foreach (var b in _tabButtons) Controls.Remove(b);
        _tabButtons.Clear();

        int width = _groups.Count > 2 ? 56 : 130;
        int x = 20;
        for (int i = 0; i < _groups.Count; i++)
        {
            int idx = i;
            var b = MakeToggle(TabLabel(_groups[i].Days), width, 26);
            b.Location = new Point(x, 128);
            b.Click += (_, _) => SwitchGroup(idx);
            Controls.Add(b);
            _tabButtons.Add(b);
            x += width + 6;
        }

        for (int i = 0; i < _tabButtons.Count; i++)
        {
            bool sel = i == _cur;
            _tabButtons[i].BackColor = sel ? Accent : InputBg;
            _tabButtons[i].ForeColor = sel ? TextWhite : TextDim;
        }
    }

    static string TabLabel(int days)
    {
        if (days == PlanBar.AllDays) return "每天";
        if (days == PlanBar.Workdays) return "工作日(一~五)";
        if (days == PlanBar.Weekend) return "周末(六·日)";
        string[] names = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        int[] masks = { PlanBar.DMon, PlanBar.DTue, PlanBar.DWed, PlanBar.DThu, PlanBar.DFri, PlanBar.DSat, PlanBar.DSun };
        for (int i = 0; i < 7; i++)
            if (days == masks[i]) return names[i];
        return PlanBar.DayLabel(days);
    }

    // ===== 编辑行（FlowLayoutPanel 自动垂直排列，无手动坐标计算） =====
    TextBox MakeInput(string text, int width)
    {
        return new TextBox
        {
            Text = text,
            Width = width,
            BackColor = InputBg,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }

    void RebuildList()
    {
        _listPanel.Controls.Clear();
        _rows.Clear();
        foreach (var (start, name) in _groups[_cur].Items)
            AddRow(start, name);
    }

    void AddRow(string time, string name)
    {
        // 行容器
        var rowPanel = new Panel
        {
            Width = 396,
            Height = 42,
            BackColor = PanelBg,
            Margin = new Padding(0, 0, 0, 2),
        };

        var timeBox = new TimePicker { TimeText = time, Location = new Point(6, 8) };

        var sep = new Label
        {
            Text = "→",
            ForeColor = TextDim,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Location = new Point(94, 8),
            Size = new Size(18, 26),
        };

        var nameBox = MakeInput(name, 248);
        nameBox.Location = new Point(116, 8);
        nameBox.Height = 26;

        var del = new Label
        {
            Text = "✕",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextDim,
            Cursor = Cursors.Hand,
            Font = new Font("Microsoft YaHei UI", 10f),
            AutoSize = false,
            Location = new Point(366, 8),
            Size = new Size(24, 26),
        };
        var row = new Row { Panel = rowPanel, TimeBox = timeBox, Sep = sep, NameBox = nameBox, Del = del };
        del.Tag = row;
        del.Click += (_, _) => RemoveRow(row);
        del.MouseEnter += (_, _) => del.ForeColor = Danger;
        del.MouseLeave += (_, _) => del.ForeColor = TextDim;

        rowPanel.Controls.Add(timeBox);
        rowPanel.Controls.Add(sep);
        rowPanel.Controls.Add(nameBox);
        rowPanel.Controls.Add(del);

        _listPanel.Controls.Add(rowPanel);
        _rows.Add(row);
    }

    void RemoveRow(Row row)
    {
        _listPanel.Controls.Remove(row.Panel);
        _rows.Remove(row);
    }

    // 把当前行内容落回当前分组
    void CommitRows()
    {
        var items = new List<(string, string)>();
        foreach (var row in _rows)
            items.Add((row.TimeBox.TimeText, row.NameBox.Text.Trim()));
        _groups[_cur].Items = items;
    }

    // ===== 保存 =====
    void OnSave()
    {
        CommitRows();

        foreach (var g in _groups)
        {
            foreach (var (start, name) in g.Items)
            {
                if (!IsValidTime(start))
                {
                    _hint.Text = "时间格式错误：" + (start.Length == 0 ? "（空）" : start) + "，应为 HH:mm";
                    return;
                }
                if (name.Length == 0)
                {
                    _hint.Text = "任务名不能为空";
                    return;
                }
            }
            g.Items.Sort((a, b) => ToMinutes(a.start).CompareTo(ToMinutes(b.start)));
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    static bool IsValidTime(string t)
    {
        if (t.Length != 5 || t[2] != ':') return false;
        if (!int.TryParse(t.Substring(0, 2), out int h)) return false;
        if (!int.TryParse(t.Substring(3, 2), out int mm)) return false;
        return h >= 0 && h <= 23 && mm >= 0 && mm <= 59;
    }

    static int ToMinutes(string t)
    {
        var p = t.Split(':');
        return int.Parse(p[0]) * 60 + int.Parse(p[1]);
    }

    // ===== 拖动 / 圆角 =====
    void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.Y < 48)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    void ApplyRoundedRegion()
    {
        int r = 16;
        float d = r * 2f;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(Width - d, 0, d, d, 270, 90);
        path.AddArc(Width - d, Height - d, d, d, 0, 90);
        path.AddArc(0, Height - d, d, d, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }

    sealed class Row
    {
        public Panel Panel = null!;
        public TimePicker TimeBox = null!;
        public Label Sep = null!;
        public TextBox NameBox = null!;
        public Label Del = null!;
    }
}
