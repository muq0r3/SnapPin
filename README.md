# SnapPin

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WinForms](https://img.shields.io/badge/WinForms-Desktop-0A84FF?style=for-the-badge)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Release](https://img.shields.io/badge/release-v0.1.0-2ea44f?style=for-the-badge)

Keep a screenshot in front of you while you work. Capture a region, mark it up, pin it on top.

把截图留在眼前。框选、标注，然后钉在最上层。

[English](#english) · [中文](#中文)

---

## English

### What it does

SnapPin captures a region of the screen, gives you a small set of annotation tools, and keeps the result floating above other windows.

No account, no background service, no installer. One exe and an optional config file.

### Highlights

- Global hotkeys for capture and pin.
- Region selection with live pixel dimensions.
- Pen, arrow, rectangle, mosaic brush and inline text.
- Annotations can be selected, moved, resized and deleted.
- Pins can be dragged, zoomed, copied, saved and locked in place.
- Recent screenshots stay in a history menu for quick re-pinning.
- Tray menu, configurable hotkeys and optional autostart.
- Per-monitor V2 DPI awareness; captures use physical pixels.
- Portable by default: history and settings live next to the app.

### Usage

| Action | Result |
| --- | --- |
| `Ctrl + Alt + A` | Select a region, annotate, then choose an outcome |
| `Ctrl + Alt + P` | Pin the image currently in the clipboard |
| Drag a pin | Move it |
| Mouse wheel | Zoom it |
| Double-click / `Esc` | Close it |
| Right-click a pin | Copy, save, toggle topmost, lock, close |
| Tray menu | Capture, pin, history, pin manager, settings, autostart, exit |

After a capture you can **Pin** (copy and pin), **Copy** (clipboard only), **Save** (image file) or **Cancel**.

Editor shortcuts: `Ctrl+Z` undo, `Delete` delete selection, `Enter` pin, `Ctrl+C` copy, `Ctrl+S` save, `Esc` cancel.

### Build

Visual Studio 2019+ or Build Tools with the .NET Framework 4.7.2 targeting pack.

```powershell
.\build.ps1
```

The exe lands in `SnapPin\bin\Release\SnapPin.exe`.

### Configuration

Settings are stored in `%APPDATA%\SnapPin\settings.json` and can also be edited from the tray menu.

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

`historyDirectory` is optional. When empty, SnapPin uses the app folder if it is writable, otherwise `%APPDATA%\SnapPin\history`.

### Notes

- Locking a pin freezes its position until you unlock it from the tray.
- On mixed-DPI setups the selection overlay may look slightly stretched on a secondary monitor; the capture itself still uses that monitor's physical pixels.
- Unhandled errors are appended to `error.log` next to the executable.

---

## 中文

### 它做什么

工作时想让一张截图一直留在眼前。SnapPin 负责框选、标注，然后把截图钉在其他窗口上面。

不需要账号，不常驻后台服务，也没有安装程序。一个 exe，加一个可选的配置文件。

### 特性

- 全局快捷键，随时截图、贴图。
- 框选时实时显示像素尺寸。
- 画笔、箭头、矩形、马赛克涂抹、行内文字。
- 标注可以选中、移动、缩放、删除。
- 贴图可以拖动、缩放、复制、保存、锁定位置。
- 最近的截图会留在历史记录里，随时重新贴出。
- 托盘菜单、自定义快捷键、可选开机自启。
- Per-Monitor V2 高分屏支持，截图按物理像素获取。
- 默认便携：历史和配置都放在程序旁边。

### 用法

| 操作 | 结果 |
| --- | --- |
| `Ctrl + Alt + A` | 框选、标注，然后选择去向 |
| `Ctrl + Alt + P` | 把剪贴板里的图片贴出来 |
| 拖动贴图 | 移动 |
| 鼠标滚轮 | 缩放 |
| 双击 / `Esc` | 关闭 |
| 右键贴图 | 复制、保存、置顶开关、锁定、关闭 |
| 托盘菜单 | 截图、贴图、历史记录、贴图管理、设置、开机自启、退出 |

截图后可以选 **贴图**（复制并置顶）、**复制**（只进剪贴板）、**保存**（存为图片）或 **取消**。

标注快捷键：`Ctrl+Z` 撤销，`Delete` 删除选中，`Enter` 贴图，`Ctrl+C` 复制，`Ctrl+S` 保存，`Esc` 取消。

### 构建

需要 Visual Studio 2019+，或带 .NET Framework 4.7.2 目标包的 Build Tools。

```powershell
.\build.ps1
```

产物在 `SnapPin\bin\Release\SnapPin.exe`。

### 配置

配置在 `%APPDATA%\SnapPin\settings.json`，也可以在托盘菜单里改。

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

`historyDirectory` 可以留空。留空时，程序目录可写就用程序目录，否则用 `%APPDATA%\SnapPin\history`。

### 说明

- 锁定贴图会固定位置，解锁入口在托盘。
- 多屏混合 DPI 时，框选遮罩在副屏上可能略有拉伸，截图本身仍按该显示器的物理像素获取。
- 未处理的异常会追加写入程序目录下的 `error.log`。

---

## Project layout

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
