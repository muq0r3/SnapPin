using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SnapPin
{
    internal static class ClipboardHelper
    {
        public static bool TrySetImage(Image image)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.SetImage(image);
                    return true;
                }
                catch (ExternalException)
                {
                    // 剪贴板可能被其他进程短暂占用，稍后重试。
                    Thread.Sleep(50);
                }
            }
            return false;
        }

        public static Image GetImage()
        {
            try
            {
                return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
            }
            catch (ExternalException)
            {
                return null;
            }
        }
    }
}
