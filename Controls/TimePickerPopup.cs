using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DailyPlan.Controls;

// ===== 时间选择弹窗：输入框（可直接输入）+ 小时/分钟滚轮 + 确定/取消 =====
sealed class TimePickerPopup : Form
{
    static readonly Color Bg        = Color.FromArgb(0x1E, 0x1E, 0x24);   // 窗体底色
    static readonly Color InputBg   = Color.FromArgb(0x33, 0x33, 0x38);   // 输入框背景
    static readonly Color PanelBg   = Color.FromArgb(0x2A, 0x2A, 0x2E);   // 滚轮面板背景
    static readonly Color TextWhite = Color.FromArgb(0xFA, 0xFA, 0xFA);   // 白字
    static readonly Color TextDim   = Color.FromArgb(0xA1, 0xA1, 0xAA);   // 次要文字
    static readonly Color Accent    = Color.FromArgb(0x33, 0x8E, 0xF7);   // 强调蓝

    readonly WheelPicker _hourWheel;
    readonly WheelPicker _minWheel;
    readonly MaskedTextBox _input;
    bool _syncing;

    public string TimeResult { get; private set; } = "06:30";

    public TimePickerPopup(int hour, int minute, Point anchor)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        BackColor = Bg;
        ClientSize = new Size(240, 336);
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Font = new Font("Microsoft YaHei UI", 9f);
        TopMost = true;

        // 标题
        var title = new Label
        {
            Text = "选择时间",
            Location = new Point(0, 14),
            Size = new Size(240, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextWhite,
            BackColor = Bg,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
        };
        Controls.Add(title);

        // 输入框：掩码 00:00，冒号固定不可删除，只输入数字
        _input = new MaskedTextBox
        {
            Location = new Point(60, 46),
            Size = new Size(120, 30),
            TextAlign = HorizontalAlignment.Center,
            BackColor = InputBg,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold),
            Mask = "00:00",
            PromptChar = ' ',
            BeepOnError = false,
        };
        _input.TextChanged += (_, _) => SyncFromInput();
        Controls.Add(_input);

        // 滚轮面板
        var panel = new Panel
        {
            Location = new Point(24, 90),
            Size = new Size(192, 170),
            BackColor = PanelBg,
        };
        _hourWheel = new WheelPicker { Location = new Point(8, 0) };
        _minWheel  = new WheelPicker { Location = new Point(104, 0) };
        _hourWheel.Setup(0, 23, hour);
        _minWheel.Setup(0, 59, minute);
        _hourWheel.ValueChanged += (_, _) => SyncFromWheels();
        _minWheel.ValueChanged += (_, _) => SyncFromWheels();

        var colon = new Label
        {
            Text = ":",
            Location = new Point(88, 70),
            Size = new Size(16, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextWhite,
            BackColor = PanelBg,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
        };

        panel.Controls.Add(_hourWheel);
        panel.Controls.Add(colon);
        panel.Controls.Add(_minWheel);
        Controls.Add(panel);

        // 底部按钮
        var cancel = MakeButton("取消", InputBg);
        cancel.Location = new Point(24, 282);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);

        var ok = MakeButton("确定", Accent);
        ok.Location = new Point(128, 282);
        ok.Click += (_, _) => Confirm();
        Controls.Add(ok);

        _input.Text = $"{hour:00}:{minute:00}";

        ApplyRoundedRegion();
        PositionNear(anchor);

        // 打开时取消输入框自动全选（避免系统蓝色选中高亮与深色 UI 不协调）
        Shown += (_, _) => { _input.SelectionLength = 0; _input.SelectionStart = 0; };
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

    void SyncFromWheels()
    {
        if (_syncing) return;
        _syncing = true;
        _input.Text = $"{_hourWheel.Value:00}:{_minWheel.Value:00}";
        _syncing = false;
    }

    void SyncFromInput()
    {
        if (_syncing) return;
        if (TryParse(_input.Text, out int h, out int m))
        {
            _syncing = true;
            _hourWheel.Value = h;
            _minWheel.Value = m;
            _syncing = false;
        }
    }

    void Confirm()
    {
        if (TryParse(_input.Text, out int h, out int m))
            TimeResult = $"{h:00}:{m:00}";
        else
            TimeResult = $"{_hourWheel.Value:00}:{_minWheel.Value:00}";
        DialogResult = DialogResult.OK;
        Close();
    }

    // 严格解析：必须恰好 4 位数字（HHmm），否则无效
    static bool TryParse(string s, out int h, out int m)
    {
        h = 0; m = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var digits = "";
        foreach (var c in s)
            if (c >= '0' && c <= '9') digits += c;
        if (digits.Length != 4) return false;

        h = (digits[0] - '0') * 10 + (digits[1] - '0');
        m = (digits[2] - '0') * 10 + (digits[3] - '0');
        return h >= 0 && h <= 23 && m >= 0 && m <= 59;
    }

    void ApplyRoundedRegion()
    {
        int r = 14;
        float d = r * 2f;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(Width - d, 0, d, d, 270, 90);
        path.AddArc(Width - d, Height - d, d, d, 0, 90);
        path.AddArc(0, Height - d, d, d, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }

    void PositionNear(Point anchor)
    {
        var wa = Screen.FromPoint(anchor).WorkingArea;
        int x = anchor.X;
        int y = anchor.Y;
        if (y + Height > wa.Bottom) y = anchor.Y - Height;
        if (y < wa.Top) y = wa.Top;
        x = Math.Clamp(x, wa.Left, wa.Right - Width);
        Location = new Point(x, y);
    }
}
