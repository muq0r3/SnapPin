using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly Func<IEnumerable<PinForm>> _pinsProvider;
        private readonly ToolStripMenuItem _captureItem;
        private readonly ToolStripMenuItem _pinItem;
        private readonly ToolStripMenuItem _historyMenu;
        private readonly ToolStripMenuItem _pinManageMenu;
        private readonly ToolStripMenuItem _autostartItem;

        public event EventHandler CaptureRequested;
        public event EventHandler PinRequested;
        public event EventHandler<string> HistorySelected;
        public event EventHandler SettingsRequested;
        public event EventHandler ExitRequested;

        public TrayIconManager(Func<IEnumerable<PinForm>> pinsProvider)
        {
            _pinsProvider = pinsProvider;

            _menu = new ContextMenuStrip();

            _captureItem = new ToolStripMenuItem("截图");
            _captureItem.Click += (s, e) => Raise(CaptureRequested);

            _pinItem = new ToolStripMenuItem("贴图");
            _pinItem.Click += (s, e) => Raise(PinRequested);

            _historyMenu = new ToolStripMenuItem("历史记录");
            _historyMenu.DropDownOpening += (s, e) => BuildHistoryMenu();

            _pinManageMenu = new ToolStripMenuItem("贴图管理");
            _pinManageMenu.DropDownOpening += (s, e) => BuildPinManageMenu();

            var settingsItem = new ToolStripMenuItem("设置…");
            settingsItem.Click += (s, e) => Raise(SettingsRequested);

            _autostartItem = new ToolStripMenuItem("开机自启");
            _autostartItem.CheckOnClick = true;
            _autostartItem.Checked = AutostartHelper.IsEnabled();
            _autostartItem.CheckedChanged += (s, e) => AutostartHelper.SetEnabled(_autostartItem.Checked);

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => Raise(ExitRequested);

            _menu.Items.Add(_captureItem);
            _menu.Items.Add(_pinItem);
            _menu.Items.Add(_historyMenu);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_pinManageMenu);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(settingsItem);
            _menu.Items.Add(_autostartItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(),
                Text = "SnapPin - 截图置顶",
                ContextMenuStrip = _menu,
                Visible = true
            };
            _notifyIcon.DoubleClick += (s, e) => Raise(CaptureRequested);
        }

        public void SetHotkeyLabels(string capture, string pin)
        {
            _captureItem.Text = "截图（" + capture + "）";
            _pinItem.Text = "贴图（" + pin + "）";
        }

        public void ShowBalloon(string title, string text)
        {
            if (_notifyIcon == null)
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.ShowBalloonTip(2500);
        }

        private void BuildPinManageMenu()
        {
            _pinManageMenu.DropDownItems.Clear();

            IEnumerable<PinForm> pins = _pinsProvider != null ? _pinsProvider() : null;
            if (pins == null)
            {
                _pinManageMenu.DropDownItems.Add("（暂无贴图）");
                return;
            }

            int index = 0;
            foreach (PinForm pin in pins)
            {
                index++;
                var item = new ToolStripMenuItem(string.Format(
                    "贴图 {0}（{1}x{2}）",
                    index,
                    pin.ImageSize.Width,
                    pin.ImageSize.Height));

                var lockItem = new ToolStripMenuItem("锁定");
                lockItem.Checked = pin.IsLocked;
                lockItem.Click += (s, e) => pin.SetLocked(!pin.IsLocked);

                var pinTopItem = new ToolStripMenuItem("置顶");
                pinTopItem.Checked = pin.TopMost;
                pinTopItem.Click += (s, e) => pin.TopMost = !pin.TopMost;

                var closeItem = new ToolStripMenuItem("关闭");
                closeItem.Click += (s, e) => pin.Close();

                item.DropDownItems.Add(lockItem);
                item.DropDownItems.Add(pinTopItem);
                item.DropDownItems.Add(closeItem);
                _pinManageMenu.DropDownItems.Add(item);
            }

            if (index == 0)
            {
                _pinManageMenu.DropDownItems.Add("（暂无贴图）");
                return;
            }

            _pinManageMenu.DropDownItems.Add(new ToolStripSeparator());
            var closeAllItem = new ToolStripMenuItem("全部关闭");
            closeAllItem.Click += (s, e) => CloseAllPins();
            _pinManageMenu.DropDownItems.Add(closeAllItem);
        }

        private void BuildHistoryMenu()
        {
            _historyMenu.DropDownItems.Clear();

            List<string> entries = HistoryManager.ListEntries();
            if (entries.Count == 0)
            {
                _historyMenu.DropDownItems.Add("（暂无记录）");
                return;
            }

            foreach (string path in entries)
            {
                string label = File.GetLastWriteTime(path).ToString("HH:mm:ss");
                var item = new ToolStripMenuItem(label);
                item.ToolTipText = path;
                string capturedPath = path;
                item.Click += (s, e) =>
                {
                    var handler = HistorySelected;
                    if (handler != null)
                    {
                        handler(null, capturedPath);
                    }
                };
                _historyMenu.DropDownItems.Add(item);
            }

            _historyMenu.DropDownItems.Add(new ToolStripSeparator());
            var clearItem = new ToolStripMenuItem("清空历史");
            clearItem.Click += (s, e) => HistoryManager.Clear();
            _historyMenu.DropDownItems.Add(clearItem);
        }

        private void CloseAllPins()
        {
            IEnumerable<PinForm> pins = _pinsProvider != null ? _pinsProvider() : null;
            if (pins == null)
            {
                return;
            }

            foreach (PinForm pin in new List<PinForm>(pins))
            {
                pin.Close();
            }
        }

        private static void Raise(EventHandler handler)
        {
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        private static Icon CreateTrayIcon()
        {
            try
            {
                using (Stream stream = typeof(TrayIconManager).Assembly
                    .GetManifestResourceStream("SnapPin.app.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream, new Size(16, 16));
                    }
                }
            }
            catch
            {
            }

            return CreateFallbackTrayIcon();
        }

        private static Icon CreateFallbackTrayIcon()
        {
            using (var bitmap = new Bitmap(16, 16))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var path = new GraphicsPath())
                {
                    path.AddArc(1, 1, 6, 6, 180, 90);
                    path.AddArc(9, 1, 6, 6, 270, 90);
                    path.AddArc(9, 9, 6, 6, 0, 90);
                    path.AddArc(1, 9, 6, 6, 90, 90);
                    path.CloseFigure();
                    using (var background = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        g.FillPath(background, path);
                    }
                }

                using (var white = new SolidBrush(Color.White))
                {
                    g.FillEllipse(white, 9, 3, 3, 3);
                    var mountain = new[]
                    {
                        new Point(3, 12),
                        new Point(6, 7),
                        new Point(8, 10),
                        new Point(10, 6),
                        new Point(13, 12)
                    };
                    g.FillPolygon(white, mountain);
                }

                IntPtr hicon = bitmap.GetHicon();
                try
                {
                    using (Icon source = Icon.FromHandle(hicon))
                    {
                        return (Icon)source.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(hicon);
                }
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if (_menu != null)
            {
                _menu.Dispose();
            }
        }
    }
}
