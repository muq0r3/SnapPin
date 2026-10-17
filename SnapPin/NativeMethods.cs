using System;
using System.Runtime.InteropServices;

namespace SnapPin
{
    internal static class NativeMethods
    {
        public const int WM_HOTKEY = 0x0312;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static void RequestCaptureBroadcast()
        {
            uint message = RegisterWindowMessage("SnapPin.CaptureBroadcast");
            PostMessage(HWND_BROADCAST, message, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
