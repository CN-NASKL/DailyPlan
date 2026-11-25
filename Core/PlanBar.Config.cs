using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace DailyPlan.Core;

// ===== 作息分组：适用星期(位掩码) + 作息列表 =====
internal sealed class ScheduleGroup
{
    public int Days;
    public List<(string start, string name)> Items = new();
}

// ===== 配置：时间表 + 尺寸 + 配色 =====
partial class PlanBar
{
    // ===== 星期位掩码（bit0=周一 … bit6=周日） =====
    internal const int DMon = 1, DTue = 2, DWed = 4, DThu = 8, DFri = 16, DSat = 32, DSun = 64;
    internal const int AllDays  = DMon | DTue | DWed | DThu | DFri | DSat | DSun; // 127 每天
    internal const int Workdays = DMon | DTue | DWed | DThu | DFri;              // 31  工作日
    internal const int Weekend  = DSat | DSun;                                   // 96  周末

    // 默认作息（首次运行 / 无存档时使用）
    static readonly (string start, string name)[] DefaultItems =
    {
        ("06:30", "起床 + 洗漱 + 早餐"),
        ("07:00", "写《异星蓝图：从零开始的救世工厂》6000 字"),
        ("09:30", "行测卷（除政治理论）"),
        ("11:30", "午餐"),
        ("12:30", "午休"),
        ("13:00", "申论卷（除大作文）"),
        ("14:30", "俯卧撑 50 个 + 深蹲 100 个"),
        ("15:00", "写《烬穹之主》3000 字 × 2 章"),
        ("17:30", "写《第四天灾》3000 字"),
        ("18:30", "晚餐"),
        ("19:00", "EVE-CorpOS 模块开发"),
        ("20:30", "Blender 建模"),
        ("22:00", "AI 漫剧剧情一集"),
        ("23:00", "洗漱"),
        ("23:30", "睡觉"),
    };

    // 运行时作息分组（从 schedule_data.txt 加载，可被编辑器修改）
    internal static List<ScheduleGroup> ScheduleGroups = new()
    {
        new ScheduleGroup { Days = AllDays, Items = new List<(string, string)>(DefaultItems) },
    };

    static string SchedulePath => Path.Combine(AppContext.BaseDirectory, "schedule_data.txt");

    // ===== 星期工具 =====
    // 返回某天对应的位掩码（周一=1 … 周日=64）
    internal static int DayMaskOf(DateTime d)
        => 1 << (((int)d.DayOfWeek + 6) % 7);

    // 星期位掩码 → 可读标签（每天 / 工作日 / 周末 / 周一 周三 …）
    internal static string DayLabel(int days)
    {
        if (days == AllDays) return "每天";
        if (days == Workdays) return "工作日";
        if (days == Weekend) return "周末";

        string[] names = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        int[] masks = { DMon, DTue, DWed, DThu, DFri, DSat, DSun };
        var parts = new List<string>();
        for (int i = 0; i < 7; i++)
            if ((days & masks[i]) != 0) parts.Add(names[i]);
        return parts.Count > 0 ? string.Join(" ", parts) : "未设置";
    }

    // 取某天适用的作息列表（按 groups 顺序，首个掩码命中即返回）
    internal static List<(string start, string name)> ItemsFor(int dayMask)
    {
        foreach (var g in ScheduleGroups)
            if ((g.Days & dayMask) != 0)
                return g.Items;
        return ScheduleGroups.Count > 0
            ? ScheduleGroups[0].Items
            : new List<(string, string)>();
    }

    // ===== 存档读写（格式：days|HH:mm|name，兼容旧的 HH:mm|name 二段式） =====
    internal static void LoadSchedule()
    {
        try
        {
            if (!File.Exists(SchedulePath)) return;

            var map = new Dictionary<int, List<(string, string)>>();
            foreach (var raw in File.ReadAllLines(SchedulePath))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                int days;
                string t, n;
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    if (!int.TryParse(parts[0].Trim(), out days)) continue;
                    t = parts[1].Trim();
                    n = parts[2].Trim();
                }
                else if (parts.Length == 2)
                {
                    days = AllDays; // 旧格式默认「每天」
                    t = parts[0].Trim();
                    n = parts[1].Trim();
                }
                else continue;

                if (days <= 0 || t.Length == 0 || n.Length == 0) continue;
                if (!map.TryGetValue(days, out var list))
                    map[days] = list = new List<(string, string)>();
                list.Add((t, n));
            }

            if (map.Count > 0)
            {
                var groups = new List<ScheduleGroup>();
                foreach (var kv in map)
                    groups.Add(new ScheduleGroup { Days = kv.Key, Items = kv.Value });
                // 按掩码稳定排序（保持「每天/工作日/周末/单天」的可读顺序）
                groups.Sort((a, b) => a.Days.CompareTo(b.Days));
                ScheduleGroups = groups;
            }
        }
        catch { /* 忽略，保留默认 */ }
    }

    internal static void SaveSchedule()
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var g in ScheduleGroups)
                foreach (var (start, name) in g.Items)
                    sb.Append(g.Days).Append('|').Append(start).Append('|').Append(name).Append('\n');
            File.WriteAllText(SchedulePath, sb.ToString());
        }
        catch { /* 忽略写入失败 */ }
    }

    // 生成可读的时间表 txt（按分组）
    internal static void WriteScheduleTxt()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "schedule.txt");
        var lines = new List<string> { "每日任务时间表", "====================" };

        foreach (var g in ScheduleGroups)
        {
            if (g.Items.Count == 0) continue;
            lines.Add("");
            lines.Add("【" + DayLabel(g.Days) + "】");
            for (int i = 0; i < g.Items.Count; i++)
            {
                var end = i + 1 < g.Items.Count ? g.Items[i + 1].start : "24:00";
                lines.Add($"{g.Items[i].start} - {end}   {g.Items[i].name}");
            }
        }

        lines.Add("");
        lines.Add("23:00 之后为睡眠时间");
        File.WriteAllText(path, string.Join("\n", lines));
    }

    // 尺寸
    const int BarWidthDip = 480;        // 默认宽度（逻辑单位）
    const int MinWidthDip = 140;        // 拖拽最小宽度
    const int MaxWidthDip = 1600;       // 拖拽最大宽度
    const int ResizeZone = 14;          // 右侧拖拽手柄像素宽度

    // 配色（纯灰背景 + 进度色竖条）
    static readonly Color BgSolid      = Color.FromArgb(0x3E, 0x3E, 0x42);  // 纯灰背景（偏灰）
    static readonly Color TextMain     = Color.FromArgb(0xFA, 0xFA, 0xFA);  // 主文字白
    static readonly Color BorderColor  = Color.FromArgb(40, 255, 255, 255); // 微光描边
    // 竖条进度色：青绿 → 黄 → 红（按当前任务剩余时间）
    static readonly Color BarStart     = Color.FromArgb(0x2E, 0xD9, 0xA2);  // 青绿（时间充裕）
    static readonly Color BarMid       = Color.FromArgb(0xF5, 0xC5, 0x18);  // 黄（时间过半）
    static readonly Color BarEnd       = Color.FromArgb(0xEF, 0x44, 0x44);  // 红（即将结束）
}
