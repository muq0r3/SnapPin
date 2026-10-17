using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SnapPin
{
    internal static class MosaicEffect
    {
        public static void Apply(Bitmap bitmap, Rectangle rect, int cellSize)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }
            if (cellSize < 1)
            {
                cellSize = 1;
            }

            rect = Rectangle.Intersect(rect, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            int smallWidth = Math.Max(1, rect.Width / cellSize);
            int smallHeight = Math.Max(1, rect.Height / cellSize);

            using (var small = new Bitmap(smallWidth, smallHeight))
            {
                using (Graphics smallGraphics = Graphics.FromImage(small))
                {
                    smallGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    smallGraphics.PixelOffsetMode = PixelOffsetMode.Half;
                    smallGraphics.DrawImage(
                        bitmap,
                        new Rectangle(0, 0, smallWidth, smallHeight),
                        rect,
                        GraphicsUnit.Pixel);
                }

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(
                        small,
                        new Rectangle(rect.X, rect.Y, rect.Width, rect.Height),
                        new Rectangle(0, 0, smallWidth, smallHeight),
                        GraphicsUnit.Pixel);
                }
            }
        }
    }
}
