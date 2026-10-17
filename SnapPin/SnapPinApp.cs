using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class SnapPinApp : ApplicationContext
    {
        private readonly MessageWindow _messageWindow = new MessageWindow();
        private readonly HotkeyManager _hotkeys;
        private readonly TrayIconManager _tray;
        private readonly List<PinForm> _pins = new List<PinForm>();
        private AppSettings _settings;
        private Bitmap _lastCapture;
        private bool _busy;

        public SnapPinApp()
        {
            _settings = AppSettings.Load();
            _settings.Save();
            HistoryManager.Configure(_settings.HistoryDirectory);

            _hotkeys = new HotkeyManager(_messageWindow.Handle);
            if (!TryRegisterHotkeys(_settings))
            {
                var defaults = AppSettings.CreateDefault();
                if (TryRegisterHotkeys(defaults))
                {
                    _settings = defaults;
                    _settings.Save();
                }
                else
                {
                    MessageBox.Show(
                        "快捷键注册失败，可能被其他程序占用。\n可在设置中修改，或直接编辑：\n" + AppSettings.SettingsPath(),
                        "SnapPin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            _messageWindow.HotkeyPressed += OnHotkeyPressed;
            _messageWindow.CaptureRequested += Capture;

            _tray = new TrayIconManager(() => _pins);
            _tray.CaptureRequested += (s, e) => Capture();
            _tray.PinRequested += (s, e) => PinFromClipboard();
            _tray.HistorySelected += (s, path) => PinHistory(path);
            _tray.SettingsRequested += (s, e) => OpenSettings();
            _tray.ExitRequested += (s, e) => Exit();
            _tray.SetHotkeyLabels(_settings.CaptureHotkey, _settings.PinHotkey);
        }

        private bool TryRegisterHotkeys(AppSettings settings)
        {
            uint captureMods;
            Keys captureKey;
            uint pinMods;
            Keys pinKey;

            if (!HotkeyParser.TryParse(settings.CaptureHotkey, out captureMods, out captureKey))
            {
                return false;
            }
            if (!HotkeyParser.TryParse(settings.PinHotkey, out pinMods, out pinKey))
            {
                return false;
            }

            _hotkeys.Unregister(HotkeyAction.Capture);
            _hotkeys.Unregister(HotkeyAction.PinClipboard);

            return _hotkeys.Register(HotkeyAction.Capture, captureMods, captureKey)
                && _hotkeys.Register(HotkeyAction.PinClipboard, pinMods, pinKey);
        }

        private void OnHotkeyPressed(int id)
        {
            if (id == HotkeyManager.CaptureId)
            {
                Capture();
            }
            else if (id == HotkeyManager.PinClipboardId)
            {
                PinFromClipboard();
            }
        }

        private void Capture()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            try
            {
                Rectangle? selection = RegionSelectorForm.SelectRegion();
                if (!selection.HasValue)
                {
                    return;
                }

                Bitmap captured;
                try
                {
                    captured = ScreenCaptureService.CaptureRegion(selection.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "截图失败：" + ex.Message,
                        "SnapPin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                AnnotatorResult action;
                Bitmap annotated = AnnotatorForm.Edit(captured, out action);
                captured.Dispose();

                switch (action)
                {
                    case AnnotatorResult.Pin:
                        ReleaseLastCapture();
                        _lastCapture = annotated;
                        HistoryManager.Add(annotated);
                        ClipboardHelper.TrySetImage(annotated);
                        ShowPin(annotated);
                        break;
                    case AnnotatorResult.Copy:
                        HistoryManager.Add(annotated);
                        ClipboardHelper.TrySetImage(annotated);
                        annotated.Dispose();
                        break;
                    default:
                        if (annotated != null)
                        {
                            annotated.Dispose();
                        }
                        break;
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private void PinFromClipboard()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            try
            {
                using (Image image = ClipboardHelper.GetImage())
                {
                    if (image == null)
                    {
                        if (_lastCapture != null)
                        {
                            ShowPin(_lastCapture);
                            return;
                        }

                        MessageBox.Show(
                            "剪贴板中没有图片。",
                            "SnapPin",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    using (var bitmap = new Bitmap(image))
                    {
                        ShowPin(bitmap);
                    }
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm(_settings))
            {
                if (form.ShowDialog() != DialogResult.OK || form.Result == null)
                {
                    return;
                }

                AppSettings previous = _settings;
                if (TryRegisterHotkeys(form.Result))
                {
                    _settings = form.Result;
                    _settings.Save();
                    HistoryManager.Configure(_settings.HistoryDirectory);
                    _tray.SetHotkeyLabels(_settings.CaptureHotkey, _settings.PinHotkey);
                }
                else
                {
                    TryRegisterHotkeys(previous);
                    MessageBox.Show(
                        "新快捷键注册失败（可能被其他程序占用），已恢复原设置。",
                        "SnapPin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void ShowPin(Bitmap image)
        {
            var pin = new PinForm(image);
            pin.FormClosed += (s, e) => _pins.Remove(pin);
            pin.LockChanged += (s, e) => _tray.ShowBalloon(
                "SnapPin",
                pin.IsLocked
                    ? "贴图已锁定，无法拖动或缩放，可在托盘「贴图管理」中解锁。"
                    : "贴图已解锁，可正常拖动和缩放。");
            _pins.Add(pin);
            pin.Show();
            pin.Activate();
        }

        private void PinHistory(string path)
        {
            using (Bitmap bitmap = HistoryManager.Load(path))
            {
                if (bitmap == null)
                {
                    MessageBox.Show(
                        "无法加载历史记录。",
                        "SnapPin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                ShowPin(bitmap);
            }
        }

        private void ReleaseLastCapture()
        {
            if (_lastCapture != null)
            {
                _lastCapture.Dispose();
                _lastCapture = null;
            }
        }

        private void Exit()
        {
            ReleaseLastCapture();

            foreach (PinForm pin in _pins.ToArray())
            {
                pin.Close();
                pin.Dispose();
            }
            _pins.Clear();

            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hotkeys != null)
                {
                    _hotkeys.Dispose();
                }
                if (_messageWindow != null)
                {
                    _messageWindow.Dispose();
                }
                if (_tray != null)
                {
                    _tray.Dispose();
                }
                ReleaseLastCapture();
            }

            base.Dispose(disposing);
        }
    }
}
