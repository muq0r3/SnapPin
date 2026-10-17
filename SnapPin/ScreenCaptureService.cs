using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SnapPin
{
    internal static class ScreenCaptureService
    {
        private const int SRCCOPY = 0x00CC0020;

        public static Bitmap CaptureVirtualScreen()
        {
            return CaptureRegion(ScreenMetrics.VirtualBoundsPhysical);
        }

        public static Bitmap CaptureRegion(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                throw new ArgumentException("Invalid capture region.");
            }

            IntPtr screenDC = GetDC(IntPtr.Zero);
            if (screenDC == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取屏幕设备上下文。");
            }

            IntPtr memoryDC = CreateCompatibleDC(screenDC);
            IntPtr hBitmap = CreateCompatibleBitmap(screenDC, region.Width, region.Height);
            IntPtr previous = SelectObject(memoryDC, hBitmap);
            try
            {
                BitBlt(
                    memoryDC,
                    0,
                    0,
                    region.Width,
                    region.Height,
                    screenDC,
                    region.X,
                    region.Y,
                    SRCCOPY);
            }
            finally
            {
                SelectObject(memoryDC, previous);
            }

            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memoryDC);
            ReleaseDC(IntPtr.Zero, screenDC);
            return bitmap;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
            IntPtr destDC,
            int x,
            int y,
            int width,
            int height,
            IntPtr srcDC,
            int srcX,
            int srcY,
            int rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
