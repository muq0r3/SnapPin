# SnapPin

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WinForms](https://img.shields.io/badge/WinForms-Desktop-0A84FF?style=for-the-badge)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Release](https://img.shields.io/badge/release-v0.1.0-2ea44f?style=for-the-badge)

[English](README.md) · **简体中文**

工作时想让一张截图一直留在眼前。框选、标注，然后钉在最上层。

## 它做什么

SnapPin 负责框选、标注，然后把截图钉在其他窗口上面。

不需要账号，不常驻后台服务，也没有安装程序。一个 exe，加一个可选的配置文件。

## 特性

- 全局快捷键，随时截图、贴图。
- 框选时实时显示像素尺寸。
- 画笔、箭头、矩形、马赛克涂抹、行内文字。
- 标注可以选中、移动、缩放、删除。
- 贴图可以拖动、缩放、复制、保存、锁定位置。
- 最近的截图会留在历史记录里，随时重新贴出。
- 托盘菜单、自定义快捷键、可选开机自启。
- Per-Monitor V2 高分屏支持，截图按物理像素获取。
- 默认便携：历史和配置都放在程序旁边。

## 用法

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

## 构建

需要 Visual Studio 2019+，或带 .NET Framework 4.7.2 目标包的 Build Tools。

```powershell
.\build.ps1
```

产物在 `SnapPin\bin\Release\SnapPin.exe`。

## 配置

配置在 `%APPDATA%\SnapPin\settings.json`，也可以在托盘菜单里改。

```json
{
  "captureHotkey": "Ctrl+Alt+A",
  "pinHotkey": "Ctrl+Alt+P",
  "historyDirectory": ""
}
```

`historyDirectory` 可以留空。留空时，程序目录可写就用程序目录，否则用 `%APPDATA%\SnapPin\history`。

## 说明

- 锁定贴图会固定位置，解锁入口在托盘。
- 多屏混合 DPI 时，框选遮罩在副屏上可能略有拉伸，截图本身仍按该显示器的物理像素获取。
- 未处理的异常会追加写入程序目录下的 `error.log`。

## 目录结构

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
