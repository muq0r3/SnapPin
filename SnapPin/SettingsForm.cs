using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class SettingsForm : Form
    {
        private readonly HotkeyRecorderTextBox _captureBox = new HotkeyRecorderTextBox();
        private readonly HotkeyRecorderTextBox _pinBox = new HotkeyRecorderTextBox();
        private readonly TextBox _historyBox = new TextBox();

        public AppSettings Result { get; private set; }

        public SettingsForm(AppSettings settings)
        {
            Text = "SnapPin 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 232);

            uint captureMods;
            Keys captureKey;
            if (HotkeyParser.TryParse(settings.CaptureHotkey, out captureMods, out captureKey))
            {
                _captureBox.SetHotkey(captureMods, captureKey);
            }
            else
            {
                _captureBox.ClearHotkey();
            }

            uint pinMods;
            Keys pinKey;
            if (HotkeyParser.TryParse(settings.PinHotkey, out pinMods, out pinKey))
            {
                _pinBox.SetHotkey(pinMods, pinKey);
            }
            else
            {
                _pinBox.ClearHotkey();
            }

            _historyBox.Text = settings.HistoryDirectory ?? string.Empty;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(16)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            table.Controls.Add(new Label { Text = "截图快捷键", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            _captureBox.Dock = DockStyle.Fill;
            _captureBox.Margin = new Padding(0, 4, 0, 4);
            table.Controls.Add(_captureBox, 1, 0);

            table.Controls.Add(new Label { Text = "贴图快捷键", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            _pinBox.Dock = DockStyle.Fill;
            _pinBox.Margin = new Padding(0, 4, 0, 4);
            table.Controls.Add(_pinBox, 1, 1);

            table.Controls.Add(new Label { Text = "历史目录", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _historyBox.Dock = DockStyle.Fill;
            _historyBox.Margin = new Padding(0, 4, 0, 0);
            table.Controls.Add(_historyBox, 1, 2);

            table.Controls.Add(new Label(), 0, 3);
            var historyButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 0)
            };
            var browse = new Button { Text = "浏览…", Width = 82 };
            browse.Click += (s, e) => BrowseHistoryDirectory();
            var reset = new Button { Text = "自动", Width = 96 };
            reset.Click += (s, e) => _historyBox.Text = string.Empty;
            historyButtons.Controls.Add(browse);
            historyButtons.Controls.Add(reset);
            table.Controls.Add(historyButtons, 1, 3);

            table.Controls.Add(new Label(), 0, 4);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 14, 0, 0)
            };
            var save = new Button { Text = "保存", Width = 92 };
            save.Click += (s, e) => SaveAndClose();
            var cancel = new Button { Text = "取消", Width = 92, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            table.Controls.Add(buttons, 1, 4);

            Controls.Add(table);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void BrowseHistoryDirectory()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择截图历史记录保存目录";
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(_historyBox.Text))
                {
                    dialog.SelectedPath = _historyBox.Text;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _historyBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveAndClose()
        {
            if (!_captureBox.HasHotkey || !_pinBox.HasHotkey)
            {
                MessageBox.Show(
                    "请为截图和贴图各设置一个快捷键。",
                    "SnapPin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Result = new AppSettings
            {
                CaptureHotkey = HotkeyParser.Format(_captureBox.Modifiers, _captureBox.Key),
                PinHotkey = HotkeyParser.Format(_pinBox.Modifiers, _pinBox.Key),
                HistoryDirectory = (_historyBox.Text ?? string.Empty).Trim()
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class HotkeyRecorderTextBox : TextBox
    {
        public uint Modifiers { get; private set; }
        public Keys Key { get; private set; }
        public bool HasHotkey { get; private set; }

        public HotkeyRecorderTextBox()
        {
            ReadOnly = true;
            Cursor = Cursors.Default;
            TextAlign = HorizontalAlignment.Center;
            ClearHotkey();
        }

        public void SetHotkey(uint modifiers, Keys key)
        {
            Modifiers = modifiers;
            Key = key;
            HasHotkey = true;
            Text = HotkeyParser.Format(modifiers, key);
        }

        public void ClearHotkey()
        {
            Modifiers = 0;
            Key = Keys.None;
            HasHotkey = false;
            Text = "按下快捷键…";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Keys code = e.KeyCode;
            if (code == Keys.ShiftKey || code == Keys.ControlKey || code == Keys.Menu ||
                code == Keys.LWin || code == Keys.RWin)
            {
                e.SuppressKeyPress = true;
                base.OnKeyDown(e);
                return;
            }

            uint mods = 0;
            if (e.Control)
            {
                mods |= NativeMethods.MOD_CONTROL;
            }
            if (e.Alt)
            {
                mods |= NativeMethods.MOD_ALT;
            }
            if (e.Shift)
            {
                mods |= NativeMethods.MOD_SHIFT;
            }

            Modifiers = mods;
            Key = code;
            HasHotkey = true;
            Text = HotkeyParser.Format(mods, code);
            e.SuppressKeyPress = true;
            base.OnKeyDown(e);
        }
    }
}
