using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SnapPin
{
    internal sealed class PinForm : Form
    {
        private const int WM_CONTEXTMENU = 0x007B;

        private const double MinZoom = 0.05;
        private const double MaxZoom = 8.0;
        private const double ZoomStep = 1.1;

        private readonly Bitmap _image;
        private double _zoom = 1.0;
        private Point _dragStartScreen;
        private Point _formStartLocation;
        private bool _dragging;
        private bool _locked;
        private ToolStripMenuItem _pinToggle;
        private ToolStripMenuItem _lockToggle;

        public event EventHandler LockChanged;

        internal PinForm(Bitmap image)
        {
            _image = new Bitmap(image);

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.ResizeRedraw |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint,
                true);

            BuildContextMenu();
            ApplySize(false);
            CenterOnPrimaryScreen();
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

        protected override void WndProc(ref Message m)
        {
            if (_locked && m.Msg == WM_CONTEXTMENU)
            {
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(_image, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_locked || e.Button != MouseButtons.Left)
            {
                return;
            }

            _dragging = true;
            _dragStartScreen = PointToScreen(e.Location);
            _formStartLocation = Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_locked || !_dragging)
            {
                return;
            }

            Point cursor = Cursor.Position;
            Location = new Point(
                _formStartLocation.X + (cursor.X - _dragStartScreen.X),
                _formStartLocation.Y + (cursor.Y - _dragStartScreen.Y));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_locked)
            {
                return;
            }

            double factor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoom * factor));
            ApplySize(true);
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_locked)
            {
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            if (_locked)
            {
                return;
            }

            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.Dispose();
            }

            base.Dispose(disposing);
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("复制图片", null, (s, e) => ClipboardHelper.TrySetImage(_image));
            menu.Items.Add("另存为…", null, (s, e) => SaveImage());
            _pinToggle = new ToolStripMenuItem("置顶");
            _pinToggle.Click += (s, e) => TopMost = !TopMost;
            menu.Items.Add(_pinToggle);
            _lockToggle = new ToolStripMenuItem("锁定（禁止拖动）");
            _lockToggle.Click += (s, e) => SetLocked(!_locked);
            menu.Items.Add(_lockToggle);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("关闭", null, (s, e) => Close());
            menu.Opening += (s, e) =>
            {
                _pinToggle.Checked = TopMost;
                _lockToggle.Checked = _locked;
            };
            ContextMenuStrip = menu;
        }

        public Size ImageSize
        {
            get { return _image.Size; }
        }

        public bool IsLocked
        {
            get { return _locked; }
        }

        public void SetLocked(bool locked)
        {
            if (_locked == locked)
            {
                return;
            }

            _locked = locked;

            var handler = LockChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void SaveImage()
        {
            ImageSaveHelper.SaveWithDialog(_image, this);
        }

        private void ApplySize(bool preserveCenter)
        {
            Point? center = preserveCenter
                ? (Point?)(new Point(Left + Width / 2, Top + Height / 2))
                : null;

            int width = Math.Max(1, (int)Math.Round(_image.Width * _zoom));
            int height = Math.Max(1, (int)Math.Round(_image.Height * _zoom));

            Size virtualSize = SystemInformation.VirtualScreen.Size;
            if (width > virtualSize.Width || height > virtualSize.Height)
            {
                double scale = Math.Min(
                    (double)virtualSize.Width / width,
                    (double)virtualSize.Height / height);
                width = Math.Max(1, (int)Math.Round(width * scale));
                height = Math.Max(1, (int)Math.Round(height * scale));
                _zoom = Math.Min((double)width / _image.Width, (double)height / _image.Height);
            }

            ClientSize = new Size(width, height);

            if (center.HasValue)
            {
                Location = new Point(
                    center.Value.X - Width / 2,
                    center.Value.Y - Height / 2);
            }
        }

        private void CenterOnPrimaryScreen()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                workingArea.X + (workingArea.Width - Width) / 2,
                workingArea.Y + (workingArea.Height - Height) / 2);
        }
    }
}
