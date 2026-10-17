# SnapPin

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WinForms](https://img.shields.io/badge/WinForms-Desktop-0A84FF?style=for-the-badge)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Release](https://img.shields.io/badge/release-v0.1.0-2ea44f?style=for-the-badge)
![License](https://img.shields.io/github/license/muq0r3/SnapPin?style=for-the-badge)

**English** · [简体中文](README.zh-CN.md)

Keep a screenshot in front of you while you work. Capture a region, mark it up, pin it on top.

## What it does

SnapPin captures a region of the screen, gives you a small set of annotation tools, and keeps the result floating above other windows.

No account, no background service, no installer. One exe and an optional config file.

## Highlights

- Global hotkeys for capture and pin.
- Region selection with live pixel dimensions.
- Pen, arrow, rectangle, mosaic brush and inline text.
- Annotations can be selected, moved, resized and deleted.
- Pins can be dragged, zoomed, copied, saved and locked in place.
- Recent screenshots stay in a history menu for quick re-pinning.
- Tray menu, configurable hotkeys and optional autostart.
- Per-monitor V2 DPI awareness; captures use physical pixels.
- Portable by default: history and settings live next to the app.

## Usage

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

## Build

Visual Studio 2019+ or Build Tools with the .NET Framework 4.7.2 targeting pack.

```powershell
.\build.ps1
```

The exe lands in `SnapPin\bin\Release\SnapPin.exe`.

## Configuration

Settings are stored in `%APPDATA%\SnapPin\settings.json` and can also be edited from the tray menu.

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

`historyDirectory` is optional. When empty, SnapPin uses the app folder if it is writable, otherwise `%APPDATA%\SnapPin\history`.

## Notes

- Locking a pin freezes its position until you unlock it from the tray.
- On mixed-DPI setups the selection overlay may look slightly stretched on a secondary monitor; the capture itself still uses that monitor's physical pixels.
- Unhandled errors are appended to `error.log` next to the executable.

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

## License

MIT © 2026 muq0r3
