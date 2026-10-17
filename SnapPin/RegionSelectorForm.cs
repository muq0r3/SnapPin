using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class RegionSelectorForm : Form
    {
        private readonly Bitmap _screenShot;
        private readonly SolidBrush _maskBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        private readonly SolidBrush _labelBrush = new SolidBrush(Color.FromArgb(200, 30, 30, 30));
        private readonly Pen _borderPen = new Pen(Color.FromArgb(0, 170, 255), 2f);
        private readonly Font _labelFont = new Font("Segoe UI", 9f);
        private Point _start;
        private Point _current;
        private bool _dragging;

        private RegionSelectorForm(Bitmap screenShot)
        {
            _screenShot = screenShot;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            DoubleBuffered = true;
        }

        public static Rectangle? SelectRegion()
        {
            Bitmap shot = null;
            try
            {
                shot = ScreenCaptureService.CaptureVirtualScreen();
            }
            catch
            {
                shot = null;
            }

            using (var form = new RegionSelectorForm(shot))
            {
                DialogResult result = form.ShowDialog();
                Point origin = SystemInformation.VirtualScreen.Location;
                if (result == DialogResult.OK &&
                    form.SelectionSize().Width > 3 &&
                    form.SelectionSize().Height > 3)
                {
                    Rectangle clientRect = form.NormalizedSelection();
                    Rectangle dipScreenRect = new Rectangle(
                        clientRect.X + origin.X,
                        clientRect.Y + origin.Y,
                        clientRect.Width,
                        clientRect.Height);
                    return ScreenMetrics.DipToPhysical(dipScreenRect);
                }
                return null;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _dragging = true;
            _start = e.Location;
            _current = e.Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging)
            {
                return;
            }

            Rectangle oldRect = NormalizedSelection();
            _current = e.Location;
            Rectangle newRect = NormalizedSelection();

            Rectangle dirty = Rectangle.Union(oldRect, newRect);
            dirty.Inflate(40, 40);
            dirty.Intersect(ClientRectangle);
            if (dirty.Width > 0 && dirty.Height > 0)
            {
                Invalidate(dirty);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _dragging = false;
            Capture = false;

            Size size = SelectionSize();
            if (size.Width > 3 && size.Height > 3)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            _start = Point.Empty;
            _current = new Point(ClientSize.Width, ClientSize.Height);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_screenShot != null)
            {
                g.DrawImage(_screenShot, ClientRectangle);
            }
            else
            {
                g.Clear(Color.Black);
            }

            Rectangle selection = NormalizedSelection();
            if (selection.Width <= 0 || selection.Height <= 0)
            {
                g.FillRectangle(_maskBrush, ClientRectangle);
                return;
            }

            using (var region = new Region(ClientRectangle))
            {
                region.Exclude(selection);
                g.FillRegion(_maskBrush, region);
            }

            g.DrawRectangle(_borderPen, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);
            DrawSizeLabel(g, selection);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _maskBrush.Dispose();
                _labelBrush.Dispose();
                _borderPen.Dispose();
                _labelFont.Dispose();
                if (_screenShot != null)
                {
                    _screenShot.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void DrawSizeLabel(Graphics g, Rectangle selection)
        {
            string label = selection.Width + " x " + selection.Height;
            SizeF textSize = g.MeasureString(label, _labelFont);
            int labelX = selection.Left;
            int labelY = selection.Top - (int)textSize.Height - 6;
            if (labelY < 0)
            {
                labelY = selection.Top + 6;
            }

            g.FillRectangle(_labelBrush, labelX, labelY, textSize.Width + 10, textSize.Height + 4);
            g.DrawString(label, _labelFont, Brushes.White, labelX + 5, labelY + 2);
        }

        private Rectangle NormalizedSelection()
        {
            int x = Math.Min(_start.X, _current.X);
            int y = Math.Min(_start.Y, _current.Y);
            int width = Math.Abs(_current.X - _start.X);
            int height = Math.Abs(_current.Y - _start.Y);
            return new Rectangle(x, y, width, height);
        }

        private Size SelectionSize()
        {
            return NormalizedSelection().Size;
        }
    }
}
