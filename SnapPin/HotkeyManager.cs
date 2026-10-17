using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SnapPin
{
    internal enum HotkeyAction
    {
        Capture = 1,
        PinClipboard = 2
    }

    internal sealed class HotkeyManager : IDisposable
    {
        public const int CaptureId = (int)HotkeyAction.Capture;
        public const int PinClipboardId = (int)HotkeyAction.PinClipboard;

        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, bool> _registered = new Dictionary<int, bool>();

        public HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        public bool Register(HotkeyAction action, uint modifiers, Keys key)
        {
            int id = (int)action;
            Unregister(action);

            bool ok = NativeMethods.RegisterHotKey(
                _windowHandle,
                id,
                modifiers | NativeMethods.MOD_NOREPEAT,
                (uint)key);

            _registered[id] = ok;
            return ok;
        }

        public void Unregister(HotkeyAction action)
        {
            int id = (int)action;
            bool currentlyRegistered;
            if (_registered.TryGetValue(id, out currentlyRegistered) && currentlyRegistered)
            {
                NativeMethods.UnregisterHotKey(_windowHandle, id);
            }
            _registered[id] = false;
        }

        public void Dispose()
        {
            foreach (int id in new List<int>(_registered.Keys))
            {
                bool currentlyRegistered;
                if (_registered.TryGetValue(id, out currentlyRegistered) && currentlyRegistered)
                {
                    NativeMethods.UnregisterHotKey(_windowHandle, id);
                }
            }
            _registered.Clear();
        }
    }
}
