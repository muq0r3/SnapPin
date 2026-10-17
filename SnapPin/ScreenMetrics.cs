using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapPin
{
    internal static class ScreenMetrics
    {
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int ENUM_CURRENT_SETTINGS = -1;

        public static Rectangle VirtualBoundsPhysical
        {
            get
            {
                return new Rectangle(
                    GetSystemMetrics(SM_XVIRTUALSCREEN),
                    GetSystemMetrics(SM_YVIRTUALSCREEN),
                    GetSystemMetrics(SM_CXVIRTUALSCREEN),
                    GetSystemMetrics(SM_CYVIRTUALSCREEN));
            }
        }

        public static Rectangle DipToPhysical(Rectangle dipRect)
        {
            Point center = new Point(
                dipRect.X + dipRect.Width / 2,
                dipRect.Y + dipRect.Height / 2);
            Screen screen = Screen.FromPoint(center);
            float scale = GetScreenScale(screen);

            Rectangle physicalVirtual = VirtualBoundsPhysical;
            Rectangle dipVirtual = SystemInformation.VirtualScreen;

            int monitorPhysicalX = physicalVirtual.X +
                (int)Math.Round((screen.Bounds.X - dipVirtual.X) * scale);
            int monitorPhysicalY = physicalVirtual.Y +
                (int)Math.Round((screen.Bounds.Y - dipVirtual.Y) * scale);

            return new Rectangle(
                monitorPhysicalX + (int)Math.Round((dipRect.X - screen.Bounds.X) * scale),
                monitorPhysicalY + (int)Math.Round((dipRect.Y - screen.Bounds.Y) * scale),
                Math.Max(1, (int)Math.Round(dipRect.Width * scale)),
                Math.Max(1, (int)Math.Round(dipRect.Height * scale)));
        }

        private static float GetScreenScale(Screen screen)
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                if (devMode.dmPelsWidth > 0 && screen.Bounds.Width > 0)
                {
                    return (float)devMode.dmPelsWidth / screen.Bounds.Width;
                }
            }
            return 1f;
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }
    }
}
