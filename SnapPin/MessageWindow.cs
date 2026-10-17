using System;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class MessageWindow : NativeWindow, IDisposable
    {
        private readonly uint _captureBroadcastMessage;

        public event Action<int> HotkeyPressed;
        public event Action CaptureRequested;

        public MessageWindow()
        {
            CreateHandle(new CreateParams());
            _captureBroadcastMessage = NativeMethods.RegisterWindowMessage("SnapPin.CaptureBroadcast");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                var handler = HotkeyPressed;
                if (handler != null)
                {
                    handler((int)m.WParam);
                }
                return;
            }

            if (_captureBroadcastMessage != 0 && m.Msg == (int)_captureBroadcastMessage)
            {
                var handler = CaptureRequested;
                if (handler != null)
                {
                    handler();
                }
                return;
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
