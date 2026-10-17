using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class TransparentTextBox : TextBox
    {
        private const int WM_ERASEBKGND = 0x0014;

        public Bitmap BackgroundImageSource;
        public Rectangle BackgroundSourceRect;

        public TransparentTextBox()
        {
            BorderStyle = BorderStyle.None;
            BackColor = Color.White;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                PaintBackground(m.WParam);
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            InvalidateRect(Handle, IntPtr.Zero, true);
        }

        private void PaintBackground(IntPtr hdc)
        {
            using (Graphics g = Graphics.FromHdc(hdc))
            {
                g.Clear(Parent != null ? Parent.BackColor : Color.Black);

                if (BackgroundImageSource == null)
                {
                    return;
                }

                Rectangle source = Rectangle.Intersect(
                    BackgroundSourceRect,
                    new Rectangle(0, 0, BackgroundImageSource.Width, BackgroundImageSource.Height));
                if (source.Width <= 0 || source.Height <= 0)
                {
                    return;
                }

                float scaleX = (float)Width / Math.Max(1, BackgroundSourceRect.Width);
                float scaleY = (float)Height / Math.Max(1, BackgroundSourceRect.Height);
                var destination = new Rectangle(
                    (int)Math.Round((source.X - BackgroundSourceRect.X) * scaleX),
                    (int)Math.Round((source.Y - BackgroundSourceRect.Y) * scaleY),
                    Math.Max(1, (int)Math.Round(source.Width * scaleX)),
                    Math.Max(1, (int)Math.Round(source.Height * scaleY)));

                g.InterpolationMode = InterpolationMode.Low;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(BackgroundImageSource, destination, source, GraphicsUnit.Pixel);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    }
}
