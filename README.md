<div align="center">

# DailyPlan · 每日计划任务栏

**原生嵌入 Windows 11 任务栏的每日计划展示栏 + 音乐控制**  
*A native Windows 11 taskbar widget for daily plans & music control*

[![License](https://img.shields.io/github/license/CN-NASKL/DailyPlan?style=flat-square&color=3A8EFF)](./LICENSE)
[![Release](https://img.shields.io/github/v/release/CN-NASKL/DailyPlan?style=flat-square)](https://github.com/CN-NASKL/DailyPlan/releases)
[![Stars](https://img.shields.io/github/stars/CN-NASKL/DailyPlan?style=flat-square)](https://github.com/CN-NASKL/DailyPlan/stargazers)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square)]()

</div>

---

## 🇨🇳 中文

### 这是什么

DailyPlan 是一个**原生嵌入 Windows 11 任务栏**的小工具：它以一条深色胶囊长条常驻任务栏，按当前时间自动显示你正在进行的安排；当系统有音乐播放时，右侧会自动浮现歌曲信息和控制按钮。

- 🎯 无弹窗、无独立窗口，真正"长"在任务栏里
- ⏰ 自动定位当前时段任务，进度竖条颜色随时间变化
- 🎵 音乐控制，读取系统正在播放的歌名/歌手

### ✨ 功能特性

- **原生嵌入任务栏**：通过 `SetParent` + `UpdateLayeredWindow` 逐像素 alpha，与开始菜单、系统图标共存，不被窗口覆盖。
- **当前时段展示**：自动显示进行中的任务，左侧竖条颜色随剩余时间由青绿 → 黄 → 红。
- **音乐控制**：读取系统媒体会话（QQ音乐 / 网易云 / Spotify / 浏览器等），显示歌名 + 上一首 / 播放暂停 / 下一首。
- **自由拖拽与避让**：栏体可在任务栏内任意拖动、调宽，自动避让系统按钮。
- **作息编辑器**：支持「每天一致 / 工作日·周末 / 按星期独立」，时间栏点击弹出滚轮选择。
- **深色设置面板**：透明度、字体大小、底色调色盘（含自定义取色器）、开机自启动。

### 📥 快速开始

1. 到 [Releases](https://github.com/CN-NASKL/DailyPlan/releases) 下载 `DailyPlan.exe`（自包含单文件，**无需安装 .NET**）。
2. 双击运行，计划栏会出现在任务栏左侧。
3. **右键计划栏 → 「我的作息安排」**，添加你的每日计划。

> 首次运行默认是**空计划**，需要你自己添加作息；添加后会自动保存到 exe 同目录。

### 📖 使用指南

| 操作 | 效果 |
|---|---|
| 右键计划栏 | 打开设置面板 |
| 拖动栏体 / 右边缘 | 整体移动 / 调整宽度 |
| 「我的作息安排」 | 编辑每日作息（日期模式 + 时间滚轮） |
| 「底色」 | 更换背景色（预设色块 + 自定义取色器） |
| 「开机启动」 | 开机自动运行 |
| 播放音乐 | 右侧自动显示歌名 + 控制按钮 |

### 🛠 从源码构建

需要 [.NET 9 SDK](https://dotnet.microsoft.com/download)（Windows）。

```bash
# 编译
dotnet build -c Release

# 发布为自包含单文件（输出到 dist/DailyPlan.exe）
dotnet publish -c Release -r win-x64 --self-contained true
```

**技术栈**：C# / WinForms · Win32 P/Invoke · UIA（任务栏按钮探测）· WinRT SMTC（媒体控制）

### 📁 项目结构

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

### ⚠️ 说明

- 仅支持 **Windows**（针对 Windows 11 任务栏布局优化）。
- 未签名开源程序，下载运行时 SmartScreen 可能提示拦截，点「更多信息 → 仍要运行」即可。

### 📄 License

[MIT](./LICENSE)

<br>
<br>

---

## 🌐 English

### What is this

DailyPlan is a widget **natively embedded in the Windows 11 taskbar**: it lives on the taskbar as a dark capsule bar, showing the task you're currently doing based on the time. When music is playing, the song info and control buttons appear on the right automatically.

- 🎯 No popup, no separate window — it truly lives *inside* the taskbar
- ⏰ Auto-highlights the current task, with a progress bar that changes color over time
- 🎵 Music control — reads the song/artist currently playing on your system

### ✨ Features

- **Native taskbar embedding**: uses `SetParent` + `UpdateLayeredWindow` per-pixel alpha, coexisting with the Start menu and system icons.
- **Current-task display**: shows the active task, with a left color bar that shifts green → yellow → red as time runs out.
- **Music control**: reads the system media session (QQ Music / NetEase / Spotify / browser, etc.), showing song info plus previous / play-pause / next.
- **Drag & avoid**: drag and resize the bar anywhere on the taskbar; it auto-avoids system buttons.
- **Schedule editor**: supports "same every day / weekdays & weekends / per-weekday", with a wheel picker for time.
- **Dark settings panel**: opacity, font size, background palette (with custom color picker), auto-start.

### 📥 Quick start

1. Download `DailyPlan.exe` from [Releases](https://github.com/CN-NASKL/DailyPlan/releases) (self-contained, **no .NET install needed**).
2. Double-click to run — the bar appears on the left of the taskbar.
3. **Right-click the bar → "Schedule"** to add your daily plan.

> First run starts with an **empty schedule**; add your own items, which are saved automatically next to the exe.

### 📖 Usage

| Action | Effect |
|---|---|
| Right-click the bar | Open the settings panel |
| Drag the bar / right edge | Move / resize |
| "Schedule" | Edit daily schedule (date mode + time wheel) |
| "Background" | Change bar color (presets + custom picker) |
| "Auto-start" | Run at login |
| Play music | Song info + controls appear on the right |

### 🛠 Build from source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download) (Windows).

```bash
# Build
dotnet build -c Release

# Publish a self-contained single file (outputs to dist/DailyPlan.exe)
dotnet publish -c Release -r win-x64 --self-contained true
```

**Tech stack**: C# / WinForms · Win32 P/Invoke · UIA (taskbar button detection) · WinRT SMTC (media control)

### 📁 Project structure

```
daily-plan-wpf/
├── Program.cs                 # Entry point
├── Core/                      # Main window PlanBar (partial class)
│   ├── PlanBar.cs             #   fields / ctor / lifecycle
│   ├── PlanBar.Config.cs      #   config / schedule group model
│   ├── PlanBar.Interaction.cs #   drag / resize / settings persistence
│   ├── PlanBar.Menu.cs        #   settings panel entry
│   ├── PlanBar.Music.cs       #   music lifecycle
│   ├── PlanBar.Native.cs      #   Win32 P/Invoke
│   ├── PlanBar.Render.cs      #   per-pixel render / text / music area
│   ├── PlanBar.Startup.cs     #   auto-start
│   └── PlanBar.Taskbar.cs     #   taskbar embedding / UIA avoidance
├── Services/                  # Service layer
│   └── MusicService.cs        #   SMTC music service
├── Controls/                  # Custom controls
│   ├── Slider.cs              #   modern slider
│   ├── ToggleSwitch.cs        #   toggle switch
│   ├── TimePicker.cs          #   time field
│   ├── TimePickerPopup.cs     #   time picker popup
│   ├── WheelPicker.cs         #   wheel selector
│   ├── ColorPicker.cs         #   color palette
│   ├── ColorPickerDialog.cs   #   custom color picker
│   └── DarkMenuRenderer.cs    #   dark menu renderer
└── Forms/                     # Forms
    ├── SettingsForm.cs        #   settings panel
    └── ScheduleEditorForm.cs  #   schedule editor
```

### ⚠️ Notes

- Windows only (optimized for Windows 11 taskbar layout).
- This is an unsigned open-source app, so SmartScreen may warn on download — click "More info → Run anyway".

### 📄 License

[MIT](./LICENSE)
