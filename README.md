# SnapPin

A small Windows screenshot tool that captures a screen region and pins it above other windows.

一个轻量的 Windows 截图置顶工具：框选截图、标注，然后把截图钉在屏幕最上层。

[English](#english) · [简体中文](#简体中文)

---

## English

### What it is

SnapPin takes a screenshot of a screen region and keeps it visible on top of other windows. Capture, annotate, pin, move on.

Built with C# and WinForms on .NET Framework 4.7.2. It starts fast, uses little memory, and runs on Windows 10/11 without installing a separate runtime.

### Features

- Global hotkeys for capture and pin.
- Region selection with live size display.
- Annotations: pen, arrow, rectangle, mosaic brush and text.
- Select, move, resize and delete annotations.
- Pin windows: drag, zoom, copy, save and lock position.
- Screenshot history with one-click re-pin.
- Tray menu, configurable hotkeys and optional autostart.
- Per-monitor V2 DPI awareness, capture in physical pixels.
- Single instance: launching the exe again starts a new capture.

### Usage

| Action | Description |
| --- | --- |
| `Ctrl + Alt + A` | Select a region, annotate, then choose an action |
| `Ctrl + Alt + P` | Pin the image currently in the clipboard |
| Drag a pin | Move the pinned window |
| Mouse wheel | Zoom the pinned window |
| Double-click / `Esc` | Close the pin |
| Right-click a pin | Copy, save, toggle topmost, lock, close |
| Tray menu | Capture, pin, history, pin manager, settings, autostart, exit |

After selecting a region, the editor offers four outcomes: **Pin** (copy and pin), **Copy** (clipboard only), **Save** (write an image file) or **Cancel**.

Annotation shortcuts: `Ctrl+Z` undo, `Delete` remove the selected annotation, `Enter` pin, `Ctrl+C` copy, `Ctrl+S` save, `Esc` cancel.

Text annotations are edited directly on the image through a transparent text box. Chinese and other IME input work as usual.

### Build

Required: Visual Studio 2019+ or Build Tools with the .NET Framework 4.7.2 targeting pack.

```powershell
.\build.ps1
```

or

```powershell
msbuild SnapPin.sln /p:Configuration=Release /p:Platform="Any CPU"
```

The executable is written to `SnapPin\bin\Release\SnapPin.exe`.

### Configuration

Settings live in `%APPDATA%\SnapPin\settings.json`. Hotkeys and the history directory can also be changed from the tray menu.

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

- An empty `historyDirectory` means automatic: use the program folder when writable, otherwise `%APPDATA%\SnapPin\history`.
- Hotkeys accept `Ctrl`, `Alt`, `Shift`, `Win`, plus letters, digits, `F1`–`F24`, `Space`, `Enter`, `Esc`, `Tab`, `Backspace` and arrow keys.

### Notes

- Locking a pin freezes its position. It cannot be dragged, zoomed or closed until unlocked from the tray menu.
- On mixed-DPI multi-monitor setups the selection overlay may look slightly stretched on secondary monitors; the captured image is still taken at the monitor's physical resolution.
- Unhandled errors are written to `error.log` next to the executable.

### Project layout

```
SnapPin/
├─ SnapPin.sln
├─ build.ps1
└─ SnapPin/
   ├─ Program.cs              entry point, single instance, error logging
   ├─ SnapPinApp.cs           tray, hotkeys, capture pipeline, pins
   ├─ RegionSelectorForm.cs   full-screen region selection
   ├─ AnnotatorForm.cs        annotation editor
   ├─ PinForm.cs              always-on-top image window
   ├─ TransparentTextBox.cs   inline text input over the image
   ├─ ScreenCaptureService.cs physical-pixel screen capture
   ├─ ScreenMetrics.cs        DPI and monitor geometry
   ├─ HistoryManager.cs       screenshot history
   ├─ SettingsForm.cs         hotkey and directory settings
   └─ app.ico                 application and tray icon
```

---

## 简体中文

### 简介

SnapPin 用来截取屏幕的一块区域，并把截图固定在其它窗口之上。流程很短：截图、标注、钉住。

使用 C# + WinForms，目标框架 .NET Framework 4.7.2。启动快、占用低，Windows 10/11 无需另外安装运行时。

### 功能

- 全局快捷键截图与贴图。
- 框选时实时显示尺寸。
- 标注：画笔、箭头、矩形、马赛克涂抹、文字。
- 标注可选中、移动、缩放、删除。
- 贴图窗口支持拖动、滚轮缩放、复制、保存、锁定位置。
- 截图历史记录，随时重新贴出。
- 托盘菜单、自定义快捷键、可选开机自启。
- Per-Monitor V2 高分屏支持，按物理像素截图。
- 单实例：再次启动 exe 会直接触发一次截图。

### 使用

| 操作 | 说明 |
| --- | --- |
| `Ctrl + Alt + A` | 框选截图，标注后选择去向 |
| `Ctrl + Alt + P` | 把剪贴板里的图片贴出来 |
| 拖动贴图 | 移动窗口 |
| 鼠标滚轮 | 缩放窗口 |
| 双击 / `Esc` | 关闭贴图 |
| 右键贴图 | 复制、另存为、置顶开关、锁定、关闭 |
| 托盘菜单 | 截图、贴图、历史记录、贴图管理、设置、开机自启、退出 |

框选完成后，编辑器提供四种去向：**贴图**（复制并置顶）、**复制**（只进剪贴板）、**保存**（存为图片）、**取消**。

标注快捷键：`Ctrl+Z` 撤销，`Delete` 删除选中，`Enter` 贴图，`Ctrl+C` 复制，`Ctrl+S` 保存，`Esc` 取消。

文字标注是直接画在图片上的透明文本框，中文等输入法可以正常使用。

### 构建

需要 Visual Studio 2019+ 或装有 .NET Framework 4.7.2 目标包的 Build Tools。

```powershell
.\build.ps1
```

或者

```powershell
msbuild SnapPin.sln /p:Configuration=Release /p:Platform="Any CPU"
```

产物位于 `SnapPin\bin\Release\SnapPin.exe`。

### 配置

配置文件在 `%APPDATA%\SnapPin\settings.json`。快捷键和历史目录也可以在托盘「设置」中修改。

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

- `historyDirectory` 为空时自动选择：程序目录可写就用程序目录，否则用 `%APPDATA%\SnapPin\history`。
- 快捷键支持 `Ctrl`、`Alt`、`Shift`、`Win`，主键支持字母、数字、`F1`–`F24`、`Space`、`Enter`、`Esc`、`Tab`、`Backspace` 和方向键。

### 说明

- 锁定贴图会固定它的位置，不能再拖动、缩放或关闭；解锁入口在托盘「贴图管理」。
- 多屏混合 DPI 时，框选遮罩在副屏上可能有轻微拉伸，但截图仍按所在显示器的物理分辨率获取。
- 未处理的异常会写入程序目录下的 `error.log`。

### 目录结构

```
SnapPin/
├─ SnapPin.sln
├─ build.ps1
└─ SnapPin/
   ├─ Program.cs              入口、单实例、异常日志
   ├─ SnapPinApp.cs           托盘、快捷键、截图流程、贴图
   ├─ RegionSelectorForm.cs   全屏框选
   ├─ AnnotatorForm.cs        标注编辑器
   ├─ PinForm.cs              置顶贴图窗口
   ├─ TransparentTextBox.cs   图片上的透明文字输入
   ├─ ScreenCaptureService.cs 物理像素截图
   ├─ ScreenMetrics.cs        DPI 与显示器几何
   ├─ HistoryManager.cs       截图历史
   ├─ SettingsForm.cs         快捷键与目录设置
   └─ app.ico                 程序与托盘图标
```
