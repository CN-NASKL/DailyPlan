# DailyPlan · 每日计划任务栏

一个**原生嵌入 Windows 11 任务栏**的每日计划展示栏。它在任务栏上以一条深色胶囊长条常驻显示你当前时间段的安排，并可在有音乐播放时自动浮现歌曲信息与播放控制按钮。

> DailyPlan is a Windows 11 taskbar widget that shows your daily schedule inline, with an optional music controller / now-playing display via the system SMTC (System Media Transport Controls).

## 📥 下载

在 [Releases](https://github.com/CN-NASKL/DailyPlan/releases) 页面下载 `DailyPlan.exe`（自包含单文件，无需安装 .NET），双击即可运行。

## ✨ 功能特性

- **原生嵌入任务栏**：通过 `SetParent` 成为 `Shell_TrayWnd` 子窗口，使用 `UpdateLayeredWindow` + 逐像素 alpha 突破任务栏 Mica / DirectComposition 层，与系统图标共存。
- **当前时段安排展示**：自动根据当前时间定位并高亮显示正在进行的任务，左侧竖条颜色随时间进度由青绿 → 黄 → 红。
- **音乐控制**（可选）：通过系统 SMTC 自动读取当前播放器（QQ音乐 / 网易云 / Spotify / 浏览器等）的歌名与歌手，提供上一首 / 播放暂停 / 下一首按钮。
- **自由拖拽与避让**：栏体可在任务栏内任意拖动、调宽，并通过 UIA 探测自动避让「开始 / 搜索 / 任务视图 / 小组件 / 托盘」等系统按钮。
- **作息编辑器**：支持「每天一致 / 工作日·周末 / 按星期独立」三种日期模式，时间栏可点击弹出滚轮 + 输入框双模式选择。
- **现代化深色设置面板**：透明度、字体大小、底色调色盘（含自定义取色器）、开机自启动开关。
- **开机自启动**：通过注册表 `HKCU\...\Run` 实现（无需管理员权限）。

## 🛠 技术栈

- C# / WinForms（`.NET 9`，`net9.0-windows10.0.19041.0`）
- Win32 P/Invoke（`UpdateLayeredWindow`、`SetParent`、DPI、分层窗口）
- UIA（UI Automation）任务栏按钮边界探测
- WinRT `Windows.Media.Control`（SMTC 媒体控制）

## 📁 目录结构

```
daily-plan-wpf/
├── Program.cs                 # 程序入口
├── Core/                      # 主窗体 PlanBar（partial class）
│   ├── PlanBar.cs             #   字段 / 构造 / 生命周期
│   ├── PlanBar.Config.cs      #   配置 / 作息分组数据模型
│   ├── PlanBar.Interaction.cs #   拖动 / 调宽 / 设置持久化
│   ├── PlanBar.Menu.cs        #   设置面板入口
│   ├── PlanBar.Music.cs       #   音乐生命周期
│   ├── PlanBar.Native.cs      #   Win32 P/Invoke
│   ├── PlanBar.Render.cs      #   逐像素渲染 / 文字 / 音乐区
│   ├── PlanBar.Startup.cs     #   开机自启动
│   └── PlanBar.Taskbar.cs     #   任务栏嵌入 / UIA 避让
├── Services/                  # 服务层
│   └── MusicService.cs        #   SMTC 音乐服务
├── Controls/                  # 自定义控件
│   ├── Slider.cs              #   现代化滑动条
│   ├── ToggleSwitch.cs        #   开关按钮
│   ├── TimePicker.cs          #   时间栏
│   ├── TimePickerPopup.cs     #   时间选择弹窗
│   ├── WheelPicker.cs         #   滚轮选择器
│   ├── ColorPicker.cs         #   调色盘
│   ├── ColorPickerDialog.cs   #   自定义取色器
│   └── DarkMenuRenderer.cs    #   深色菜单渲染器
└── Forms/                     # 窗体
    ├── SettingsForm.cs        #   设置面板
    └── ScheduleEditorForm.cs  #   作息编辑器
```

## 🚀 构建与运行

需要 [.NET 9 SDK](https://dotnet.microsoft.com/download)（Windows）。

```bash
# 编译
dotnet build -c Release

# 发布为自包含单文件（输出到 dist/）
dotnet publish -c Release -r win-x64 --self-contained true
```

运行 `dist\DailyPlan.exe` 即可。首次运行会使用内置的默认作息；修改作息、位置、配色等设置后会自动保存到 exe 同目录下的 `settings.txt` / `schedule_data.txt`。

## 📖 使用说明

| 操作 | 说明 |
|---|---|
| 右键计划栏 | 打开设置面板 |
| 拖动栏体 / 右缘 | 整体移动 / 调整宽度 |
| 「我的作息安排」 | 打开作息编辑器（日期模式 + 时间滚轮） |
| 「底色」 | 预设色块 + 自定义取色器 |
| 「开机启动」 | 切换开机自启动 |
| 播放音乐 | 计划栏右侧自动显示歌名 + 控制按钮 |

## ⚠️ 说明

- 仅支持 Windows（针对 Windows 11 任务栏布局优化）。

## 📄 License

[MIT](./LICENSE)
